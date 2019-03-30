using SyncHole.App.Utility;
using SyncHole.Core.Client;
using SyncHole.Core.Manifest;
using SyncHole.Core.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncHole.App.Service.Worker
{
    public class SyncHoleWorker : ISyncWorker
    {
        private readonly IStorageClient _client;
        private readonly IConfigManager _configManager;
        private readonly FileInfo _fileInfo;
        public event WorkerProgressEventHandler ProgressEvent;

        public SyncHoleWorker(IStorageClient client,
            IConfigManager configManager,
            ManifestItem workerState)
        {
            FilePath = workerState.FilePath;
            State = workerState;
            _client = client;
            _configManager = configManager;
            _fileInfo = new FileInfo(FilePath);

            //use creation time for container name
            var format = _configManager.VaultNameFormat;
            var containerName = _fileInfo.CreationTimeUtc.ToString(format);
            State.ContainerName = containerName;
        }

        public bool IsActive { get; private set; }

        public bool HasFailed { get; private set; }

        public Exception Exception { get; private set; }

        public string FilePath { get; }

        public ManifestItem State { get; }

        public async Task RunAsync(CancellationToken token)
        {
            if (!_fileInfo.Exists)
            {
                throw new FileNotFoundException(_fileInfo.FullName);
            }

            IsActive = true;

            try
            {
                //validate cancellation before making the API call
                token.ThrowIfCancellationRequested();
                var job = (State.Status == JobStatus.Pending.ToString())
                    ? await CreateNewJob() : LoadJobFromState();

                using (var fs = _fileInfo.OpenRead())
                {
                    var item = new UploadItem
                    {
                        ContentLength = _fileInfo.Length,
                        DataStream = fs
                    };

                    //if this is a resumed job, we need to set the stream position
                    if (State.Status != JobStatus.Pending.ToString())
                    {
                        item.DataStream.Position = job.CurrentPosition;
                    }

                    UpdateProgress(job, item);

                    while (job.CurrentPosition < _fileInfo.Length)
                    {
                        //validate cancellation before making the API call
                        token.ThrowIfCancellationRequested();

                        //upload next chunk
                        await _client.UploadChunkAsync(job, item);

                        //update the manifest
                        State.CurrentPosition = job.CurrentPosition;
                        State.ProcessedAt = DateTime.UtcNow;

                        UpdateProgress(job, item);
                    }

                    //validate cancellation before making the API call
                    token.ThrowIfCancellationRequested();
                    var archiveId = await _client.FinishUploadAsync(job, item);

                    //mark manifest on complete
                    State.Status = JobStatus.Completed.ToString();
                    State.CurrentPosition = job.CurrentPosition;
                    State.ProcessedAt = DateTime.UtcNow;
                    State.ArchiveId = archiveId;

                    fs.Close();
                    UpdateProgress(job, item);
                }

                CleanUp();
            }
            catch (Exception ex)
            {
                HasFailed = true;
                Exception = ex;

                //update manifest to indicate failure
                State.Status = JobStatus.Failed.ToString();
                State.ProcessedAt = DateTime.UtcNow;
            }

            IsActive = false;
        }

        private async Task<UploadJob> CreateNewJob()
        {
            //initialize new upload
            var job = await _client.InitializeAsync(State.ContainerName, _fileInfo.FullName);
            job.TotalSize = _fileInfo.Length;
            job.FilePath = _fileInfo.FullName;

            //update manifest to indicate progress
            State.UploadId = job.UploadId;
            State.Status = JobStatus.InProgress.ToString();
            State.ChunkHashes = job.ChunkChecksums;
            State.ChunkSize = job.ChunkSize;
            return job;
        }

        private UploadJob LoadJobFromState()
        {
            return new UploadJob
            {
                FilePath = State.FilePath,
                CurrentPosition = State.CurrentPosition,
                UploadId = State.UploadId,
                TotalSize = _fileInfo.Length,
                VaultName = State.ContainerName,
                ChunkChecksums = State.ChunkHashes,
                ChunkSize = State.ChunkSize
            };
        }

        private void CleanUp()
        {
            //delete the uploaded file
            _fileInfo.Delete();

            CleanUpDirectory(_fileInfo.Directory);
        }

        private void CleanUpDirectory(DirectoryInfo directory)
        {
            while (true)
            {
                //dont delete the sync dir or crawl any further above
                if (directory == null
                    || directory.FullName.Equals(_configManager.SyncDirectory, StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }

                //check if there are more files in this directory
                var hasFiles = directory.EnumerateFiles().Any();
                if (hasFiles)
                {
                    return;
                }

                //all files are deleted, remove directory
                directory.Delete();

                //cleanup the parent directory recursively
                directory = directory.Parent;
            }
        }

        private void UpdateProgress(UploadJob job, UploadItem item)
        {
            ProgressEvent?.Invoke(job, item);
        }
    }
}
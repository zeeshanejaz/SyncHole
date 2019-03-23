using SyncHole.App.Console;
using SyncHole.App.Utility;
using SyncHole.Core.Client;
using SyncHole.Core.Model;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SyncHole.App.Service
{
    public class SyncHoleWorker : ISyncWorker
    {
        private readonly IStorageClient _client;
        private readonly IConfigManager _configManager;
        private readonly FileInfo _fileInfo;
        private readonly ProgressPrinter _progressPrinter;

        public SyncHoleWorker(IStorageClient client,
            IConfigManager configManager,
            string filePath)
        {
            FilePath = filePath;
            _client = client;
            _configManager = configManager;
            _fileInfo = new FileInfo(filePath);
            _progressPrinter = new ProgressPrinter();
        }

        public async Task RunAsync(CancellationToken token)
        {
            if (!_fileInfo.Exists)
            {
                throw new FileNotFoundException(_fileInfo.FullName);
            }

            IsActive = true;

            //use creation time for container name
            var format = _configManager.VaultNameFormat;
            var containerName = _fileInfo.CreationTimeUtc.ToString(format);
            try
            {
                //validate cancellation before making the API call
                token.ThrowIfCancellationRequested();
                var job = await _client.InitializeAsync(containerName, _fileInfo.FullName);

                using (var fs = _fileInfo.OpenRead())
                {
                    job.TotalSize = _fileInfo.Length;
                    job.FilePath = _fileInfo.FullName;
                    UpdateProgress(job);

                    var item = new UploadItem
                    {
                        ContentLength = _fileInfo.Length,
                        DataStream = fs
                    };

                    while (job.CurrentPosition < _fileInfo.Length)
                    {
                        //validate cancellation before making the API call
                        token.ThrowIfCancellationRequested();

                        //upload next chunk
                        await _client.UploadChunkAsync(job, item);
                        UpdateProgress(job);
                    }

                    //validate cancellation before making the API call
                    token.ThrowIfCancellationRequested();
                    await _client.FinishUploadAsync(job, item);

                    fs.Close();
                    UpdateProgress(job);
                }

                //delete the uploaded file
                _fileInfo.Delete();
            }
            catch (Exception ex)
            {
                HasFailed = true;
                Exception = ex;
            }

            IsActive = false;
        }

        public bool IsActive { get; private set; }

        public bool HasFailed { get; private set; }

        public Exception Exception { get; private set; }

        public string FilePath { get; }

        private void UpdateProgress(UploadJob job)
        {
            _progressPrinter.PrintProgress(job);
        }
    }
}

using SyncHole.Core.Client;
using SyncHole.Core.Model;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SyncHole.App
{
    public class SyncHoleWorker : ISyncWorker
    {
        private readonly IStorageClient _client;
        private readonly IConfigManager _configManager;
        private readonly FileInfo _fileInfo;

        public SyncHoleWorker(IStorageClient client,
            IConfigManager configManager,
            string filePath)
        {
            FilePath = filePath;
            _client = client;
            _configManager = configManager;
            _fileInfo = new FileInfo(filePath);
        }

        public async Task RunAsync()
        {
            if (!_fileInfo.Exists)
            {
                throw new FileNotFoundException(_fileInfo.FullName);
            }

            IsActive = true;

            //use creation time for container name
            var format = _configManager.ArchiveNameFormat;
            var containerName = _fileInfo.CreationTimeUtc.ToString(format);
            try
            {
                var job = await _client.InitializeAsync(containerName, _fileInfo.FullName);

                using (var fs = _fileInfo.OpenRead())
                {
                    var item = new UploadItem
                    {
                        ContentLength = _fileInfo.Length,
                        DataStream = fs
                    };

                    while (job.CurrentPosition < _fileInfo.Length)
                    {
                        await _client.UploadChunkAsync(job, item);
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }

                    await _client.FinishUploadAsync(job, item);
                    fs.Close();
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

        public string FilePath { get; private set; }
    }
}

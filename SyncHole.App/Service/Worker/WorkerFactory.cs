using SyncHole.App.Utility;
using SyncHole.Core.Client;
using SyncHole.Core.Manifest;
using SyncHole.Core.Model;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SyncHole.App.Service.Worker
{
    public class WorkerFactory : IWorkerFactory
    {
        private readonly IStorageClient _storageClient;
        private readonly IConfigManager _configManager;
        private readonly SyncManifest _manifest;

        public WorkerFactory(IStorageClient storageClient,
            IConfigManager configManager,
            SyncManifest manifest)
        {
            _storageClient = storageClient;
            _configManager = configManager;
            _manifest = manifest;
        }

        public async Task<ISyncWorker> CreateWorkerAsync()
        {
            return ResumeJobWorker() ?? await NewJobWorkerAsync();
        }

        private async Task<ISyncWorker> NewJobWorkerAsync()
        {
            //enumerate files and load only those that fit in the batch
            var fileEnumerable = Directory.EnumerateFiles(
                _configManager.SyncDirectory, _configManager.SyncFileFilter, SearchOption.AllDirectories);

            //load upload worker for the first file found in the dir
            string firstFile = null;
            foreach (var filePath in fileEnumerable)
            {
                var fileName = Path.GetFileName(filePath);

                //ignore the file that matches the regex, or is the manifest file
                if ((!string.IsNullOrWhiteSpace(_configManager.IgnoreFileRegex)
                     && Regex.IsMatch(fileName, _configManager.IgnoreFileRegex))
                    || filePath.EndsWith(Constants.ManifestFileName))
                {
                    continue;
                }

                firstFile = filePath;
            }

            //no file found with matching criteria
            if (firstFile == null)
            {
                return null;
            }

            //create a new upload worker
            var initialState = new ManifestItem
            {
                FilePath = firstFile,
                Status = JobStatus.Pending.ToString(),
                CurrentPosition = 0,
                ProcessedAt = DateTime.UtcNow
            };

            //create a worker for a new job
            var newWorker = new SyncHoleWorker(_storageClient, _configManager, initialState);

            //this manifest will keep track of the progress
            await _manifest.AddAsync(newWorker.State);
            return newWorker;
        }

        private ISyncWorker ResumeJobWorker()
        {
            //load upload worker for the first incomplete job found in manifest
            var firstItem = _manifest.Items
                .FirstOrDefault(item => item.Status != JobStatus.Completed.ToString());

            //no file found with matching criteria
            if (firstItem == null)
            {
                return null;
            }

            //resume an upload worker
            return new SyncHoleWorker(_storageClient, _configManager, firstItem);
        }
    }
}

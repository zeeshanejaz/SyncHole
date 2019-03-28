using Microsoft.Extensions.Logging;
using SyncHole.App.Utility;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SyncHole.App.Service
{
    public class SyncHoleService : ISyncService
    {
        private readonly IConfigManager _configManager;
        private readonly ILogger<SyncHoleService> _logger;
        private readonly IWorkerFactory _workerFactory;
        private readonly CancellationTokenSource _cts;
        private readonly ExponentialBackoff _expBackoff;
        private readonly SyncManifest _syncManifest;

        public SyncHoleService(
            IWorkerFactory workerFactory,
            ILogger<SyncHoleService> logger,
            IConfigManager configManager, SyncManifest syncManifest)
        {
            _logger = logger;
            _configManager = configManager;
            _syncManifest = syncManifest;
            _workerFactory = workerFactory;
            _cts = new CancellationTokenSource();
            _expBackoff = new ExponentialBackoff(100, 60000);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Starting the SyncHole service");

            try
            {
                //Keep poling and uploading
                while (true)
                {
                    var uploaded = await RunNextWorkerAsync();
                    if (uploaded)
                    {
                        await _syncManifest.SaveAsync();
                    }

                    await _expBackoff.DelayAsync(!uploaded);
                }
            }
            catch (AggregateException)
            {
                //if the cancellation has been requested absorb the error
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
        }

        private async Task<bool> RunNextWorkerAsync()
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
                return false;
            }

            _logger.LogTrace($"Uploading file: {firstFile}");

            //create upload worker
            var worker = _workerFactory.CreateWorker(firstFile);

            //this manifest will keep track of the progress
            await _syncManifest.AddAsync(worker.ItemManifest);

            _logger.LogTrace($"Starting upload process: {worker.FilePath}");

            //start background task
            await worker.RunAsync(_cts.Token);

            _logger.LogTrace($"Finished upload process: {worker.FilePath}");
            return true;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Stopping the SyncHole service");
            await _syncManifest.SaveAsync();
            _cts.Cancel();
        }
    }
}

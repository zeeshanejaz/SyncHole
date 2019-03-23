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

        public SyncHoleService(
            IWorkerFactory workerFactory,
            ILogger<SyncHoleService> logger,
            IConfigManager configManager)
        {
            _logger = logger;
            _configManager = configManager;
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

                //ignore the file that matches the regex
                if (!string.IsNullOrWhiteSpace(_configManager.IgnoreFileRegex)
                    && Regex.IsMatch(fileName, _configManager.IgnoreFileRegex))
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

            _logger.LogTrace($"Starting upload process: {worker.FilePath}");

            //start background task
            await worker.RunAsync(_cts.Token);

            _logger.LogTrace($"Finished upload process: {worker.FilePath}");
            return true;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Stopping the SyncHole service");
            _cts.Cancel();
            return Task.CompletedTask;
        }
    }
}

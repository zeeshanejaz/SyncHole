using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncHole.App
{
    public class SyncHoleService : ISyncService
    {
        private readonly ILogger<SyncHoleService> _logger;
        private readonly IConfiguration _configuration;
        private readonly FileSystemWatcher _fileWatcher;
        private readonly IWorkerFactory _workerFactory;
        private readonly List<ISyncWorker> _batchTasks;
        private readonly CancellationTokenSource _cts;

        public SyncHoleService(FileSystemWatcher fileWatcher,
            IWorkerFactory workerFactory,
            ILogger<SyncHoleService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _fileWatcher = fileWatcher;
            _workerFactory = workerFactory;
            _batchTasks = new List<ISyncWorker>();
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Starting the SyncHole service");
            _fileWatcher.Created += FileWatcherOnCreated;
            _fileWatcher.EnableRaisingEvents = true;

            //upload in batch
            try
            {
                await RunBatchLogicAsync();
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

        private async Task RunBatchLogicAsync()
        {
            do
            {
                //load items into the batch
                LoadBatchTasks();

                _logger.LogTrace($"A new batch has been loaded, found {_batchTasks.Count} items");

                //no more items in the batch
                if (_batchTasks.Count <= 0)
                {
                    break;
                }

                _logger.LogTrace("Waiting for the batch process to finish");

                //wait for all items to finish to reevaluate batch
                await Task.WhenAll(_batchTasks.Select(t => t.RunAsync()));

                _logger.LogTrace("Finished processing the batch");

                _batchTasks.Clear();
            }
            while (true);
        }

        private void LoadBatchTasks()
        {
            var batchSize = _configuration.GetValue<int>("SyncOptions:BatchSize");

            //enumerate files and load only those that fit in the batch
            var fileEnumerable = Directory.EnumerateFiles(
                _fileWatcher.Path, _fileWatcher.Filter, SearchOption.AllDirectories);

            foreach (var file in fileEnumerable)
            {
                //is this path already in the queue?
                if (_batchTasks.Any(w => w.FilePath.Equals(file, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                _logger.LogTrace($"Adding file to batch: {file}");

                //create upload worker
                var worker = _workerFactory.CreateWorker(file);
                _batchTasks.Add(worker);

                if (_batchTasks.Count >= batchSize)
                {
                    break;
                }
            }
        }

        private void FileWatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            _logger.LogTrace($"Received file created event: {e.FullPath}");

            //a batch is already in progress
            if (_batchTasks.Count > 0)
            {
                _logger.LogTrace("A batch process is already in progress");
                return;
            }

            try
            {
                _logger.LogTrace($"Waiting for file lock release: {e.FullPath}");

                //the file may not have been completely written, let's wait
                FileUtils.WaitUntilLocked(e.FullPath);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex.Message);
                _logger.LogTrace($"Cancelling file created trigger: {e.FullPath}");
                //omitting the file to trigger the process
                return;
            }

            //initiate another batch
            _logger.LogTrace("Starting a new batch process");
            new TaskFactory().StartNew(async () => await RunBatchLogicAsync());
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Stopping the SyncHole service");
            _cts.Cancel();
            _fileWatcher.EnableRaisingEvents = false;
            return Task.CompletedTask;
        }
    }
}

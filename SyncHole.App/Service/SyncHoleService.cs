using Microsoft.Extensions.Logging;
using SyncHole.App.Console;
using SyncHole.App.Service.Worker;
using SyncHole.App.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SyncHole.App.Service
{
    public class SyncHoleService : ISyncService
    {
        private readonly ILogger<SyncHoleService> _logger;
        private readonly IWorkerFactory _workerFactory;
        private readonly CancellationTokenSource _cts;
        private readonly ExponentialBackoff _expBackoff;
        private readonly SyncManifest _syncManifest;

        public SyncHoleService(
            IWorkerFactory workerFactory,
            ILogger<SyncHoleService> logger,
            SyncManifest syncManifest)
        {
            _logger = logger;
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
                await new TaskFactory().StartNew(async () =>
                {
                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        //create a new worker
                        var worker = await _workerFactory.CreateWorkerAsync();
                        var hasWorker = worker != null;

                        //execute the worker job
                        if (hasWorker)
                        {
                            await RunNextWorkerAsync(worker);
                            await _syncManifest.SaveAsync();
                        }

                        //idle wait
                        await _expBackoff.DelayAsync(!hasWorker);
                    }
                }, cancellationToken);
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

        private async Task RunNextWorkerAsync(ISyncWorker worker)
        {
            _logger.LogTrace($"Uploading file: {worker.FilePath}");

            //subscribe to worker for progress event
            var currentProgressPrinter = new ProgressPrinter();
            worker.ProgressEvent += (job, item) =>
            {
                currentProgressPrinter.PrintProgress(job);
            };

            _logger.LogTrace($"Starting upload process: {worker.FilePath}");

            //start background task
            await worker.RunAsync(_cts.Token);

            // no need to retian chunk hashes upon completion
            worker.State?.ChunkHashes.Clear();

            _logger.LogTrace($"Finished upload process: {worker.FilePath}");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Stopping the SyncHole service");
            await _syncManifest.SaveAsync();
            _cts.Cancel();
        }
    }
}

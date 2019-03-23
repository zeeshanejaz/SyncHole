using SyncHole.App.Utility;
using SyncHole.Core.Client;

namespace SyncHole.App.Service
{
    public class WorkerFactory : IWorkerFactory
    {
        private readonly IStorageClient _storageClient;
        private readonly IConfigManager _configManager;

        public WorkerFactory(IStorageClient storageClient, IConfigManager configManager)
        {
            _storageClient = storageClient;
            _configManager = configManager;
        }

        public ISyncWorker CreateWorker(string syncFilePath)
        {
            return new SyncHoleWorker(_storageClient, _configManager, syncFilePath);
        }
    }
}

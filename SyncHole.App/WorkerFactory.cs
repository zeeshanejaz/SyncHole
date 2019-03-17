using SyncHole.Core.Client;

namespace SyncHole.App
{
    public class WorkerFactory : IWorkerFactory
    {
        private readonly IStorageClient _storageClient;

        public WorkerFactory(IStorageClient storageClient)
        {
            _storageClient = storageClient;
        }

        public ISyncWorker CreateWorker(string syncFilePath)
        {
            return new SyncHoleWorker(_storageClient, syncFilePath);
        }
    }
}

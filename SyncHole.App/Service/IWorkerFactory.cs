namespace SyncHole.App.Service
{
    public interface IWorkerFactory
    {
        ISyncWorker CreateWorker(string syncFilePath);
    }
}

namespace SyncHole.App
{
    public interface IWorkerFactory
    {
        ISyncWorker CreateWorker(string syncFilePath);
    }
}

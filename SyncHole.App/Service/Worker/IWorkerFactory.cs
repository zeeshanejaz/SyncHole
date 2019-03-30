using System.Threading.Tasks;

namespace SyncHole.App.Service.Worker
{
    public interface IWorkerFactory
    {
        Task<ISyncWorker> CreateWorkerAsync();
    }
}

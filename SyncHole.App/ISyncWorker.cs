using System;
using System.Threading.Tasks;

namespace SyncHole.App
{
    public interface ISyncWorker
    {
        Task RunAsync();

        bool IsActive { get; }

        bool HasFailed { get; }

        Exception Exception { get; }

        string FilePath { get; }
    }
}

using SyncHole.Core.Manifest;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SyncHole.App.Service
{
    public interface ISyncWorker
    {
        Task RunAsync(CancellationToken token);

        bool IsActive { get; }

        bool HasFailed { get; }

        Exception Exception { get; }

        string FilePath { get; }

        ManifestItem ItemManifest { get; }
    }
}

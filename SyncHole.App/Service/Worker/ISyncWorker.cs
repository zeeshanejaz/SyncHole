using System;
using System.Threading;
using System.Threading.Tasks;
using SyncHole.Core.Manifest;
using SyncHole.Core.Model;

namespace SyncHole.App.Service.Worker
{
    public interface ISyncWorker
    {
        Task RunAsync(CancellationToken token);

        bool IsActive { get; }

        bool HasFailed { get; }

        Exception Exception { get; }

        string FilePath { get; }

        ManifestItem State { get; }

        event WorkerProgressEventHandler ProgressEvent;
    }

    public delegate void WorkerProgressEventHandler(UploadJob job, UploadItem item);
}

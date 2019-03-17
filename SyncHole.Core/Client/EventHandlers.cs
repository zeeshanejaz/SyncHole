using SyncHole.Core.Model;

namespace SyncHole.Core.Client
{
    public delegate void StartedEventHandler(UploadJob job);

    public delegate void CompletedEventHandler(UploadJob job, string archiveId);

    public delegate void ProgressEventHandler(UploadJob job);

    public delegate void FailureEventHandler(UploadJob job);
}

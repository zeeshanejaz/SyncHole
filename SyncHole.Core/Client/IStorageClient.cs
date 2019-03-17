using SyncHole.Core.Model;
using System.Threading.Tasks;

namespace SyncHole.Core.Client
{
    public interface IStorageClient
    {
        Task<UploadJob> InitializeAsync(string containerName, string description, long? chunkSize = null);

        Task UploadChunkAsync(UploadJob job, UploadItem item);

        Task<string> FinishUploadAsync(UploadJob job, UploadItem item);
    }
}

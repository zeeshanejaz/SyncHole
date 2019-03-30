using System.Threading.Tasks;

namespace SyncHole.Core.Manifest
{
    public interface IManifestBuilder
    {
        Task SaveManifestAsync(ManifestCollection manifest, string manifestPath);

        Task LoadManifestAsync(ManifestCollection manifest, string manifestPath);
    }
}
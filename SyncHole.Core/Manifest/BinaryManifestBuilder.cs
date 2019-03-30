using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SyncHole.Core.Manifest
{
    public class BinaryManifestBuilder : IManifestBuilder
    {
        public Task SaveManifestAsync(ManifestCollection manifest, string manifestPath)
        {
            using (Stream stream = File.Open(manifestPath, FileMode.Create))
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, manifest.ToArray());
            }

            return Task.CompletedTask;
        }

        public Task LoadManifestAsync(ManifestCollection manifest, string manifestPath)
        {
            var manifestInfo = new FileInfo(manifestPath);
            if (!manifestInfo.Exists)
            {
                return Task.CompletedTask;
            }

            using (Stream stream = File.Open(manifestPath, FileMode.Open))
            {
                var binaryFormatter = new BinaryFormatter();
                var temp = (ManifestItem[])binaryFormatter.Deserialize(stream);
                manifest.Clear();
                manifest.AddRange(temp);
            }

            return Task.CompletedTask;
        }
    }
}

using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace SyncHole.Core.Manifest
{
    public class JsonManifestBuilder : IManifestBuilder
    {
        public Task SaveManifestAsync(ManifestCollection manifest, string manifestPath)
        {
            var jsonData = JsonConvert.SerializeObject(manifest.ToArray(), Formatting.Indented);
            File.WriteAllText(manifestPath, jsonData);
            return Task.CompletedTask;
        }

        public Task LoadManifestAsync(ManifestCollection manifest, string manifestPath)
        {
            var manifestInfo = new FileInfo(manifestPath);
            if (!manifestInfo.Exists)
            {
                return Task.CompletedTask;
            }

            var jsonData = File.ReadAllText(manifestPath);
            var items = JsonConvert.DeserializeObject<ManifestItem[]>(jsonData);
            manifest.Clear();
            manifest.AddRange(items);
            return Task.CompletedTask;
        }
    }
}

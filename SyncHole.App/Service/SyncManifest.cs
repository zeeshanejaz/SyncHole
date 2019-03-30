using SyncHole.App.Utility;
using SyncHole.Core.Manifest;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SyncHole.App.Service
{
    public class SyncManifest
    {
        private readonly IConfigManager _configManager;
        private readonly ManifestCollection _manifestCollection;
        private readonly IManifestBuilder _manifestBuilder;
        private readonly SemaphoreSlim _semaphoreSlim;

        public SyncManifest(IConfigManager configManager,
            IManifestBuilder manifestBuilder)
        {
            _configManager = configManager;
            _manifestBuilder = manifestBuilder;
            _manifestCollection = new ManifestCollection();
            _semaphoreSlim = new SemaphoreSlim(1, 1);
        }

        public IReadOnlyCollection<ManifestItem> Items =>
            new ReadOnlyCollection<ManifestItem>(_manifestCollection);

        public async Task ReloadAsync()
        {
            //load from the file
            var manifestPath = Path.Combine(_configManager.SyncDirectory, Constants.ManifestFileName);

            await _semaphoreSlim.WaitAsync();
            try
            {
                //empty the collection before reloading
                _manifestCollection.Clear();
                await _manifestBuilder.LoadManifestAsync(_manifestCollection, manifestPath);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task SaveAsync()
        {
            //load from the file
            var manifestPath = Path.Combine(_configManager.SyncDirectory, Constants.ManifestFileName);

            await _semaphoreSlim.WaitAsync();
            try
            {
                await _manifestBuilder.SaveManifestAsync(_manifestCollection, manifestPath);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task AddAsync(ManifestItem manifestItem)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                _manifestCollection.Add(manifestItem);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
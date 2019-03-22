using Microsoft.Extensions.Configuration;

namespace SyncHole.App
{
    public class ConfigManager : IConfigManager
    {
        private readonly IConfiguration _configuration;
        private const string SyncDirKey = "SyncOptions:SyncDirectory";
        private const string SyncBatchSizeKey = "SyncOptions:BatchSize";
        private const string ArchiveNameFormatKey = "SyncOptions:ArchiveNameFormat";

        public ConfigManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string SyncDirectory => _configuration.GetValue<string>(SyncDirKey);
        public int SyncBatchSize => _configuration.GetValue<int>(SyncBatchSizeKey);
        public string ArchiveNameFormat => _configuration.GetValue<string>(ArchiveNameFormatKey);
    }
}

using Microsoft.Extensions.Configuration;

namespace SyncHole.App.Utility
{
    public class ConfigManager : IConfigManager
    {
        private readonly IConfiguration _configuration;
        private const string SyncDirKey = "SyncOptions:SyncDirectory";
        private const string VaultNameFormatKey = "SyncOptions:VaultNameFormat";
        private const string SyncFileFilterKey = "SyncOptions:SyncFilter";
        private const string IgnoreFileRegexKey = "SyncOptions:IgnoreRegex";

        public ConfigManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string SyncDirectory => _configuration.GetValue<string>(SyncDirKey);
        public string VaultNameFormat => _configuration.GetValue<string>(VaultNameFormatKey);
        public string SyncFileFilter => _configuration.GetValue<string>(SyncFileFilterKey);
        public string IgnoreFileRegex => _configuration.GetValue<string>(IgnoreFileRegexKey);
    }
}

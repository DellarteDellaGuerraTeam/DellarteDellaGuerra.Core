using System;
using System.IO;
using System.Xml.Serialization;
using DellarteDellaGuerra.Domain.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Utils;

namespace DellarteDellaGuerra.Infrastructure.Configuration.Providers
{
    /**
     * <summary>
     *  Detects changes in the mod's configuration file and updates the in-memory configuration accordingly.
     *  <br/>
     *  <br/>
     * The configuration file is expected to be named "dadg.config.xml" and should be located in the mod's root config folder.
     * </summary>
     */
    public class DadgConfigWatcher : IConfigurationProvider<DadgConfig>
    {
        private const string ConfigFileName = "dadg.config.xml";
        private readonly ILogger _logger;
        private readonly FileSystemWatcher? _configWatcher;
        private DadgConfig? _config;

        public DadgConfigWatcher(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DadgConfigWatcher>();
            _configWatcher = InitialiseConfigWatcher();
        }

        /**
         * <summary>
         * Gets the mod's configuration.
         * </summary>
         * 
         */
        public DadgConfig? Config => _config;

        /**
         * <summary>
         * Disposes of the configuration watcher.
         * </summary>
         */
        public void Destroy()
        {
            _configWatcher?.Dispose();
        }

        private FileSystemWatcher? InitialiseConfigWatcher()
        {
            string? configFolderPath = ResourceLocator.GetConfigurationFolderPath();
            if (configFolderPath == null)
            {
                _logger.Warn("No configuration folder found");
                return null;
            }
            var watcher = new FileSystemWatcher
            {
                Path = configFolderPath,
                Filter = ConfigFileName,
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            watcher.Changed += OnConfigChanged;
            watcher.Created += OnConfigChanged;
            LoadConfig();
            return watcher;
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            // Config file exists as a change was detected 
            string configFolderPath = ResourceLocator.GetConfigurationFolderPath()!;
            var configPath = Path.Combine(configFolderPath, ConfigFileName);
            var serialiser = new XmlSerializer(typeof(DadgConfig));

            try
            {
                using var writer = new FileStream(configPath, FileMode.Open);
                _config = (DadgConfig) serialiser.Deserialize(writer);
            }
            catch (InvalidOperationException e)
            {
                _logger.Error($"Failed to parse config file: {e.InnerException}");
            }
            catch (IOException e)
            {
                _logger.Error($"Failed to read config file: {e}");
            }
        }
    }    
}


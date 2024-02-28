using System;
using System.IO;
using System.Xml.Serialization;
using DellarteDellaGuerra.Configuration.Models;
using DellarteDellaGuerra.Logging;
using DellarteDellaGuerra.Utils;
using NLog;

namespace DellarteDellaGuerra.Configuration.Providers
{

    /**
     * <summary>
     *  Detects changes in the mod's configuration file and updates the in-memory configuration accordingly.
     *  <br/>
     *  <br/>
     * The configuration file is expected to be named "dadg.config.xml" and should be located in mod's root config folder.
     * </summary>
     */
    public class DadgConfigWatcher : IConfigurationProvider<DadgConfig>
    {
        private static readonly Logger Logger = LoggerFactory.GetLogger<DadgConfigWatcher>();
        private const string ConfigFileName = "dadg.config.xml";
        private DadgConfig? _config;

        public DadgConfigWatcher()
        {
            // TODO - correctly dispose of the watcher
            var watcher = new FileSystemWatcher
            {
                Path = ResourceLocator.GetConfigurationFolderPath(),
                Filter = ConfigFileName,
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            watcher.Changed += OnConfigChanged;
            LoadConfig();
        }

        /**
         * <summary>
         * Gets the mod's configuration.
         * </summary>
         * 
         */
        public DadgConfig? Config
        {
            get => _config;
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            var configPath = Path.Combine(ResourceLocator.GetConfigurationFolderPath(), ConfigFileName);
            var serialiser = new XmlSerializer(typeof(DadgConfig));
            try
            {
                using var writer = new FileStream(configPath, FileMode.Open);
                _config = (DadgConfig)serialiser.Deserialize(writer);
            }
            catch (InvalidOperationException e)
            {
                Logger.Error("Failed to parse config file: {}", e.InnerException);
            }
            catch (IOException e)
            {
                Logger.Error("Failed to read config file: {}", e);
            }
        }
    }
}
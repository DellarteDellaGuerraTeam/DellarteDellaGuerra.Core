using System;
using System.IO;
using System.Xml.Serialization;
using DellarteDellaGuerra.Configuration.Models;
using DellarteDellaGuerra.Logging;
using DellarteDellaGuerra.Utils;
using NLog;

namespace DellarteDellaGuerra.Configuration.Providers;

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
    private static readonly Logger Logger = LoggerFactory.GetLogger<DadgConfigWatcher>();
    private static readonly FileSystemWatcher? ConfigWatcher = InitialiseConfigWatcher();
    private static DadgConfig? _config;

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
    public static void Destroy()
    {
        ConfigWatcher?.Dispose();
    }

    private static FileSystemWatcher? InitialiseConfigWatcher()
    {
        string? configFolderPath = ResourceLocator.GetConfigurationFolderPath();
        if (configFolderPath == null)
        {
            Logger.Warn("No configuration folder found");
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

    private static void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
        LoadConfig();
    }

    private static void LoadConfig()
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
            Logger.Error("Failed to parse config file: {}", e.InnerException);
        }
        catch (IOException e)
        {
            Logger.Error("Failed to read config file: {}", e);
        }
    }
}

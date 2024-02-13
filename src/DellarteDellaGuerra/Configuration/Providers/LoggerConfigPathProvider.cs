using System.IO;
using DellarteDellaGuerra.Utils;

namespace DellarteDellaGuerra.Configuration.Providers;

/**
 * <summary>
 *  A provider for the path to the logger configuration file.
 * </summary>
 */
public class LoggerConfigPathProvider : IConfigurationProvider<string>
{
    private const string LogConfigFileName = "nlog.config";

    private static readonly string? LogConfigPath = GetConfigFilePath();

    /**
     * <summary>
     * Gets the path to the logger configuration file.
     * </summary>
     * <returns>
     * The path to the logger configuration file.
     * </returns>
     */
    public string? Config => LogConfigPath;

    private static string? GetConfigFilePath()
    {
        string? configFolderPath = ResourceLocator.GetConfigurationFolderPath();
        return configFolderPath != null ? Path.Combine(configFolderPath, LogConfigFileName) : null;
    }
}

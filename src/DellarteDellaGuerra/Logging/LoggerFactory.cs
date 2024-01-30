using System.IO;
using System.Linq;
using NLog;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Logging;

public static class LoggerFactory
{
    private const string LogConfigFileName = "nlog.config";

    /**
     * <summary>
     *     Gets a logger for the specified type.
     * </summary>
     * <typeparam name="T">The type to get a logger for.</typeparam>
     * <returns>A logger for the specified type.</returns>
     * <remarks>
     * The logger name is shortened to the first letter of each namespace and the type name seperated by points.
     * It is also padded to 30 characters.
     * So, if the full type name is "DellarteDellaGuerra.Logging.LoggerFactory",
     * then the logger name will be "d.l.LoggerFactory             ".
     * </remarks>
     */
    public static Logger GetLogger<T>()
    {
        var configFilePath = GetConfigurationFilePath();
        var shortenedLoggerName = ShortenLoggerName(typeof(T).FullName ?? string.Empty);
        return LogManager.Setup().LoadConfigurationFromFile(configFilePath).GetLogger(shortenedLoggerName);
    }

    private static string? GetConfigurationFilePath()
    {
        var fullSubModuleName = typeof(SubModule).FullName;
        if (string.IsNullOrWhiteSpace(fullSubModuleName))
        {
            return null;
        }
        var assemblyPath = Module.CurrentModule?.GetSubModule(fullSubModuleName).Assembly.Location;
        return string.IsNullOrWhiteSpace(assemblyPath)
            ? null
            : Path.Combine(Directory.GetParent(assemblyPath)?.FullName ?? string.Empty, LogConfigFileName);
    }

    private static string ShortenLoggerName(string loggerName, int namespaceLimit = 1, int loggerNameLimit = 30)
    {
        var namespaces = loggerName.Split('.');
        var shortenedNamespaces = namespaces
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select((name, index) => index == namespaces.Length - 1 ? name : name.Substring(0, namespaceLimit).ToLower());

        return string.Join(".", shortenedNamespaces).PadRight(loggerNameLimit).Substring(0, loggerNameLimit);
    }
}
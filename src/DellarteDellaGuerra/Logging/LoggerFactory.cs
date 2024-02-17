using System.IO;
using System.Linq;
using DellarteDellaGuerra.Configuration.Providers;
using DellarteDellaGuerra.Utils;
using NLog;
using NLog.Config;
using NLog.Targets;
using Logger = NLog.Logger;

namespace DellarteDellaGuerra.Logging;

public static class LoggerFactory
{
    private static readonly IConfigurationProvider<string> ConfigProvider = new LoggerConfigPathProvider();

    private static readonly ISetupBuilder ConfiguredLoggerBuilder = GetConfiguredLoggerBuilder();
    
    /**
     * <summary>
     *     Gets a logger for the specified type.
     * </summary>
     * <typeparam name="T">The type to get a logger for.</typeparam>
     * <returns>A logger for the specified type.</returns>
     * <remarks>
     * The logger name is shortened to the first letter of each namespace and the type name seperated by points.
     * It is also padded to 30 characters.
     * </remarks>
     * <example>
     * &#47;&#47; full type namespace is DellarteDellaGuerra.Logging.LoggerFactory
     * <br/>
     * Logger logger = GetLogger&lt;LoggerFactory&gt;();
     * <br/>
     * // "d.l.LoggerFactory&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;"
     * <br/>
     * Console.WriteLine(logger.Name);
     * </example>
     */
    public static Logger GetLogger<T>()
    {
        var shortenedLoggerName = ShortenLoggerName(typeof(T).FullName ?? string.Empty);
        return ConfiguredLoggerBuilder.GetLogger(shortenedLoggerName);
    }

    private static ISetupBuilder GetConfiguredLoggerBuilder()
    {
        if (ConfigProvider.Config != null)
        {
            return LogManager.Setup().LoadConfigurationFromFile(ConfigProvider.Config);   
        }
        var modulePath = ResourceLocator.GetModuleFolder();
        if (modulePath != null)
        {
            var defaultConfig = GetDefaultConfiguration(Path.Combine(modulePath, "log"));
            LogManager.Setup().LoadConfiguration(defaultConfig);
        }
        // log where Bannerlord is executed from
        return LogManager.Setup();
    }
    
    private static string ShortenLoggerName(string loggerName, int namespaceLimit = 1, int loggerNameLimit = 30)
    {
        var namespaces = loggerName.Split('.');
        var shortenedNamespaces = namespaces
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select((name, index) => index == namespaces.Length - 1 ? name : name.Substring(0, namespaceLimit).ToLower());

        return string.Join(".", shortenedNamespaces).PadRight(loggerNameLimit).Substring(0, loggerNameLimit);
    }

    private static LoggingConfiguration GetDefaultConfiguration(string logPath)
    {
        var path = Path.Combine(logPath, "dadg-${shortdate}.log");
        var config = new LoggingConfiguration();
        var logfile = new FileTarget("logfile")
        {
            FileName = path,
            Layout = "[${date:format=yyyy-MM-dd HH\\:mm\\:ss} | ${uppercase:${level:format=FirstCharacter}} | ${logger}] ${message}${exception:format=tostring}"
        };
#if DEBUG
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
#else
        config.AddRule(LogLevel.Warn, LogLevel.Fatal, logfile);
#endif
        return config;
    }
}

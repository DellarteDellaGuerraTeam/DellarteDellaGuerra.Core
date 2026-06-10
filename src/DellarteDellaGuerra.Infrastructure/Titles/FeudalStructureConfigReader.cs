using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Titles.Model;
using DellarteDellaGuerra.Infrastructure.Utils;

namespace DellarteDellaGuerra.Infrastructure.Titles;

/**
 * <summary>
 * Reads the feudal titles configuration file from the mod's config folder.
 * <br/>
 * <br/>
 * The configuration file is expected to be named "titles.config.xml" and should be located
 * in the mod's root config folder.
 * </summary>
 */
public class FeudalStructureConfigReader
{
    private const string ConfigFileName = "titles.config.xml";

    private readonly ILogger _logger;
    private readonly Func<string?> _configFilePathProvider;

    public FeudalStructureConfigReader(ILoggerFactory loggerFactory)
        : this(loggerFactory, () => ResourceLocator.GetConfigurationFilePath(ConfigFileName))
    {
    }

    internal FeudalStructureConfigReader(ILoggerFactory loggerFactory, Func<string?> configFilePathProvider)
    {
        _logger = loggerFactory.CreateLogger<FeudalStructureConfigReader>();
        _configFilePathProvider = configFilePathProvider;
    }

    /**
     * <summary>
     * Loads the configured de jure feudal hierarchy.
     * </summary>
     * <returns>
     * The parsed feudal title forest, or an empty list if the config file is missing or invalid.
     * </returns>
     */
    internal IReadOnlyList<FeudalTitleNode> Load()
    {
        string? configFilePath = _configFilePathProvider();
        if (configFilePath is null)
        {
            _logger.Warn($"Could not find config file {ConfigFileName}");
            return Array.Empty<FeudalTitleNode>();
        }

        try
        {
            using var stream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read);
            return FeudalStructureParser.Parse(stream);
        }
        catch (InvalidOperationException e)
        {
            _logger.Error($"Failed to parse config file {ConfigFileName}: {e.Message}", e);
        }
        catch (XmlException e)
        {
            _logger.Error($"Failed to parse config file {ConfigFileName}: {e.Message}", e);
        }
        catch (IOException e)
        {
            _logger.Error($"Failed to read config file {ConfigFileName}", e);
        }

        return Array.Empty<FeudalTitleNode>();
    }

    // Creates and populates an XmlFeudalStructure from the configured titles file.
    public XmlFeudalStructure CreateStructure()
    {
        return new XmlFeudalStructure(Load());
    }
}

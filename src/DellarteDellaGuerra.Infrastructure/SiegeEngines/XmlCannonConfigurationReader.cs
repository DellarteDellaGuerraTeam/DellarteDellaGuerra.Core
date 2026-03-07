using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.SiegeEngines.Model;
using DellarteDellaGuerra.Domain.SiegeEngines.Port;
using DellarteDellaGuerra.Infrastructure.Utils;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class XmlCannonConfigurationReader : ICannonConfigurationReader
{
    private readonly ILogger _logger;

    public XmlCannonConfigurationReader(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<XmlCannonConfigurationReader>();
    }

    public IEnumerable<CannonProperties> LoadCannonProperties()
    {
        string? configPath = ResourceLocator.GetCannonXmlFilePath();

        if (configPath is null)
        {
            _logger.Error($"Could not find any xml for cannons.");
            return new List<CannonProperties>();
        }

        var cannonProperties = XDocument.Load(configPath).Descendants("CannonType")
            .Select(CreateCannonProperties).ToList();

        foreach (var cannon in cannonProperties) _logger.Debug($"Loaded '{cannon.Id}' cannon");

        return cannonProperties;
    }

    private static CannonProperties CreateCannonProperties(XElement element) =>
        new(
            element.Element("Id")?.Value ?? "falconet",
            element.Element("DisplayName")?.Value ?? "Falconet",
            element.Element("SpriteId")?.Value ?? "falconet",
            element.Element("MapPrefabName")?.Value ?? "dadg_falconet_mapicon",
            element.Element("ProjectilePrefab")?.Value ?? "cannonball_mapicon_projectile",
            element.Element("ReloadPrefab")?.Value ?? "ballista_a_mapicon_reload",
            element.Element("FirePrefab")?.Value ?? "ballista_a_mapicon_fire",
            int.TryParse(element.Element("MachineType")?.Value, out var machineType) ? machineType : 8,
            int.TryParse(element.Element("ProjectileBoneIndex")?.Value, out var boneIndex) ? boneIndex : 0
        );
}

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
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

    public IEnumerable<Domain.SiegeEngines.Model.Cannon> LoadCannons()
    {
        string? configPath = ResourceLocator.GetCannonXmlFilePath();

        if (configPath is null)
        {
            _logger.Error($"Could not find any xml for cannons.");
            return new List<Domain.SiegeEngines.Model.Cannon>();
        }

        var cannons = XDocument.Load(configPath).Descendants("Cannon")
            .Select(CreateCannons).ToList();

        foreach (var cannon in cannons) _logger.Debug($"Loaded '{cannon.Id}' cannon");

        return cannons;
    }

    private static Domain.SiegeEngines.Model.Cannon CreateCannons(XElement element)
    {
        return new Domain.SiegeEngines.Model.Cannon(
            element.Element("Id")?.Value ?? "falconet",
            element.Element("DisplayName")?.Value ?? "Falconet",
            element.Element("SiegeDeploymentSelectionIconSpriteId")?.Value ?? string.Empty,
            element.Element("MapSiegeMarkerSpriteId")?.Value ?? string.Empty,
            element.Element("CampaignMapSelectionIconSpriteId")?.Value ?? string.Empty,
            element.Element("CampaignMapPrefabName")?.Value ?? "dadg_falconet_mapicon",
            element.Element("CampaignMapProjectilePrefabName")?.Value ?? "cannonball_mapicon_projectile",
            element.Element("CampaignMapReloadAnimationName")?.Value ?? "ballista_a_mapicon_reload",
            element.Element("CampaignMapFireAnimationName")?.Value ?? "ballista_a_mapicon_fire",
            int.TryParse(element.Element("MachineType")?.Value, out var machineType) ? machineType : 8,
            int.TryParse(element.Element("CampaignMapProjectileBoneIndex")?.Value, out var boneIndex) ? boneIndex : 0
        );
    }
}

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
            element.Element("Id")?.Value,
            element.Element("DisplayName")?.Value,
            element.Element("SiegeDeploymentSelectionIconSpriteId")?.Value,
            element.Element("MapSiegeMarkerSpriteId")?.Value,
            element.Element("CampaignMapSelectionIconSpriteId")?.Value,
            element.Element("CampaignMapPrefabName")?.Value,
            element.Element("CampaignMapProjectilePrefabName")?.Value,
            element.Element("CampaignMapReloadAnimationName")?.Value,
            element.Element("CampaignMapFireAnimationName")?.Value,
            int.TryParse(element.Element("MachineType")?.Value, out var machineType) ? machineType : -1,
            int.TryParse(element.Element("CampaignMapProjectileBoneIndex")?.Value, out var boneIndex) ? boneIndex : -1
        );
    }
}

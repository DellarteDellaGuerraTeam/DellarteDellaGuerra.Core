using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.SiegeEngines.Model;
using DellarteDellaGuerra.Domain.SiegeEngines.Port;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class XmlCannonConfiguration : ICannonConfiguration
{
    private readonly string _configPath;

    public XmlCannonConfiguration(string configPath)
    {
        _configPath = configPath;
    }

    public IEnumerable<CannonProperties> LoadCannonProperties()
    {
        try
        {
            var doc = XDocument.Load(_configPath);
            return doc.Descendants("CannonType")
                .Select(CreateCannonProperties);
        }
        catch
        {
            // If config file doesn't exist or is invalid, return default Falconet
            return new List<CannonProperties>
            {
                new(
                    "falconet",
                    "Falconet",
                    "falconet",
                    "dadg_falconet_mapicon",
                    "cannonball_mapicon_projectile",
                    "ballista_a_mapicon_reload",
                    "ballista_a_mapicon_fire",
                    8,
                    0
                )
            };
        }
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

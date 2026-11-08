using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Combat;
using DellarteDellaGuerra.Domain.Combat.Port;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Utils;

namespace DellarteDellaGuerra.Infrastructure.Combat.Spi
{
    /// <summary>
    /// Loads weapon tiers, armour materials and the combat rules from the first
    /// ModuleData/CustomXml/dadg_combat_items.xml found among DADG's own modules.
    /// </summary>
    public class CombatItemXmlRepository : ICombatItemRepository
    {
        private readonly ILogger _logger;

        public CombatItemXmlRepository(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CombatItemXmlRepository>();
        }

        public CombatItems Load()
        {
            var path = ResourceLocator.GetCombatItemsXmlFilePath();
            if (path == null)
            {
                _logger.Warn("dadg_combat_items.xml not found in any module's ModuleData/CustomXml folder");
                return CombatItems.Empty;
            }

            try
            {
                var document = XDocument.Load(path);
                var tiers = new Dictionary<string, int>();
                foreach (var node in document.Descendants("Tier"))
                    tiers[(string)node.Attribute("id")] = (int)node.Attribute("tier");

                var armours = new Dictionary<string, ArmourPiece>();
                foreach (var node in document.Descendants("Armour"))
                    armours[(string)node.Attribute("id")] = new ArmourPiece(ParseMaterials(node));

                return new CombatItems(tiers, armours, ParseAbsorption(document), ParseHalfSword(document),
                    ParseNonTipThrust(document));
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to load {path}: {e.Message}");
                return CombatItems.Empty;
            }
        }

        // Absorption / material / blow / Default or weapon class; leaf values are final-damage fractions.
        private static ArmourAbsorption ParseAbsorption(XDocument document)
        {
            var fractions = new Dictionary<(ArmourMaterial, BlowType, CombatWeaponClass), float>();
            var rules = document.Root?.Element("Absorption");
            if (rules == null) return ArmourAbsorption.None;
            if (rules.DescendantsAndSelf().Any(node => node.HasAttributes)
                || document.Root!.Elements("Absorption").Skip(1).Any())
                throw new FormatException("Use one nested Absorption/Material/Blow/WeaponClass tree, not attribute rows.");

            foreach (var material in rules.Elements())
            {
                var materialType = (ArmourMaterial)Enum.Parse(typeof(ArmourMaterial), material.Name.LocalName);
                foreach (var blow in material.Elements())
                {
                    var blowType = (BlowType)Enum.Parse(typeof(BlowType), blow.Name.LocalName);
                    if (!blow.HasElements)
                        throw new FormatException($"{material.Name}/{blow.Name} requires Default or weapon-class elements.");
                    foreach (var weaponClass in blow.Elements())
                    {
                        if (weaponClass.HasElements)
                            throw new FormatException($"{weaponClass.Name} must contain a numeric absorption fraction.");
                        fractions.Add((materialType, blowType,
                                (CombatWeaponClass)Enum.Parse(typeof(CombatWeaponClass), weaponClass.Name.LocalName)),
                            (float)weaponClass);
                    }
                }
            }

            return new ArmourAbsorption(fractions);
        }

        private static HalfSword ParseHalfSword(XDocument document)
        {
            var node = document.Descendants("HalfSword").FirstOrDefault();
            return node == null ? HalfSword.None : new HalfSword((float)node.Attribute("swing"));
        }

        private static NonTipThrust ParseNonTipThrust(XDocument document)
        {
            var multipliers = new Dictionary<string, float>();
            foreach (var node in document.Descendants("NonTipThrust"))
                multipliers[(string)node.Attribute("template")] = (float)node.Attribute("multiplier");

            return new NonTipThrust(multipliers);
        }

        // One attribute per HitLocation a piece covers, named after it.
        private static Dictionary<HitLocation, ArmourMaterial> ParseMaterials(XElement node)
        {
            var materials = new Dictionary<HitLocation, ArmourMaterial>();
            foreach (HitLocation location in Enum.GetValues(typeof(HitLocation)))
            {
                var attribute = node.Attribute(location.ToString().ToLowerInvariant());
                if (attribute != null)
                    materials[location] = (ArmourMaterial)Enum.Parse(typeof(ArmourMaterial), attribute.Value);
            }

            return materials;
        }
    }
}

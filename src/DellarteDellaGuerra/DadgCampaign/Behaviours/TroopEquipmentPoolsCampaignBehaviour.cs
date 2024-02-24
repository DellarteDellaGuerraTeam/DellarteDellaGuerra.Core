using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using DellarteDellaGuerra.Logging;
using DellarteDellaGuerra.Utils;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.DadgCampaign.Behaviours;

/**
 * <summary>
 * This class provides a way to store the equipment pools for troops.
 * </summary>
 * <remarks>
 * It reads the characters from the xml after xsl transformation to get the equipment pools for each troop.
 * <br/>
 * <br/>
 * An equipment pool is a collection of equipments defined by its pool attribute.
 * The pool attribute is optional and defaults to 0 if not specified.
 * <br/>
 * <br/>
 * Equipments with the same pool attribute are grouped together to form an equipment pool.
 * </remarks>
 */
public class TroopEquipmentPoolsCampaignBehaviour : CampaignBehaviorBase
{
    private static readonly Logger Logger = LoggerFactory.GetLogger<TroopEquipmentPoolsCampaignBehaviour>(); 
    private readonly Dictionary<string, List<MBEquipmentRoster>> _troopTypeEquipmentPools = new ();

    public override void RegisterEvents()
    {
        CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, ReadAllTroopEquipmentPools);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    /**
     * <summary>
     * Get the equipment pools for a troop.
     * </summary>
     * <param name="character">The troop character</param>
     * <returns>The equipment pools for the troop</returns>
     * <remarks>
     * If the character is a hero or a non combatant character, then an empty list is returned.
     * If the character is null or the character string id is null or empty, then an empty list is returned.
     * </remarks>
     */
    public List<MBEquipmentRoster> GetTroopEquipmentPools(BasicCharacterObject character)
    {
        if (string.IsNullOrWhiteSpace(character?.StringId))
        {
            Logger.Debug("The character string id is null or empty. {0}",  Environment.StackTrace);
            return new List<MBEquipmentRoster>();
        }

        if (!_troopTypeEquipmentPools.ContainsKey(character!.StringId))
        {
            Logger.Warn("The character string id is not in the equipment pools. {0}",  character.StringId);
            return new List<MBEquipmentRoster>();
        }

        return _troopTypeEquipmentPools[character.StringId];
    }

    private void ReadAllTroopEquipmentPools()
    {
        var npcCharactersNode = MBObjectManager.GetMergedXmlForManaged("NPCCharacters", false);

        Campaign.Current.Characters.Where(character => 
            character.Occupation == Occupation.Soldier || 
            character.Occupation == Occupation.Mercenary || 
            character.Occupation == Occupation.Bandit || 
            character.Occupation ==  Occupation.CaravanGuard || 
            character.Occupation == Occupation.Villager && 
            character.Occupation != Occupation.Special && 
            character.Occupation != Occupation.NotAssigned &&
            !character.IsTemplate &&
            !character.IsChildTemplate &&
            !character.IsObsolete 
        )
        .ToList()
        .ForEach(soldier =>
        {
            string troopId = soldier.StringId;
            if (!_troopTypeEquipmentPools.ContainsKey(troopId))
            {
                _troopTypeEquipmentPools[troopId] = ReadTroopEquipmentPools(npcCharactersNode, troopId);
            }
        });
    }

    private List<MBEquipmentRoster> ReadTroopEquipmentPools(XmlNode npcCharactersNodes, string troopId)
    {
        var groupedEquipment = new Dictionary<int, MBEquipmentRoster>();
        List<XmlNode> equipmentRosterNodes = GetAllEquipmentRosterNodes(npcCharactersNodes, troopId);
        foreach (XmlNode equipmentRosterNode in equipmentRosterNodes)
        {
            string? uncheckedPool = equipmentRosterNode.Attributes?["pool"]?.Value?.Trim();
            // default pool is 0 if not specified
            int.TryParse(uncheckedPool, out var poolId);
            if (!groupedEquipment.ContainsKey(poolId))
            {
                groupedEquipment[poolId] = new MBEquipmentRoster();
            }

            AddReferencedEquipmentsToPool(equipmentRosterNode, groupedEquipment[poolId]);
            AddExplicitEquipmentToPool(equipmentRosterNode, groupedEquipment[poolId]);
        }

        // return list of equipment rosters as the poolId has no use case upstream
        return groupedEquipment.Select(groupedRoster => groupedRoster.Value).ToList();
    }

    private List<XmlNode> GetAllEquipmentRosterNodes(XmlNode npcCharactersNodes, string troopId)
    {
        string xpath = TroopEquipmentXPath(troopId).Build();
        var troopEquipmentNodes = npcCharactersNodes.SelectNodes(xpath);
        return troopEquipmentNodes is null ? new List<XmlNode>() : troopEquipmentNodes.Cast<XmlNode>().ToList();
    }

    private void AddReferencedEquipmentsToPool(XmlNode referencedEquipmentNode, MBEquipmentRoster equipmentRoster)
    {
        if (!referencedEquipmentNode.Name.Equals("EquipmentSet", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        // civilian is optional, default is false -> equipment for battle
        bool.TryParse(referencedEquipmentNode.Attributes?["civilian"]?.Value, out var isCivilian);
        var id = referencedEquipmentNode.Attributes?["id"]?.Value;
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var referencedId = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(id);
        if (referencedId is not null)
        {
            // add all referenced equipments from the EquipmentSet node to the roster
            equipmentRoster.AddEquipmentRoster(referencedId, isCivilian);
        }
    }

    private void AddExplicitEquipmentToPool(XmlNode equipmentRosterNode, MBEquipmentRoster equipmentRoster)
    {
        if (equipmentRosterNode.Name.Equals("EquipmentRoster", StringComparison.InvariantCultureIgnoreCase))
        {
            equipmentRoster.Init(MBObjectManager.Instance, equipmentRosterNode);
        }
    }

    private XPathBuilder TroopXPath(string troopId)
    {
        return new XPathBuilder().WithRoot("NPCCharacters")
            .WithChildNode("NPCCharacter")
            .WithAttribute("id", troopId);
    }

    private XPathBuilder TroopEquipmentXPath(string troopId)
    {
        return TroopXPath(troopId)
            .WithChildNode("Equipments")
            .WithChildNodes("EquipmentRoster", "equipmentRoster", "EquipmentSet", "equipmentSet");
    }
}

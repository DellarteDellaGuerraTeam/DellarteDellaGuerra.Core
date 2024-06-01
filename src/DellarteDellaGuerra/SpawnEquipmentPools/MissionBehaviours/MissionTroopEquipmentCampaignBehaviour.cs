using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Logging.Port;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment;
using DellarteDellaGuerra.SpawnEquipmentPools.Mappers;
using DellarteDellaGuerra.SpawnEquipmentPools.MissionLogic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.SpawnEquipmentPools.MissionBehaviours
{
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
    public class MissionTroopEquipmentCampaignBehaviour : CampaignBehaviorBase, IMissionTroopEquipmentProvider
    {
        private readonly ILogger _logger;

        private readonly ListBattleEquipment _listBattleEquipment;
        private readonly EquipmentPoolMapper _equipmentPoolMapper;
        
        private IReadOnlyDictionary<string, IReadOnlyCollection<MBEquipmentRoster>> _troopTypeBattleEquipmentPools;

        public MissionTroopEquipmentCampaignBehaviour(
            ILoggerFactory loggerFactory,
            ListBattleEquipment listBattleEquipment,
            EquipmentPoolMapper equipmentPoolMapper)
        {
            _logger = loggerFactory.CreateLogger<MissionTroopEquipmentCampaignBehaviour>();
            _listBattleEquipment = listBattleEquipment;
            _equipmentPoolMapper = equipmentPoolMapper;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, ReadAllTroopEquipmentPools);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, _ => ReadAllTroopEquipmentPools());
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
        public IEnumerable<MBEquipmentRoster> GetTroopEquipmentPools(BasicCharacterObject character)
        {
            if (string.IsNullOrWhiteSpace(character?.StringId))
            {
                _logger.Debug($"The character string id is null or empty. {Environment.StackTrace}");
                return new List<MBEquipmentRoster>();
            }

            if (!_troopTypeBattleEquipmentPools.ContainsKey(character!.StringId))
            {
                _logger.Warn($"The character string id {character.StringId} is not in the equipment pools.");
                return new List<MBEquipmentRoster>();
            }

            return _troopTypeBattleEquipmentPools[character.StringId];
        }

        private void ReadAllTroopEquipmentPools()
        {
            _troopTypeBattleEquipmentPools = _listBattleEquipment.ListEquipment()
                .ToDictionary(
                    characterEquipmentPools => characterEquipmentPools.Key,
                    characterEquipmentPools => _equipmentPoolMapper.MapEquipmentPool(characterEquipmentPools.Value)
                );
        }
    }
}

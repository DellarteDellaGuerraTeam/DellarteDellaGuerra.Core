using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Logging;
using DellarteDellaGuerra.Missions.MissionLogics.SpawnLogic;
using DellarteDellaGuerra.Xml.Characters.Mappers;
using DellarteDellaGuerra.Xml.Characters.Repositories;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.DadgCampaign.Behaviours
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
        private static readonly Logger Logger = LoggerFactory.GetLogger<MissionTroopEquipmentCampaignBehaviour>();

        private readonly CharacterEquipmentRepository _characterEquipmentRepository;
        private readonly EquipmentPoolMapper _equipmentPoolMapper;
        
        private IReadOnlyDictionary<string, IReadOnlyCollection<MBEquipmentRoster>> _troopTypeBattleEquipmentPools;

        public MissionTroopEquipmentCampaignBehaviour(
            CharacterEquipmentRepository characterEquipmentRepository,
            EquipmentPoolMapper equipmentPoolMapper)
        {
            _characterEquipmentRepository = characterEquipmentRepository;
            _equipmentPoolMapper = equipmentPoolMapper;
        }
        
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
        public IEnumerable<MBEquipmentRoster> GetTroopEquipmentPools(BasicCharacterObject character)
        {
            if (string.IsNullOrWhiteSpace(character?.StringId))
            {
                Logger.Debug("The character string id is null or empty. {0}",  Environment.StackTrace);
                return new List<MBEquipmentRoster>();
            }

            if (!_troopTypeBattleEquipmentPools.ContainsKey(character!.StringId))
            {
                Logger.Warn("The character string id {0} is not in the equipment pools.",  character.StringId);
                return new List<MBEquipmentRoster>();
            }

            return _troopTypeBattleEquipmentPools[character.StringId];
        }

        private void ReadAllTroopEquipmentPools()
        {
            _troopTypeBattleEquipmentPools =  _characterEquipmentRepository.FindEquipmentPoolsByCharacter().Select(
                equipmentPools => new KeyValuePair<string, IReadOnlyCollection<MBEquipmentRoster>>(
                    equipmentPools.Key,
                    _equipmentPoolMapper.MapEquipmentPool(equipmentPools.Value)
                )
            ).ToDictionary(equipmentPools => equipmentPools.Key, equipmentPools => equipmentPools.Value);
            // Kingdom.All.ForEach(k => k.Banner.Deserialize("11.127.145.1956.1843.764.764.0.0.0.505.12.127.752.534.528.439.0.0.90.505.12.127.752.534.988.1087.0.0.90.505.145.127.752.534.529.1086.0.0.90.505.145.127.752.534.988.440.0.0.90.460.127.127.371.282.2071.1014.1.0.90.141.143.127.282.273.616.1052.0.0.0.141.143.127.282.273.616.943.0.0.0.141.143.127.282.273.616.834.0.0.0.424.143.127.193.193.989.871.0.0.0.424.143.127.193.193.845.871.0.0.0.424.143.127.193.193.917.1000.0.0.0.424.143.127.193.193.687.529.0.0.0.424.143.127.193.193.543.529.0.0.0.424.143.127.193.193.615.658.0.0.0.141.143.127.282.273.914.694.0.0.0.141.143.127.282.273.914.585.0.0.0.141.143.127.282.273.914.476.0.0.0.460.127.127.371.282.2071.1014.1.0.90.505.12.127.371.1779.754.234.0.0.-90.505.145.127.371.889.1140.235.0.0.-90.505.145.127.371.1779.761.1295.0.0.-90.505.145.127.484.1779.255.1019.0.0.0.505.145.127.484.1779.1270.1043.0.0.0.460.127.127.371.282.2071.1014.1.0.90.513.116.127.90.87.231.632.0.0.0.505.12.127.484.1779.256.989.0.0.0.505.142.127.484.1779.-43.1001.0.0.0.515.84.143.98.37.222.608.1.0.113.515.84.143.98.37.222.608.1.0.68.515.84.143.98.37.222.608.1.1.338.515.84.143.98.37.222.608.1.0.23.515.84.143.122.46.222.608.1.0.90.515.84.143.122.46.222.608.1.0.45.515.84.143.122.46.222.608.1.1.315.515.84.143.122.46.222.608.1.0.0.503.40.116.25.8.222.582.1.0.-180.514.40.140.33.20.222.589.0.0.0.425.22.140.20.26.222.591.1.1.90.427.150.140.11.6.222.586.1.1.0.427.150.140.6.3.229.584.1.0.180.427.150.140.6.3.214.584.1.0.180.503.40.116.25.8.196.601.1.0.-106.514.40.140.33.20.202.602.0.0.74.425.22.140.20.26.205.603.1.1.165.427.150.140.11.6.199.601.1.1.74.427.150.140.6.3.200.594.1.0.254.427.150.140.6.3.195.608.1.0.254.503.40.116.25.8.237.629.1.0.36.514.40.140.33.20.233.623.0.0.-144.425.22.140.20.26.231.621.1.0.126.427.150.140.11.6.235.626.1.0.217.427.150.140.6.3.242.623.1.1.37.427.150.140.6.3.230.632.1.1.37.503.40.116.25.8.246.600.1.1.105.514.40.140.33.20.240.602.0.1.285.425.22.140.20.26.237.603.1.0.194.427.150.140.11.6.243.601.1.0.285.427.150.140.6.3.243.593.1.1.105.427.150.140.6.3.247.608.1.1.105.503.40.116.25.8.205.629.1.1.324.514.40.140.33.20.210.623.0.1.144.425.22.140.20.26.212.621.1.1.233.427.150.140.11.6.208.626.1.1.143.427.150.140.6.3.201.623.1.0.323.427.150.140.6.3.212.632.1.0.323.506.40.116.26.13.210.593.0.1.35.219.126.116.7.8.203.583.1.1.35.218.126.116.6.12.207.589.1.1.35.506.40.116.26.13.233.593.0.0.325.219.126.116.7.8.240.583.1.0.325.218.126.116.6.12.236.589.1.0.325.506.40.116.26.13.239.614.0.0.253.219.126.116.7.8.251.617.1.0.253.218.126.116.6.12.244.615.1.0.253.506.40.116.26.13.204.614.0.1.106.219.126.116.7.8.192.617.1.1.106.218.126.116.6.12.198.615.1.1.106.506.40.116.26.13.222.626.0.1.181.219.126.116.7.8.222.638.1.1.181.503.116.116.14.4.222.623.0.0.0.503.116.116.14.4.236.612.1.0.-286.503.116.116.14.4.212.595.1.0.-144.503.116.116.14.4.206.613.1.1.-75.503.116.116.14.4.230.595.1.1.144.218.126.116.6.13.222.632.1.1.181.220.126.131.23.25.221.607.1.0.0.514.140.116.19.11.222.619.1.0.-180.503.140.116.14.4.222.623.0.0.0.425.22.140.11.14.222.618.1.1.-90.427.150.149.6.3.222.621.1.1.-180.427.150.149.3.2.217.622.1.0.0.427.150.149.3.2.226.621.1.0.0.514.140.116.19.11.232.611.1.0.-106.503.140.116.14.4.236.612.1.0.-286.425.22.140.11.14.231.611.1.1.-15.427.150.116.6.3.235.611.1.1.-106.427.150.116.3.2.234.616.1.0.74.427.150.116.3.2.236.608.1.0.74.514.140.116.19.11.215.598.1.0.-324.503.140.116.14.4.212.595.1.0.-144.425.22.140.11.14.215.601.1.0.-54.427.150.140.6.3.213.597.1.0.37.427.150.140.3.2.208.599.1.1.-143.427.150.140.3.2.216.594.1.1.-143.514.140.116.19.11.210.611.1.1.105.503.140.116.14.4.207.613.1.1.-75.425.22.140.11.14.212.611.1.0.14.427.150.116.6.3.208.612.1.0.105.427.150.116.3.2.208.617.1.1.-75.427.150.116.3.2.206.608.1.1.-75.514.140.116.19.11.228.598.1.1.-36.503.140.116.14.4.230.595.1.1.144.425.22.140.11.14.227.600.1.1.53.427.150.140.6.3.229.597.1.1.-37.427.150.140.3.2.234.598.1.0.143.427.150.140.3.2.226.594.1.0.143.427.140.116.8.10.229.617.0.1.-145.427.140.116.8.10.215.617.0.0.145.427.140.116.8.10.210.604.0.0.73.427.140.116.8.10.232.604.0.1.-74.427.140.116.8.10.221.596.0.1.1.520.131.140.23.23.222.608.1.0.143.503.144.84.21.21.222.608.1.0.143.503.131.131.17.17.222.608.1.0.143.503.84.143.16.16.222.608.1.0.143.503.47.143.16.16.222.608.1.0.143.503.47.5.11.11.222.608.1.0.143.505.142.127.484.1779.1395.917.0.0.0"));
        }
    }
}

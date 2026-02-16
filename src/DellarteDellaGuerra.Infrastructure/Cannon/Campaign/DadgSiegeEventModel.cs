using System.Collections.Generic;
using DellarteDellaGuerra.Domain.SiegeEngines;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.Cannon
{
    public class DadgSiegeEventModel : SiegeEventModel
    {
        private readonly SiegeEventModel _defaultSiegeEventModel;
        private readonly GetDefaultSiegeEngine defaultSiegeEngine;

        public DadgSiegeEventModel(SiegeEventModel defaultSiegeEventModel, GetDefaultSiegeEngine defaultSiegeEngine)
        {
            _defaultSiegeEventModel = defaultSiegeEventModel;
            this.defaultSiegeEngine = defaultSiegeEngine;
        }

        public override int GetSiegeEngineDestructionCasualties(SiegeEvent siegeEvent, BattleSideEnum side,
            SiegeEngineType destroyedSiegeEngine)
        {
            return _defaultSiegeEventModel.GetSiegeEngineDestructionCasualties(siegeEvent, side, destroyedSiegeEngine);
        }

        public override float GetCasualtyChance(MobileParty siegeParty, SiegeEvent siegeEvent, BattleSideEnum side)
        {
            return _defaultSiegeEventModel.GetCasualtyChance(siegeParty, siegeEvent, side);
        }

        public override int GetColleteralDamageCasualties(SiegeEngineType attackerSiegeEngine, MobileParty party)
        {
            return _defaultSiegeEventModel.GetColleteralDamageCasualties(attackerSiegeEngine, party);
        }

        public override float GetSiegeEngineHitChance(SiegeEngineType siegeEngineType, BattleSideEnum battleSide,
            SiegeBombardTargets target,
            Town town)
        {
            return _defaultSiegeEventModel.GetSiegeEngineHitChance(siegeEngineType, battleSide, target, town);
        }

        public override string GetSiegeEngineMapPrefabName(SiegeEngineType siegeEngineType, int wallLevel,
            BattleSideEnum side)
        {
            if (siegeEngineType.StringId == "falconet") return "dadg_falconet_mapicon";
            
            return _defaultSiegeEventModel.GetSiegeEngineMapPrefabName(siegeEngineType, wallLevel, side);
        }

        public override string GetSiegeEngineMapProjectilePrefabName(SiegeEngineType siegeEngineType)
        {
            if (siegeEngineType.StringId == "falconet") return "cannonball_mapicon_projectile";
            
            return _defaultSiegeEventModel.GetSiegeEngineMapProjectilePrefabName(siegeEngineType);
        }

        public override string GetSiegeEngineMapReloadAnimationName(SiegeEngineType siegeEngineType,
            BattleSideEnum side)
        {
            if (siegeEngineType.StringId == "falconet") return "ballista_a_mapicon_reload";
            
            return _defaultSiegeEventModel.GetSiegeEngineMapReloadAnimationName(siegeEngineType, side);
        }

        public override string GetSiegeEngineMapFireAnimationName(SiegeEngineType siegeEngineType, BattleSideEnum side)
        {
            if (siegeEngineType.StringId == "falconet") return "ballista_a_mapicon_fire";
            
            return _defaultSiegeEventModel.GetSiegeEngineMapFireAnimationName(siegeEngineType, side);
        }

        public override sbyte GetSiegeEngineMapProjectileBoneIndex(SiegeEngineType siegeEngineType, BattleSideEnum side)
        {
            if (siegeEngineType.StringId == "falconet")
                return _defaultSiegeEventModel.GetSiegeEngineMapProjectileBoneIndex(DefaultSiegeEngineTypes.Trebuchet,
                    side); // Expect trebuchet mapicon skeleton, TODO: refactor it into a better API
            
            return _defaultSiegeEventModel.GetSiegeEngineMapProjectileBoneIndex(siegeEngineType, side);
        }

        public override float GetSiegeStrategyScore(SiegeEvent siege, BattleSideEnum side, SiegeStrategy strategy)
        {
            return _defaultSiegeEventModel.GetSiegeStrategyScore(siege, side, strategy);
        }

        public override float GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent,
            ISiegeEventSide side)
        {
            return _defaultSiegeEventModel.GetConstructionProgressPerHour(type, siegeEvent, side);
        }

        public override MobileParty GetEffectiveSiegePartyForSide(SiegeEvent siegeEvent, BattleSideEnum side)
        {
            return _defaultSiegeEventModel.GetEffectiveSiegePartyForSide(siegeEvent, side);
        }

        public override float GetAvailableManDayPower(ISiegeEventSide side)
        {
            return _defaultSiegeEventModel.GetAvailableManDayPower(side);
        }

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerRangedSiegeEngines(PartyBase party)
        {
            var siegeEngineTypes =
                new List<SiegeEngineType>(_defaultSiegeEventModel.GetAvailableAttackerRangedSiegeEngines(party));

            var falconetSiegeEngineType = GetDefaultSiegeEngineType();
            if (falconetSiegeEngineType is null) return siegeEngineTypes;

            siegeEngineTypes.Add(falconetSiegeEngineType);
            return siegeEngineTypes;
        }

        public override IEnumerable<SiegeEngineType> GetAvailableDefenderSiegeEngines(PartyBase party)
        {
            var siegeEngineTypes =
                new List<SiegeEngineType>(_defaultSiegeEventModel.GetAvailableDefenderSiegeEngines(party));

            var falconetSiegeEngineType = GetDefaultSiegeEngineType();
            if (falconetSiegeEngineType is null) return siegeEngineTypes;

            siegeEngineTypes.Add(falconetSiegeEngineType);
            return siegeEngineTypes;
        }

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerRamSiegeEngines(PartyBase party)
        {
            return _defaultSiegeEventModel.GetAvailableAttackerRamSiegeEngines(party);
        }

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerTowerSiegeEngines(PartyBase party)
        {
            return new List<SiegeEngineType>();
            // return _defaultSiegeEventModel.GetAvailableAttackerTowerSiegeEngines(party);
        }

        public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSettlement(Settlement settlement)
        {
            return _defaultSiegeEventModel.GetPrebuiltSiegeEnginesOfSettlement(settlement);
        }

        public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSiegeCamp(BesiegerCamp camp)
        {
            return _defaultSiegeEventModel.GetPrebuiltSiegeEnginesOfSiegeCamp(camp);
        }

        public override float GetSiegeEngineHitPoints(SiegeEvent siegeEvent, SiegeEngineType siegeEngine,
            BattleSideEnum battleSide)
        {
            return _defaultSiegeEventModel.GetSiegeEngineHitPoints(siegeEvent, siegeEngine, battleSide);
        }

        public override int GetRangedSiegeEngineReloadTime(SiegeEvent siegeEvent, BattleSideEnum side,
            SiegeEngineType siegeEngine)
        {
            return _defaultSiegeEventModel.GetRangedSiegeEngineReloadTime(siegeEvent, side, siegeEngine);
        }

        public override float GetSiegeEngineDamage(SiegeEvent siegeEvent, BattleSideEnum battleSide,
            SiegeEngineType siegeEngine,
            SiegeBombardTargets target)
        {
            return _defaultSiegeEventModel.GetSiegeEngineDamage(siegeEvent, battleSide, siegeEngine, target);
        }

        public override FlattenedTroopRoster GetPriorityTroopsForSallyOutAmbush()
        {
            return _defaultSiegeEventModel.GetPriorityTroopsForSallyOutAmbush();
        }

        private SiegeEngineType? GetDefaultSiegeEngineType()
        {
            return MBObjectManager.Instance.GetObject<SiegeEngineType>(defaultSiegeEngine.GetSiegeEngine().Id);
        }
    }
}
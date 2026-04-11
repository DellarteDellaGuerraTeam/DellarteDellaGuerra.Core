using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Campaign
{
    public class DadgSiegeEventModel : SiegeEventModel
    {
        private readonly SiegeEventModel _previous;

        public DadgSiegeEventModel(SiegeEventModel previous)
        {
            _previous = previous;
        }

        private static IEnumerable<SiegeEngineType> GetUnwantedSiegeEngines() =>
            new List<SiegeEngineType>
            {
                DefaultSiegeEngineTypes.SiegeTower,
                DefaultSiegeEngineTypes.HeavySiegeTower,
                DefaultSiegeEngineTypes.Ballista,
                DefaultSiegeEngineTypes.FireBallista,
                DefaultSiegeEngineTypes.FireOnager,
                DefaultSiegeEngineTypes.Onager,
                DefaultSiegeEngineTypes.Catapult,
                DefaultSiegeEngineTypes.FireCatapult
            };

        public override int GetSiegeEngineDestructionCasualties(SiegeEvent siegeEvent, BattleSideEnum side,
            SiegeEngineType destroyedSiegeEngine) =>
            _previous.GetSiegeEngineDestructionCasualties(siegeEvent, side, destroyedSiegeEngine);

        public override float GetCasualtyChance(MobileParty siegeParty, SiegeEvent siegeEvent, BattleSideEnum side) =>
            _previous.GetCasualtyChance(siegeParty, siegeEvent, side);

        public override int GetColleteralDamageCasualties(SiegeEngineType attackerSiegeEngine, MobileParty party) =>
            _previous.GetColleteralDamageCasualties(attackerSiegeEngine, party);

        public override float GetSiegeEngineHitChance(SiegeEngineType siegeEngineType, BattleSideEnum battleSide,
            SiegeBombardTargets target, Town town) =>
            _previous.GetSiegeEngineHitChance(siegeEngineType, battleSide, target, town);

        public override string GetSiegeEngineMapPrefabName(SiegeEngineType siegeEngineType, int wallLevel,
            BattleSideEnum side) =>
            _previous.GetSiegeEngineMapPrefabName(siegeEngineType, wallLevel, side);

        public override string GetSiegeEngineMapProjectilePrefabName(SiegeEngineType siegeEngineType) =>
            _previous.GetSiegeEngineMapProjectilePrefabName(siegeEngineType);

        public override string GetSiegeEngineMapReloadAnimationName(SiegeEngineType siegeEngineType,
            BattleSideEnum side) =>
            _previous.GetSiegeEngineMapReloadAnimationName(siegeEngineType, side);

        public override string GetSiegeEngineMapFireAnimationName(SiegeEngineType siegeEngineType, BattleSideEnum side) =>
            _previous.GetSiegeEngineMapFireAnimationName(siegeEngineType, side);

        public override sbyte GetSiegeEngineMapProjectileBoneIndex(SiegeEngineType siegeEngineType, BattleSideEnum side) =>
            _previous.GetSiegeEngineMapProjectileBoneIndex(siegeEngineType, side);

        public override float GetSiegeStrategyScore(SiegeEvent siege, BattleSideEnum side, SiegeStrategy strategy) =>
            _previous.GetSiegeStrategyScore(siege, side, strategy);

        public override float GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent,
            ISiegeEventSide side) =>
            _previous.GetConstructionProgressPerHour(type, siegeEvent, side);

        public override MobileParty GetEffectiveSiegePartyForSide(SiegeEvent siegeEvent, BattleSideEnum side) =>
            _previous.GetEffectiveSiegePartyForSide(siegeEvent, side);

        public override float GetAvailableManDayPower(ISiegeEventSide side) =>
            _previous.GetAvailableManDayPower(side);

        // Filter out native siege engines; cannons are already included by CannonSiegeEventModel (_previous)
        public override IEnumerable<SiegeEngineType> GetAvailableAttackerRangedSiegeEngines(PartyBase party) =>
            _previous.GetAvailableAttackerRangedSiegeEngines(party)
                .Where(e => !GetUnwantedSiegeEngines().Contains(e));

        public override IEnumerable<SiegeEngineType> GetAvailableDefenderSiegeEngines(PartyBase party) =>
            _previous.GetAvailableDefenderSiegeEngines(party)
                .Where(e => !GetUnwantedSiegeEngines().Contains(e));

        public override IEnumerable<SiegeEngineType> GetAvailableAttackerRamSiegeEngines(PartyBase party) =>
            _previous.GetAvailableAttackerRamSiegeEngines(party);

        // DADG: no siege towers
        public override IEnumerable<SiegeEngineType> GetAvailableAttackerTowerSiegeEngines(PartyBase party) =>
            new List<SiegeEngineType>();

        // Replace prebuilt native engines with the first available cannon from _previous
        public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSettlement(Settlement settlement)
        {
            var defaultCannon = _previous.GetAvailableDefenderSiegeEngines(null)
                .FirstOrDefault();

            if (defaultCannon is null)
                return _previous.GetPrebuiltSiegeEnginesOfSettlement(settlement)
                    .Where(e => !GetUnwantedSiegeEngines().Contains(e));

            return _previous.GetPrebuiltSiegeEnginesOfSettlement(settlement)
                .Select(e => GetUnwantedSiegeEngines().Contains(e) ? defaultCannon : e);
        }

        public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSiegeCamp(BesiegerCamp camp)
        {
            var defaultCannon = _previous.GetAvailableAttackerRangedSiegeEngines(camp.LeaderParty.Party)
                .FirstOrDefault();

            if (defaultCannon is null)
                return _previous.GetPrebuiltSiegeEnginesOfSiegeCamp(camp)
                    .Where(e => !GetUnwantedSiegeEngines().Contains(e));

            return _previous.GetPrebuiltSiegeEnginesOfSiegeCamp(camp)
                .Select(e => GetUnwantedSiegeEngines().Contains(e) ? defaultCannon : e);
        }

        public override float GetSiegeEngineHitPoints(SiegeEvent siegeEvent, SiegeEngineType siegeEngine,
            BattleSideEnum battleSide) =>
            _previous.GetSiegeEngineHitPoints(siegeEvent, siegeEngine, battleSide);

        public override int GetRangedSiegeEngineReloadTime(SiegeEvent siegeEvent, BattleSideEnum side,
            SiegeEngineType siegeEngine) =>
            _previous.GetRangedSiegeEngineReloadTime(siegeEvent, side, siegeEngine);

        public override float GetSiegeEngineDamage(SiegeEvent siegeEvent, BattleSideEnum battleSide,
            SiegeEngineType siegeEngine, SiegeBombardTargets target) =>
            _previous.GetSiegeEngineDamage(siegeEvent, battleSide, siegeEngine, target);

        public override FlattenedTroopRoster GetPriorityTroopsForSallyOutAmbush() =>
            _previous.GetPriorityTroopsForSallyOutAmbush();
    }
}

using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;
using DellarteDellaGuerra.Domain.PrivateWars.Port;
using DellarteDellaGuerra.PrivateWars.Spi;
using DellarteDellaGuerra.Titles.Api.Campaign;
using DellarteDellaGuerra.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.PrivateWars.Api.Campaign
{
    /**
     * <summary>
     *  Owns the registry lifecycle and the war's prosecution drive. Persists active and concluded
     *  wars through Bannerlord saves and restores them on load; and, because the engine never plans
     *  a same-kingdom siege on its own (design §5, risk #1), injects the war goal as a scored besiege
     *  candidate into each attacker party's AI think so it competes in the engine's normal behaviour
     *  vote — losing, by design, to higher priorities such as defending the clan's own threatened
     *  fief. The retention model keeps an in-progress siege sticky. Each day it also advances every
     *  active war's score from current world state and, once a side crosses ±100, resolves it:
     *  reverting captured fiefs to their pre-war owners (status quo ante) and leaving the attacker
     *  its goal as the prize.
     * </summary>
     */
    public class PrivateWarCampaignBehavior : CampaignBehaviorBase
    {
        // Score injected into the AI behaviour vote for a private-war attacker's besiege of the goal.
        // AiPartyThinkBehavior applies the maximum-scoring candidate, so this must beat ordinary /
        // opportunistic attacks (~3-15) yet stay below an active defence of the clan's own fief
        // (~15-30; defence's base factor is 1.28 vs a siege's 0.8) so defending one's title wins.
        // Flat tuning value — revisit via AI-vs-AI playtest.
        private const float PrivateWarSiegeSelectionScore = 12f;

        private readonly IFeudalStateStore _stateStore;
        private readonly IPrivateWarRepository _privateWars;
        private readonly ITickPrivateWarUseCase _tickPrivateWar;
        private readonly IResolvePrivateWarUseCase _resolvePrivateWar;
        private readonly IApplyBattleOutcomeUseCase _applyBattleOutcome;
        private readonly WarSideResolver _sideResolver;
        private readonly IFeudalHierarchy _hierarchy;

        private List<string> _serialisedWars = new();

        public PrivateWarCampaignBehavior(
            IFeudalStateStore stateStore,
            IPrivateWarRepository privateWars,
            ITickPrivateWarUseCase tickPrivateWar,
            IResolvePrivateWarUseCase resolvePrivateWar,
            IApplyBattleOutcomeUseCase applyBattleOutcome,
            WarSideResolver sideResolver,
            IFeudalHierarchy hierarchy)
        {
            _stateStore = stateStore;
            _privateWars = privateWars;
            _tickPrivateWar = tickPrivateWar;
            _resolvePrivateWar = resolvePrivateWar;
            _applyBattleOutcome = applyBattleOutcome;
            _sideResolver = sideResolver;
            _hierarchy = hierarchy;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                _serialisedWars = PrivateWarStateSerialiser.SerialiseWars(_stateStore.SnapshotPrivateWars());
            }

            dataStore.SyncData("DadgPrivateWars", ref _serialisedWars);

            _serialisedWars ??= new List<string>();
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            if (_serialisedWars.Count == 0) return;

            _stateStore.InitialisePrivateWars(PrivateWarStateSerialiser.DeserialiseWars(_serialisedWars));
        }

        // DADG owns prosecution. The stock siege planner never enumerates a same-kingdom settlement
        // (design §5, risk #1), so the goal never enters the engine's behaviour vote on its own. For
        // each attacker principal's eligible party, inject the goal as a scored besiege candidate; it
        // then flows through the engine's own max-score selection in AiPartyThinkBehavior, so a
        // higher-priority objective (notably defending the clan's own besieged fief) naturally wins.
        private void OnAiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
        {
            var goal = FindPrivateWarGoalFor(mobileParty);
            if (goal is null) return;

            var behaviour = new AIBehaviorData(
                goal, AiBehavior.BesiegeSettlement, MobileParty.NavigationType.Default,
                willGatherArmy: false, isFromPort: false, isTargetingPort: false);
            p.AddBehaviorScore((behaviour, PrivateWarSiegeSelectionScore));
        }

        // Once a day, advance every active war: recompute its signed score from current world state and,
        // when a side crosses the +/-100 threshold, resolve it. The score calculator's fatigue term
        // drifts toward whoever holds the main goal, so the war terminates even in a stalemate.
        private void OnDailyTick()
        {
            float currentDay = (float)CampaignTime.Now.ToDays;

            foreach (var war in _privateWars.GetAll())
            {
                if (war.Status != PrivateWarStatus.Active) continue;

                var observations = BuildObservations(war);
                var result = _tickPrivateWar.Execute(war, observations, currentDay);
                var scored = war with { Score = result.Score };

                if (result.Outcome is { } outcome)
                    ResolveWar(scored, outcome);
                else
                    _privateWars.Update(scored);
            }
        }

        // When a battle resolves between the two sides of an active private war, bump that war's
        // accumulated battle score (the sole mutator of BattleScore, design §18.B). The bump is the
        // share of the losing side's total strength destroyed, signed by the winner. Fires at
        // WaitingRemoval, before parties are finalized, so the sides' casualties and strength are intact.
        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent is null || mapEvent.WinningSide == BattleSideEnum.None) return;

            foreach (var war in _privateWars.GetAll())
            {
                if (war.Status != PrivateWarStatus.Active) continue;

                var attackerBattleSide = ResolveBattleSide(mapEvent.AttackerSide, war);
                var defenderBattleSide = ResolveBattleSide(mapEvent.DefenderSide, war);
                if (attackerBattleSide is null || defenderBattleSide is null) continue;
                if (attackerBattleSide == defenderBattleSide) continue;

                var winnerWarSide = mapEvent.WinningSide == BattleSideEnum.Attacker
                    ? attackerBattleSide.Value
                    : defenderBattleSide.Value;

                var losingSide = mapEvent.WinningSide == BattleSideEnum.Attacker
                    ? mapEvent.DefenderSide
                    : mapEvent.AttackerSide;

                var outcome = new BattleOutcome(
                    winnerWarSide,
                    losingSide.CasualtyStrength,
                    losingSide.CasualtyStrength + losingSide.RecalculateStrengthOfSide());

                _privateWars.Update(_applyBattleOutcome.Execute(war, outcome));
            }
        }

        // Which side of this war a battle side fights on, or null if neither belligerent. A battle side
        // can hold parties from several clans; resolve from the first whose suzerain chain maps onto the war.
        private WarSide? ResolveBattleSide(MapEventSide battleSide, PrivateWar war)
        {
            foreach (var party in battleSide.Parties)
            {
                var clan = party.Party.MobileParty?.ActualClan ?? party.Party.Settlement?.OwnerClan;
                if (clan is null) continue;

                var side = ResolveSide(clan.StringId, war);
                if (side is not null) return side;
            }

            return null;
        }

        // Assemble the score inputs from current world state. Side membership is resolved through the
        // feudal hierarchy so a clan fights with (or against) its whole suzerain subtree. The main goal
        // carries its own score term, so it is excluded from the town/castle crossing counts.
        private PrivateWarObservations BuildObservations(PrivateWar war)
        {
            int defTownsByAtt = 0, defCastlesByAtt = 0, attTownsByDef = 0, attCastlesByDef = 0;
            bool attackerHoldsGoal = false;

            foreach (var entry in war.OriginalFiefOwners)
            {
                var settlement = Settlement.Find(entry.Key);
                if (settlement?.OwnerClan is null) continue;

                var currentSide = ResolveSide(settlement.OwnerClan.StringId, war);

                if (entry.Key == war.MainGoalSettlementId)
                {
                    attackerHoldsGoal = currentSide == WarSide.Attacker;
                    continue;
                }

                var originalSide = ResolveSide(entry.Value, war);
                if (currentSide is null || originalSide is null || currentSide == originalSide) continue;

                if (originalSide == WarSide.Defender && currentSide == WarSide.Attacker)
                {
                    if (settlement.IsTown) defTownsByAtt++;
                    else if (settlement.IsCastle) defCastlesByAtt++;
                }
                else if (originalSide == WarSide.Attacker && currentSide == WarSide.Defender)
                {
                    if (settlement.IsTown) attTownsByDef++;
                    else if (settlement.IsCastle) attCastlesByDef++;
                }
            }

            int defenderPrisoners = CountPrisoners(war.DefenderPrincipalClanId, WarSide.Attacker, war);
            int attackerPrisoners = CountPrisoners(war.AttackerPrincipalClanId, WarSide.Defender, war);

            return new PrivateWarObservations(
                attackerHoldsGoal,
                defTownsByAtt, defCastlesByAtt,
                attTownsByDef, attCastlesByDef,
                defenderPrisoners, attackerPrisoners,
                war.BattleScore);
        }

        // Captives of the given principal's own clan currently held by a party or settlement belonging
        // to the opposing side (design §18.B). Only the principal clan's heroes count, not the whole side.
        private int CountPrisoners(string capturedPrincipalClanId, WarSide captorSide, PrivateWar war)
        {
            var clan = Clan.All.FirstOrDefault(c => c.StringId == capturedPrincipalClanId);
            if (clan is null) return 0;

            int count = 0;
            foreach (var hero in clan.Heroes)
            {
                if (!hero.IsPrisoner) continue;

                var captor = hero.PartyBelongedToAsPrisoner;
                var captorClan = captor?.MobileParty?.ActualClan ?? captor?.Settlement?.OwnerClan;
                if (captorClan is null) continue;

                if (ResolveSide(captorClan.StringId, war) == captorSide) count++;
            }

            return count;
        }

        // Apply the terminal outcome to engine state: revert each captured fief to its pre-war owner.
        // On attacker victory the resolution leaves the main goal with the attacker as the prize; the
        // dignity transfer already rode in on the capture (FeudalTitleCampaignBehavior.OnSettlementOwnerChanged),
        // so it is not repeated here. The war record is marked concluded.
        private void ResolveWar(PrivateWar war, PrivateWarOutcome outcome)
        {
            var currentOwners = new Dictionary<string, string>();
            foreach (var settlementId in war.OriginalFiefOwners.Keys)
            {
                var ownerClan = Settlement.Find(settlementId)?.OwnerClan;
                if (ownerClan != null) currentOwners[settlementId] = ownerClan.StringId;
            }

            var plan = _resolvePrivateWar.Execute(war, outcome, currentOwners, clanId => ResolveSide(clanId, war));

            foreach (var revert in plan.Reverts)
            {
                var settlement = Settlement.Find(revert.SettlementId);
                var newOwner = Clan.All.FirstOrDefault(c => c.StringId == revert.RevertToClanId);
                if (settlement is null || newOwner is null || newOwner.IsEliminated) continue;
                if (settlement.OwnerClan == newOwner) continue;

                var hero = newOwner.Leader ?? newOwner.Heroes.FirstOrDefault();
                if (hero is null) continue;
                ChangeOwnerOfSettlementAction.ApplyByDefault(hero, settlement);
            }

            _privateWars.Update(war with { Status = PrivateWarStatus.Concluded });

            InfoPrinter.Display(
                $"Private war concluded: {war.AttackerPrincipalClanId} vs {war.DefenderPrincipalClanId} - {outcome}.");
        }

        // Which side of this war a clan fights on, walking up its suzerain chain to the nearest principal.
        private WarSide? ResolveSide(string clanId, PrivateWar war)
            => _sideResolver.ResolveSide(clanId, war, _hierarchy.GetSuzerain);

        // The goal this party should besiege, or null if it is not an eligible attacker-principal
        // party for any active private war. The player drives their own clan, and a party mid-battle
        // or following an army leader is left to the engine.
        private Settlement? FindPrivateWarGoalFor(MobileParty mobileParty)
        {
            if (mobileParty is null || !mobileParty.IsActive || mobileParty.IsMainParty) return null;
            if (mobileParty.MapEvent != null) return null;
            if (mobileParty.Army != null && mobileParty.Army.LeaderParty != mobileParty) return null;

            var clan = mobileParty.ActualClan;
            if (clan is null || clan == Clan.PlayerClan) return null;

            foreach (var war in _stateStore.SnapshotPrivateWars())
            {
                if (war.Status != PrivateWarStatus.Active || war.MainGoalSettlementId is null) continue;
                if (war.AttackerPrincipalClanId != clan.StringId) continue;

                var goal = Settlement.Find(war.MainGoalSettlementId);
                if (goal is null || !goal.IsFortification || goal.OwnerClan == clan) continue;
                return goal;
            }

            return null;
        }
    }
}
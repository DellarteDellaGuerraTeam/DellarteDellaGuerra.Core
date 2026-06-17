using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PrivateWars.Model;
using DellarteDellaGuerra.PrivateWars.Spi;
using DellarteDellaGuerra.Titles.Api.Campaign;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.PrivateWars.Api.Campaign
{
    /**
     * <summary>
     *  Owns the registry lifecycle and the war's prosecution drive. Persists active and concluded
     *  wars through Bannerlord saves and restores them on load; and, because the engine never plans
     *  a same-kingdom siege on its own (design §5, risk #1), injects the war goal as a scored besiege
     *  candidate into each attacker party's AI think so it competes in the engine's normal behaviour
     *  vote — losing, by design, to higher priorities such as defending the clan's own threatened
     *  fief. The retention model keeps an in-progress siege sticky.
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

        private List<string> _serialisedWars = new();

        public PrivateWarCampaignBehavior(IFeudalStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
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
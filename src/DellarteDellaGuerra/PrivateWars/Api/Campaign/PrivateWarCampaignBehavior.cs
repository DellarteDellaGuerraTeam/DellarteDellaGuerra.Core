using System.Collections.Generic;
using System.Linq;
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
     *  Owns the registry lifecycle and the war's drive loop. Persists active and concluded wars
     *  through Bannerlord saves and restores them on load; and, because the engine never plans a
     *  same-kingdom siege on its own (design §5, risk #1), each day explicitly directs every AI
     *  attacker's field parties to besiege the war goal. Scoring and resolution are wired in later
     *  phases.
     * </summary>
     */
    public class PrivateWarCampaignBehavior : CampaignBehaviorBase
    {
        private readonly IFeudalStateStore _stateStore;

        private List<string> _serialisedWars = new();

        public PrivateWarCampaignBehavior(IFeudalStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
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

        // DADG owns prosecution: the stock siege AI never proposes besieging a fellow kingdom
        // member's settlement (design §5, risk #1), so each day we point every AI attacker's field
        // parties at the war goal. The player drives their own clan; the siege-retention model
        // (DadgTargetScoreCalculatingModel) keeps the engine from abandoning the ordered siege.
        private void OnDailyTick()
        {
            foreach (var war in _stateStore.SnapshotPrivateWars())
            {
                if (war.Status != PrivateWarStatus.Active || war.MainGoalSettlementId is null) continue;

                var attacker = FindClan(war.AttackerPrincipalClanId);
                if (attacker is null || attacker == Clan.PlayerClan) continue;

                var goal = Settlement.Find(war.MainGoalSettlementId);
                if (goal is null || !goal.IsFortification) continue;

                // Goal already taken -> nothing to drive; the retention model holds it.
                if (goal.OwnerClan == attacker) continue;

                DriveAttackerToBesiege(attacker, goal);
            }
        }

        private static void DriveAttackerToBesiege(Clan attacker, Settlement goal)
        {
            foreach (var component in attacker.WarPartyComponents)
            {
                var party = component.MobileParty;
                if (party is null || !party.IsActive || party.IsMainParty) continue;
                if (party.MapEvent != null) continue;                              // mid-battle/assault
                if (party.Army != null && party.Army.LeaderParty != party) continue; // follower obeys its leader

                // SetMoveBesiegeSettlement is idempotent (no-ops when already besieging this goal).
                party.SetMoveBesiegeSettlement(goal, MobileParty.NavigationType.Default);
            }
        }

        private static Clan? FindClan(string stringId)
            => TaleWorlds.CampaignSystem.Campaign.Current?.Clans.FirstOrDefault(c => c.StringId == stringId);
    }
}
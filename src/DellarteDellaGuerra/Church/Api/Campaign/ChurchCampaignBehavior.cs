using System.Linq;
using DellarteDellaGuerra.Domain.Church.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Church.Api.Campaign
{
    public class ChurchCampaignBehavior : CampaignBehaviorBase
    {
        private readonly ChurchSettlements _churchSettlements;
        private readonly ChurchSacrilege _churchSacrilege;
        private readonly IChurchSettingsProvider _churchSettingsProvider;

        public ChurchCampaignBehavior(
            ChurchSettlements churchSettlements,
            ChurchSacrilege churchSacrilege,
            IChurchSettingsProvider churchSettingsProvider)
        {
            _churchSettlements = churchSettlements;
            _churchSacrilege = churchSacrilege;
            _churchSettingsProvider = churchSettingsProvider;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, SpawnMissingAbbots);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, ApplyTithe);
            CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            SpawnMissingAbbots();
        }

        private void SpawnMissingAbbots()
        {
            foreach (var settlement in Settlement.All)
            {
                if (!_churchSettlements.IsChurchSettlement(settlement)) continue;
                if (settlement.Notables.Any(notable => notable.IsPreacher)) continue;

                var abbot = HeroCreator.CreateNotable(Occupation.Preacher, settlement);
                EnterSettlementAction.ApplyForCharacterOnly(abbot, settlement);
            }
        }

        private void ApplyTithe()
        {
            var weeklyTithePower = _churchSettingsProvider.GetSettings().WeeklyTithePower;
            foreach (var settlement in Settlement.All)
            {
                if (!_churchSettlements.IsChurchSettlement(settlement)) continue;

                foreach (var abbot in settlement.Notables.Where(notable => notable.IsPreacher && notable.IsAlive))
                    abbot.AddPower(weeklyTithePower);
            }
        }

        private void OnVillageLooted(Village village)
        {
            if (!_churchSettlements.IsChurchSettlement(village.Settlement)) return;

            var raider = village.Settlement.LastAttackerParty?.LeaderHero;
            if (raider == null || !raider.IsAlive) return;

            _churchSacrilege.Apply(raider, village.Settlement);
        }
    }
}

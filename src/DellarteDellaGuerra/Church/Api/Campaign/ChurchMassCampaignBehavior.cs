using System.Linq;
using DellarteDellaGuerra.Domain.Church.Mass;
using DellarteDellaGuerra.Domain.Church.Port;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church.Api.Campaign
{
    public class ChurchMassCampaignBehavior : CampaignBehaviorBase
    {
        private readonly ChurchSettlements _churchSettlements;
        private readonly IChurchSettingsProvider _churchSettingsProvider;

        private CampaignTime _lastMassTime = CampaignTime.Never;

        public ChurchMassCampaignBehavior(
            ChurchSettlements churchSettlements,
            IChurchSettingsProvider churchSettingsProvider)
        {
            _churchSettlements = churchSettlements;
            _churchSettingsProvider = churchSettingsProvider;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_dadgChurchLastMassTime", ref _lastMassTime);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(
                "village",
                "dadg_church_attend_mass",
                "{=aM3sVk7P}Attend mass",
                CanAttendMass,
                AttendMass);
        }

        private bool CanAttendMass(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;

            var settlement = Settlement.CurrentSettlement;
            if (settlement == null || !_churchSettlements.IsChurchSettlement(settlement)) return false;

            var isSunday = CampaignTime.Now.GetDayOfWeek == 0;
            var attendedToday = _lastMassTime.ElapsedDaysUntilNow < 1f;
            var enabled = settlement.Village.VillageState == Village.VillageStates.Normal &&
                          MassPolicy.Evaluate(isSunday, attendedToday) == MassOutcome.Allowed;

            var tooltip = new TextObject(
                "{=tY5nQw9R}Mass will be held on the Lord's day. ({N} {?N>1}days{?}day{\\?} hence)");
            tooltip.SetTextVariable("N", DaysUntilNextSunday());
            return MenuHelper.SetOptionProperties(args, enabled, !enabled, tooltip);
        }

        private static int DaysUntilNextSunday()
        {
            var dayOfWeek = (int)CampaignTime.Now.GetDayOfWeek;
            return dayOfWeek == 0 ? CampaignTime.DaysInWeek : CampaignTime.DaysInWeek - dayOfWeek;
        }

        private void AttendMass(MenuCallbackArgs args)
        {
            var settings = _churchSettingsProvider.GetSettings();
            MobileParty.MainParty.RecentEventsMorale += settings.MassMorale;

            var abbot = Settlement.CurrentSettlement.Notables
                .FirstOrDefault(notable => notable.IsPreacher && notable.IsAlive);
            if (abbot != null) ChangeRelationAction.ApplyPlayerRelation(abbot, settings.MassRelation);

            _lastMassTime = CampaignTime.Now;
            GameMenu.SwitchToMenu("village");
        }
    }
}

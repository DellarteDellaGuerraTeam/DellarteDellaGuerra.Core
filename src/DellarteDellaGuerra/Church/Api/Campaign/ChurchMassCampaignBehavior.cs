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
                ChurchMenuIds.Hub,
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
            var villageIsNormal = settlement.Village.VillageState == Village.VillageStates.Normal;
            var outcome = MassPolicy.Evaluate(isSunday, attendedToday);
            var enabled = villageIsNormal && outcome == MassOutcome.Allowed;
            var tooltip = GetMassTooltip(villageIsNormal, outcome);
            return MenuHelper.SetOptionProperties(args, enabled, !enabled, tooltip);
        }

        private TextObject GetMassTooltip(bool villageIsNormal, MassOutcome outcome)
        {
            var settings = _churchSettingsProvider.GetSettings();
            TextObject tooltip;

            if (!villageIsNormal)
            {
                tooltip = new TextObject(
                    "{=qL7wJf4D}Mass cannot be held while the settlement is in turmoil. " +
                    "When available, attending grants {MORALE} party morale and {RELATION} relation with the local clergy.");
            }
            else if (outcome == MassOutcome.AlreadyAttended)
            {
                tooltip = new TextObject(
                    "{=sH2mKv8P}You have already attended mass today. The next mass is in {DAYS} days. " +
                    "Attending grants {MORALE} party morale and {RELATION} relation with the local clergy.");
                tooltip.SetTextVariable("DAYS", CampaignTime.DaysInWeek);
            }
            else if (outcome == MassOutcome.NotSunday)
            {
                tooltip = new TextObject(
                    "{=yN6cTb3R}Mass is held on Sunday. The next mass is in {DAYS} {?DAYS>1}days{?}day{\\?}. " +
                    "Attending grants {MORALE} party morale and {RELATION} relation with the local clergy.");
                tooltip.SetTextVariable("DAYS", DaysUntilNextSunday());
            }
            else
            {
                tooltip = new TextObject(
                    "{=pV9dXm5A}Attending mass grants {MORALE} party morale and {RELATION} relation with the local clergy.");
            }

            tooltip.SetTextVariable("MORALE", settings.MassMorale);
            tooltip.SetTextVariable("RELATION", settings.MassRelation);
            return tooltip;
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
            GameMenu.SwitchToMenu(ChurchMenuIds.Hub);
        }
    }
}

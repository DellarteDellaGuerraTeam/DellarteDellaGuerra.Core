using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Church.Donation;
using DellarteDellaGuerra.Domain.Church.Mass;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church.Api.Campaign
{
    public class ChurchCampaignBehavior : CampaignBehaviorBase
    {
        private const int WeeklyTithePower = 2;
        private const int DonationPower = 5;
        private const int SacrilegeRelationLocal = -15;
        private const int SacrilegeRelationOthers = -5;

        private Dictionary<Hero, CampaignTime> _lastDonationTimes = new();
        private CampaignTime _lastMassTime = CampaignTime.Never;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, SpawnMissingAbbots);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, ApplyTithe);
            CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_dadgChurchLastDonationTimes", ref _lastDonationTimes);
            dataStore.SyncData("_dadgChurchLastMassTime", ref _lastMassTime);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            SpawnMissingAbbots();
            AddDialogs(starter);
            starter.AddGameMenuOption(
                "village",
                "dadg_church_attend_mass",
                "{=aM3sVk7P}Attend mass",
                CanAttendMass,
                AttendMass);
        }

        private static void SpawnMissingAbbots()
        {
            foreach (var settlement in Settlement.All)
            {
                if (!ChurchSettlements.IsChurchSettlement(settlement)) continue;
                if (settlement.Notables.Any(notable => notable.IsPreacher)) continue;

                var abbot = HeroCreator.CreateNotable(Occupation.Preacher, settlement);
                EnterSettlementAction.ApplyForCharacterOnly(abbot, settlement);
            }
        }

        private bool CanAttendMass(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;

            var settlement = Settlement.CurrentSettlement;
            if (settlement == null || !ChurchSettlements.IsChurchSettlement(settlement)) return false;

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
            MobileParty.MainParty.RecentEventsMorale += MassPolicy.MoraleGain;

            var abbot = Settlement.CurrentSettlement.Notables
                .FirstOrDefault(notable => notable.IsPreacher && notable.IsAlive);
            if (abbot != null) ChangeRelationAction.ApplyPlayerRelation(abbot, MassPolicy.RelationGain);

            _lastMassTime = CampaignTime.Now;
            GameMenu.SwitchToMenu("village");
        }

        private void ApplyTithe()
        {
            foreach (var settlement in Settlement.All)
            {
                if (!ChurchSettlements.IsChurchSettlement(settlement)) continue;

                foreach (var abbot in settlement.Notables.Where(notable => notable.IsPreacher && notable.IsAlive))
                    abbot.AddPower(WeeklyTithePower);
            }
        }

        private void OnVillageLooted(Village village)
        {
            if (!ChurchSettlements.IsChurchSettlement(village.Settlement)) return;

            var raider = village.Settlement.LastAttackerParty?.LeaderHero;
            if (raider == null || !raider.IsAlive) return;

            ApplySacrilege(raider, village.Settlement);
        }

        internal static void ApplySacrilege(Hero offender, Settlement site)
        {
            var isPlayerOffender = offender == Hero.MainHero;

            foreach (var settlement in Settlement.All)
            {
                if (!ChurchSettlements.IsChurchSettlement(settlement)) continue;

                var relationChange = settlement == site
                    ? SacrilegeRelationLocal
                    : SacrilegeRelationOthers;
                foreach (var abbot in settlement.Notables.Where(notable => notable.IsPreacher && notable.IsAlive))
                {
                    if (isPlayerOffender) ChangeRelationAction.ApplyPlayerRelation(abbot, relationChange);
                    else ChangeRelationAction.ApplyRelationChangeBetweenHeroes(offender, abbot, relationChange);
                }
            }

            if (!isPlayerOffender) return;
            var message = new TextObject(
                "{=gB2xLj4C}Word of your sacrilege at {SETTLEMENT} spreads among the clergy of England.");
            message.SetTextVariable("SETTLEMENT", site.Name);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
        }

        private void AddDialogs(CampaignGameStarter starter)
        {
            starter.AddDialogLine(
                "dadg_church_greeting",
                "start",
                "dadg_church_talk",
                "{=kT4mWp2Q}God keep you, my {?PLAYER.GENDER}lady{?}lord{\\?}. What brings you to {MONASTERY_NAME}?",
                IsConversationWithAbbot,
                null,
                200);
            starter.AddPlayerLine(
                "dadg_church_donate",
                "dadg_church_talk",
                "dadg_church_donate_thanks",
                "{=rB7xNc4V}I wish to make a donation to the {CHURCH_TYPE}. ({DONATION_COST} denars)",
                CanDonate,
                Donate);
            starter.AddDialogLine(
                "dadg_church_donate_thanks",
                "dadg_church_donate_thanks",
                "dadg_church_talk",
                "{=mJ5tHs9E}God reward you, my {?PLAYER.GENDER}lady{?}lord{\\?}. This {TITLE} will remember your generosity.",
                null,
                null);
            starter.AddPlayerLine(
                "dadg_church_blessing",
                "dadg_church_talk",
                "dadg_church_blessing_reply",
                "{=zD8gKa3U}Bless me, Father.",
                null,
                null);
            starter.AddDialogLine(
                "dadg_church_blessing_reply",
                "dadg_church_blessing_reply",
                "dadg_church_talk",
                "{=eX2vFq6Y}May the Lord bless you and keep you, and grant you peace on all your roads.",
                null,
                null);
            starter.AddPlayerLine(
                "dadg_church_leave",
                "dadg_church_talk",
                "close_window",
                "{=sL6yBd1W}I must be on my way.",
                null,
                null);
        }

        private static bool IsConversationWithAbbot()
        {
            if (CharacterObject.OneToOneConversationCharacter?.Occupation != Occupation.Preacher) return false;

            var settlement = Hero.OneToOneConversationHero?.CurrentSettlement;
            if (settlement == null || !ChurchSettlements.IsChurchSettlement(settlement)) return false;

            MBTextManager.SetTextVariable("MONASTERY_NAME", settlement.Name);
            return true;
        }

        private bool CanDonate()
        {
            var abbot = Hero.OneToOneConversationHero;
            if (abbot == null) return false;

            float? daysSinceLastDonation = _lastDonationTimes.TryGetValue(abbot, out var lastDonation)
                ? lastDonation.ElapsedDaysUntilNow
                : (float?)null;
            if (DonationPolicy.Evaluate(Hero.MainHero.Gold, daysSinceLastDonation) != DonationOutcome.Allowed)
                return false;

            var settlement = abbot.CurrentSettlement;
            MBTextManager.SetTextVariable("CHURCH_TYPE", ChurchSettlements.GetChurchType(settlement));
            MBTextManager.SetTextVariable("TITLE", ChurchSettlements.GetClergyTitle(settlement));
            MBTextManager.SetTextVariable("DONATION_COST", DonationPolicy.CostInGold);
            return true;
        }

        private void Donate()
        {
            var abbot = Hero.OneToOneConversationHero;
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, abbot, DonationPolicy.CostInGold);
            ChangeRelationAction.ApplyPlayerRelation(abbot, DonationPolicy.RelationGain);
            GainRenownAction.Apply(Hero.MainHero, DonationPolicy.RenownGain);
            abbot.AddPower(DonationPower);
            _lastDonationTimes[abbot] = CampaignTime.Now;
        }
    }
}

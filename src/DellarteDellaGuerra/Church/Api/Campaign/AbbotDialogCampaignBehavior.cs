using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church.Donation;
using DellarteDellaGuerra.Domain.Church.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church.Api.Campaign
{
    public class AbbotDialogCampaignBehavior : CampaignBehaviorBase
    {
        private readonly ChurchSettlements _churchSettlements;
        private readonly IChurchSettingsProvider _churchSettingsProvider;

        private Dictionary<Hero, CampaignTime> _lastDonationTimes = new();

        public AbbotDialogCampaignBehavior(
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
            dataStore.SyncData("_dadgChurchLastDonationTimes", ref _lastDonationTimes);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddDialogs(starter);
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

        private bool IsConversationWithAbbot()
        {
            if (CharacterObject.OneToOneConversationCharacter?.Occupation != Occupation.Preacher) return false;

            var settlement = Hero.OneToOneConversationHero?.CurrentSettlement;
            if (settlement == null || !_churchSettlements.IsChurchSettlement(settlement)) return false;

            MBTextManager.SetTextVariable("MONASTERY_NAME", settlement.Name);
            return true;
        }

        private bool CanDonate()
        {
            var abbot = Hero.OneToOneConversationHero;
            if (abbot == null) return false;

            var donationCost = _churchSettingsProvider.GetSettings().DonationCost;
            float? daysSinceLastDonation = _lastDonationTimes.TryGetValue(abbot, out var lastDonation)
                ? lastDonation.ElapsedDaysUntilNow
                : (float?)null;
            if (DonationPolicy.Evaluate(Hero.MainHero.Gold, daysSinceLastDonation, donationCost) != DonationOutcome.Allowed)
                return false;

            var settlement = abbot.CurrentSettlement;
            MBTextManager.SetTextVariable("CHURCH_TYPE", _churchSettlements.GetChurchType(settlement));
            MBTextManager.SetTextVariable("TITLE", _churchSettlements.GetClergyTitle(settlement));
            MBTextManager.SetTextVariable("DONATION_COST", donationCost);
            return true;
        }

        private void Donate()
        {
            var settings = _churchSettingsProvider.GetSettings();
            var abbot = Hero.OneToOneConversationHero;
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, abbot, settings.DonationCost);
            ChangeRelationAction.ApplyPlayerRelation(abbot, settings.DonationRelation);
            GainRenownAction.Apply(Hero.MainHero, DonationPolicy.RenownGain);
            abbot.AddPower(settings.DonationPower);
            _lastDonationTimes[abbot] = CampaignTime.Now;
        }
    }
}

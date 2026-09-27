using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church.Donation;
using DellarteDellaGuerra.Domain.Church.Favour;
using DellarteDellaGuerra.Domain.Church.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church.Api.Campaign
{
    public class AbbotDialogCampaignBehavior : CampaignBehaviorBase
    {
        private readonly ChurchSettlements _churchSettlements;
        private readonly ChurchFavourService _churchFavourService;
        private readonly IChurchSettingsProvider _churchSettingsProvider;

        private Dictionary<Hero, CampaignTime> _lastDonationTimes = new();
        private CampaignTime _lastBishopBlessingTime = CampaignTime.Never;

        public AbbotDialogCampaignBehavior(
            ChurchSettlements churchSettlements,
            ChurchFavourService churchFavourService,
            IChurchSettingsProvider churchSettingsProvider)
        {
            _churchSettlements = churchSettlements;
            _churchFavourService = churchFavourService;
            _churchSettingsProvider = churchSettingsProvider;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_dadgChurchLastDonationTimes", ref _lastDonationTimes);
            dataStore.SyncData("_dadgChurchLastBishopBlessingTime", ref _lastBishopBlessingTime);
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
                "{=rB7xNc4V}I wish to make a donation to the {CHURCH_TYPE}. " +
                "({DONATION_COST} denars; {DONATION_RELATION} relation, {DONATION_RENOWN} renown, " +
                "{DONATION_POWER} clergy power)",
                CanShowDonation,
                Donate,
                clickableConditionDelegate: CanDonate);
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
                "dadg_church_favour",
                "dadg_church_talk",
                "dadg_church_favour_reply",
                "{=pV6kMd2H}How does the Church regard me, Your Grace?",
                IsConversationWithBishop,
                null);
            starter.AddDialogLine(
                "dadg_church_favour_reply_beloved",
                "dadg_church_favour_reply",
                "dadg_church_talk",
                "{=yL3tWn7C}All England's cloisters speak your name with love. You are a true friend of Holy Church.",
                () => _churchFavourService.GetStatus().Rank == ChurchFavourRank.Beloved,
                null);
            starter.AddDialogLine(
                "dadg_church_favour_reply_favoured",
                "dadg_church_favour_reply",
                "dadg_church_talk",
                "{=dK8rBv4J}The Church counts you among her faithful {?PLAYER.GENDER}daughters{?}sons{\\?}.",
                () => _churchFavourService.GetStatus().Rank == ChurchFavourRank.Favoured,
                null);
            starter.AddDialogLine(
                "dadg_church_favour_reply_indifferent",
                "dadg_church_favour_reply",
                "dadg_church_talk",
                "{=fT2mHx9P}The Church knows little of you, my {?PLAYER.GENDER}lady{?}lord{\\?}. Works, not words, commend a soul.",
                () => _churchFavourService.GetStatus().Rank == ChurchFavourRank.Indifferent,
                null);
            starter.AddDialogLine(
                "dadg_church_favour_reply_ill_regarded",
                "dadg_church_favour_reply",
                "dadg_church_talk",
                "{=wS5jNc6E}There is murmuring against you in the chapter houses. Mend your ways.",
                () => _churchFavourService.GetStatus().Rank == ChurchFavourRank.IllRegarded,
                null);
            starter.AddDialogLine(
                "dadg_church_favour_reply_reviled",
                "dadg_church_favour_reply",
                "dadg_church_talk",
                "{=hM7qGz3V}You stand in the shadow of anathema. Repent, before God and His Church.",
                () => _churchFavourService.GetStatus().Rank == ChurchFavourRank.Reviled,
                null);
            starter.AddPlayerLine(
                "dadg_church_bishop_blessing",
                "dadg_church_talk",
                "dadg_church_bishop_blessing_reply",
                "{=cX4bPk8R}Grant me your blessing, Your Grace. " +
                "({BLESSING_MORALE} party morale, {BLESSING_RENOWN} renown)",
                CanShowBishopBlessing,
                GrantBishopBlessing,
                clickableConditionDelegate: CanRequestBishopBlessing);
            starter.AddDialogLine(
                "dadg_church_bishop_blessing_reply",
                "dadg_church_bishop_blessing_reply",
                "dadg_church_talk",
                "{=rJ9nFw2Y}Kneel, then. May God make you strong in battle and merciful in victory.",
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

        private bool CanShowDonation()
        {
            var abbot = Hero.OneToOneConversationHero;
            if (abbot == null) return false;

            var settings = _churchSettingsProvider.GetSettings();
            var settlement = abbot.CurrentSettlement;
            if (settlement == null) return false;

            MBTextManager.SetTextVariable("CHURCH_TYPE", _churchSettlements.GetChurchType(settlement));
            MBTextManager.SetTextVariable("TITLE", _churchSettlements.GetClergyTitle(settlement, abbot));
            MBTextManager.SetTextVariable("DONATION_COST", settings.DonationCost);
            MBTextManager.SetTextVariable("DONATION_RELATION", FormatSigned(settings.DonationRelation));
            MBTextManager.SetTextVariable("DONATION_RENOWN", FormatSigned(DonationPolicy.RenownGain));
            MBTextManager.SetTextVariable("DONATION_POWER", FormatSigned(settings.DonationPower));
            return true;
        }

        private bool CanDonate(out TextObject explanation)
        {
            explanation = new TextObject(string.Empty);
            var abbot = Hero.OneToOneConversationHero;
            if (abbot == null) return false;

            var settings = _churchSettingsProvider.GetSettings();
            float? daysSinceLastDonation = _lastDonationTimes.TryGetValue(abbot, out var lastDonation)
                ? lastDonation.ElapsedDaysUntilNow
                : (float?)null;
            var outcome = DonationPolicy.Evaluate(Hero.MainHero.Gold, daysSinceLastDonation, settings.DonationCost);
            if (outcome == DonationOutcome.Allowed) return true;

            if (outcome == DonationOutcome.InsufficientGold)
            {
                explanation = new TextObject("{=sC4hYp9D}Requires {COST} denars; you have {GOLD}.");
                explanation.SetTextVariable("COST", settings.DonationCost);
                explanation.SetTextVariable("GOLD", Hero.MainHero.Gold);
            }
            else
            {
                var daysRemaining = Math.Max(1,
                    (int)Math.Ceiling(DonationPolicy.CooldownInDays - daysSinceLastDonation.GetValueOrDefault()));
                explanation = new TextObject(
                    "{=aV7nRm3T}Available again in {DAYS} {?DAYS>1}days{?}day{\\?}.");
                explanation.SetTextVariable("DAYS", daysRemaining);
            }

            return false;
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

        private bool IsConversationWithBishop()
        {
            if (CharacterObject.OneToOneConversationCharacter?.Occupation != Occupation.Preacher) return false;

            var settlement = Hero.OneToOneConversationHero?.CurrentSettlement;
            return settlement != null && _churchSettlements.IsCathedral(settlement);
        }

        private bool CanShowBishopBlessing()
        {
            if (!IsConversationWithBishop()) return false;

            var settings = _churchSettingsProvider.GetSettings();
            MBTextManager.SetTextVariable("BLESSING_MORALE", FormatSigned(settings.BlessingMorale));
            MBTextManager.SetTextVariable("BLESSING_RENOWN", FormatSigned(BishopBlessingPolicy.RenownGain));
            return true;
        }

        private bool CanRequestBishopBlessing(out TextObject explanation)
        {
            explanation = new TextObject(string.Empty);
            if (!IsConversationWithBishop()) return false;

            float? daysSinceLastBlessing = _lastBishopBlessingTime == CampaignTime.Never
                ? (float?)null
                : _lastBishopBlessingTime.ElapsedDaysUntilNow;
            var outcome = BishopBlessingPolicy.Evaluate(
                _churchFavourService.GetStatus().Rank, daysSinceLastBlessing);
            if (outcome == BishopBlessingOutcome.Allowed) return true;

            if (outcome == BishopBlessingOutcome.NotFavoured)
            {
                explanation = new TextObject("{=pT2dKx6H}Requires Favoured Church standing.");
            }
            else
            {
                var daysRemaining = Math.Max(1,
                    (int)Math.Ceiling(BishopBlessingPolicy.CooldownInDays -
                                           daysSinceLastBlessing.GetValueOrDefault()));
                explanation = new TextObject(
                    "{=nM9qFs5B}Available again in {DAYS} {?DAYS>1}days{?}day{\\?}.");
                explanation.SetTextVariable("DAYS", daysRemaining);
            }

            return false;
        }

        private void GrantBishopBlessing()
        {
            MobileParty.MainParty.RecentEventsMorale += _churchSettingsProvider.GetSettings().BlessingMorale;
            GainRenownAction.Apply(Hero.MainHero, BishopBlessingPolicy.RenownGain);
            _lastBishopBlessingTime = CampaignTime.Now;
        }

        private static string FormatSigned(int value) => value.ToString("+0;-0;0");

    }
}

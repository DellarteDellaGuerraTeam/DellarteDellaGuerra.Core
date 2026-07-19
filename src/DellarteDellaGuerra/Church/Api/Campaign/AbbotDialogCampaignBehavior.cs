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
        private readonly IChurchSettingsProvider _churchSettingsProvider;

        private Dictionary<Hero, CampaignTime> _lastDonationTimes = new();
        private CampaignTime _lastBishopBlessingTime = CampaignTime.Never;

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
                () => GetFavourRank() == ChurchFavourRank.Beloved,
                null);
            starter.AddDialogLine(
                "dadg_church_favour_reply_favoured",
                "dadg_church_favour_reply",
                "dadg_church_talk",
                "{=dK8rBv4J}The Church counts you among her faithful {?PLAYER.GENDER}daughters{?}sons{\\?}.",
                () => GetFavourRank() == ChurchFavourRank.Favoured,
                null);
            starter.AddDialogLine(
                "dadg_church_favour_reply_indifferent",
                "dadg_church_favour_reply",
                "dadg_church_talk",
                "{=fT2mHx9P}The Church knows little of you, my {?PLAYER.GENDER}lady{?}lord{\\?}. Works, not words, commend a soul.",
                () => GetFavourRank() == ChurchFavourRank.Indifferent,
                null);
            starter.AddDialogLine(
                "dadg_church_favour_reply_ill_regarded",
                "dadg_church_favour_reply",
                "dadg_church_talk",
                "{=wS5jNc6E}There is murmuring against you in the chapter houses. Mend your ways.",
                () => GetFavourRank() == ChurchFavourRank.IllRegarded,
                null);
            starter.AddDialogLine(
                "dadg_church_favour_reply_reviled",
                "dadg_church_favour_reply",
                "dadg_church_talk",
                "{=hM7qGz3V}You stand in the shadow of anathema. Repent, before God and His Church.",
                () => GetFavourRank() == ChurchFavourRank.Reviled,
                null);
            starter.AddPlayerLine(
                "dadg_church_bishop_blessing",
                "dadg_church_talk",
                "dadg_church_bishop_blessing_reply",
                "{=cX4bPk8R}Grant me your blessing, Your Grace.",
                CanRequestBishopBlessing,
                GrantBishopBlessing);
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

        private bool IsConversationWithBishop()
        {
            if (CharacterObject.OneToOneConversationCharacter?.Occupation != Occupation.Preacher) return false;

            var settlement = Hero.OneToOneConversationHero?.CurrentSettlement;
            return settlement != null && _churchSettlements.IsCathedral(settlement);
        }

        private bool CanRequestBishopBlessing()
        {
            if (!IsConversationWithBishop()) return false;

            float? daysSinceLastBlessing = _lastBishopBlessingTime == CampaignTime.Never
                ? (float?)null
                : _lastBishopBlessingTime.ElapsedDaysUntilNow;
            return BishopBlessingPolicy.Evaluate(GetFavourRank(), daysSinceLastBlessing) ==
                   BishopBlessingOutcome.Allowed;
        }

        private void GrantBishopBlessing()
        {
            MobileParty.MainParty.RecentEventsMorale += _churchSettingsProvider.GetSettings().BlessingMorale;
            GainRenownAction.Apply(Hero.MainHero, BishopBlessingPolicy.RenownGain);
            _lastBishopBlessingTime = CampaignTime.Now;
        }

        // The Church's favour is the average of the player's relation with every living clergy
        // notable. Read from CharacterRelationManager: the raw store ChangeRelationAction writes
        // (Hero.GetRelation would add personality trait effects on top).
        private ChurchFavourRank GetFavourRank()
        {
            var relationSum = 0f;
            var clergyCount = 0;
            foreach (var settlement in Settlement.All)
            {
                if (!_churchSettlements.IsChurchSettlement(settlement)) continue;

                foreach (var clergy in settlement.Notables)
                {
                    if (!clergy.IsPreacher || !clergy.IsAlive) continue;

                    relationSum += CharacterRelationManager.GetHeroRelation(Hero.MainHero, clergy);
                    clergyCount++;
                }
            }

            var averageRelation = clergyCount == 0 ? 0f : relationSum / clergyCount;
            return ChurchFavourPolicy.Evaluate(averageRelation);
        }
    }
}

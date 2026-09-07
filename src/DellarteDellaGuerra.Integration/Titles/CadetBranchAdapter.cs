using System.Linq;
using DellarteDellaGuerra.Titles.Api.Campaign;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.Titles
{
    // Adapts the ICadetBranch port to Bannerlord's clan API, following the shape of vanilla's
    // Clan.CreateSettlementRebelClan: create the clan, dress it, seat it, then move heroes in.
    public class CadetBranchAdapter : ICadetBranch
    {
        public string? Split(string claimantHeroId, string parentClanId, string seatSettlementId)
        {
            var claimant = Campaign.Current?.CampaignObjectManager.Find<Hero>(claimantHeroId);
            var parent = Campaign.Current?.CampaignObjectManager.Find<Clan>(parentClanId);
            if (claimant is null || parent is null || parent.Kingdom is null) return null;

            Settlement? seat = Settlement.Find(seatSettlementId);

            var cadet = Clan.CreateClan(parentClanId + "_cadet");
            var name = CadetName(parent, seat);
            cadet.ChangeClanName(name, name);
            cadet.Culture = parent.Culture;
            cadet.Banner = Banner.CreateOneColoredBannerWithOneIcon(
                parent.Banner.GetPrimaryColor(), parent.Banner.GetFirstIconColor(), -1);
            cadet.Color = parent.Color;
            cadet.Color2 = parent.Color2;
            // A cadet branch inherits half its father's house standing, which is what sets its
            // tier: it is a lesser house from the day it is founded, not a landless one.
            cadet.AddRenown(parent.Renown / 2f, shouldNotify: false);
            cadet.IsNoble = true;
            cadet.SetInitialHomeSettlement(seat ?? parent.HomeSettlement);

            // A man leaving his father's house takes his own children with him, as he would on
            // marrying out, and the claim descends through them if he dies pressing it.
            foreach (var member in claimant.Children.Concat(new[] { claimant }).ToList())
            {
                Move(member, cadet);
            }

            cadet.SetLeader(claimant);

            // The war is fought inside one realm, so the new house has to be in it: outside a
            // kingdom there is no hierarchy to place it in and no private war to declare.
            ChangeKingdomAction.ApplyByJoinToKingdom(cadet, parent.Kingdom, default, showNotification: false);
            CampaignEventDispatcher.Instance.OnClanCreated(cadet, isCompanion: false);

            return cadet.StringId;
        }

        public void Reabsorb(string cadetClanId, string parentClanId)
        {
            var cadet = Campaign.Current?.CampaignObjectManager.Find<Clan>(cadetClanId);
            var parent = Campaign.Current?.CampaignObjectManager.Find<Clan>(parentClanId);
            if (cadet is null || parent is null) return;

            foreach (var member in cadet.Heroes.Where(hero => hero.IsAlive).ToList())
            {
                Move(member, parent);
            }

            // Moving a hero out does not stop his old clan calling him its leader, and
            // DestroyClanAction kills whoever it still calls that. The shell has to be
            // leaderless before it is destroyed or the reabsorbed claimant dies with it.
            cadet.SetLeader(null);
            DestroyClanAction.Apply(cadet);
        }

        // A party's clan is fixed when the party is created and never follows its leader
        // afterwards, so a hero who changes house keeps fighting for the old one until his
        // party is moved too.
        private static void Move(Hero hero, Clan clan)
        {
            hero.Clan = clan;
            if (hero.PartyBelongedTo?.LeaderHero == hero) hero.PartyBelongedTo.ActualClan = clan;
        }

        private static TextObject CadetName(Clan parent, Settlement? seat)
        {
            var name = new TextObject("{=!}{PARENT} of {SEAT}");
            name.SetTextVariable("PARENT", parent.InformalName);
            name.SetTextVariable("SEAT", (seat ?? parent.HomeSettlement).Name);

            return name;
        }
    }
}

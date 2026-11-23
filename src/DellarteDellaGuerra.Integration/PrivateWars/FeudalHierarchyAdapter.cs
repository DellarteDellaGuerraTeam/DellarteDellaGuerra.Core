using Bannerlord.PrivateWars.Domain.Port;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Infrastructure.Titles;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace DellarteDellaGuerra.Integration.PrivateWars
{
    // Adapts the existing feudal suzerain query to the ISuzerainProvider port the private-war
    // registry walks to resolve war sides, keeping the two domain features decoupled.
    public class FeudalHierarchyAdapter : CampaignBehaviorBase, IVersionedSuzerainProvider
    {
        private readonly SuzeraintyPolicy _suzeraintyPolicy;
        private readonly InMemoryTitleRegistry _titles;
        private readonly XmlFeudalStructure _structure;
        private long _clanRevision;

        public FeudalHierarchyAdapter(SuzeraintyPolicy suzeraintyPolicy,
            InMemoryTitleRegistry titles, XmlFeudalStructure structure)
        {
            _suzeraintyPolicy = suzeraintyPolicy;
            _titles = titles;
            _structure = structure;
        }

        public long? Revision => unchecked(_titles.Revision + _structure.Revision + Interlocked.Read(ref _clanRevision));

        public override void RegisterEvents()
        {
            // A title stays attached to its holder hero when that hero changes clan.
            CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnHeroChangedClan);
            // Companions derive their clan from CompanionOf instead of their birth clan.
            CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnCompanionAdded);
            CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
            // Allegiance is cut at realm borders, so a clan switching kingdom can change its liege.
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
        }

        private void OnHeroChangedClan(Hero hero, Clan oldClan) => Interlocked.Increment(ref _clanRevision);

        private void OnCompanionAdded(Hero hero) => Interlocked.Increment(ref _clanRevision);

        private void OnCompanionRemoved(Hero hero, RemoveCompanionAction.RemoveCompanionDetail detail)
            => Interlocked.Increment(ref _clanRevision);

        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
            => Interlocked.Increment(ref _clanRevision);

        public override void SyncData(IDataStore dataStore) { }

        // The de jure liege, skipping holders who sit in another kingdom: England's crown is Lancaster's,
        // but the York realm at war with it does not fight Lancaster's private wars.
        public string? GetSuzerain(string clanId)
        {
            var kingdom = Clan.FindFirst(c => c.StringId == clanId)?.Kingdom;
            var visited = new HashSet<string> { clanId };
            string? suzerain = _suzeraintyPolicy.GetSuzerain(clanId);
            while (suzerain != null && visited.Add(suzerain))
            {
                if (Clan.FindFirst(c => c.StringId == suzerain)?.Kingdom == kingdom) return suzerain;
                suzerain = _suzeraintyPolicy.GetSuzerain(suzerain);
            }
            return null;
        }
    }
}

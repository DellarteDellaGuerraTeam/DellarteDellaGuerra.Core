using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>How deep the owners view splits each kingdom's land.</summary>
    public enum PoliticalMapOwnerLevel
    {
        /// <summary>Zoomed out: the ruler's own land and one region per direct vassal of the ruler.</summary>
        Vassals,

        /// <summary>
        /// Zoomed in: the ruler's own land stays whole; each direct vassal's region splits into the
        /// vassal's own land and one region per vassal of theirs.
        /// </summary>
        SubVassals
    }

    /// <summary>
    /// Who answers to whom, as the owners view groups land, from the live titles and the clans'
    /// kingdoms at one moment. A clan's liege is the clan holding (occupying, else holding) the
    /// first title above the clan's highest title whose controller holds a strictly higher dignity,
    /// walking up the configured allegiance chain. That liege must belong to the clan's own kingdom,
    /// else the clan answers to its kingdom's ruler, and so does a clan holding no title. A ruler
    /// and a kingdomless clan answer to no one. Every chain in a kingdom so ends at its ruler; a chain that loops is cut to the
    /// clan and its ruler.
    /// </summary>
    /// <remarks>
    /// De jure titles cross kingdoms (England's crown is Lancaster's, while York is sworn to
    /// another kingdom), so the walk up the titles stops being trusted where it leaves the
    /// clan's kingdom.
    /// </remarks>
    public sealed class PoliticalMapVassalHierarchy
    {
        private readonly IFeudalStructure _feudalStructure;
        private readonly IPoliticalMapWorld _world;
        private readonly Dictionary<string, string> _controllerOfTitle = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TitleRank> _rankOfTitle = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Title> _highestOfClan = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Title> _highestLandTitleOfClan = new(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyList<string>> _chains = new(StringComparer.Ordinal);

        public PoliticalMapVassalHierarchy(
            IFeudalStructure feudalStructure,
            ITitleRepository titleRepository,
            IGenealogy genealogy,
            IPoliticalMapWorld world)
        {
            _feudalStructure = feudalStructure;
            _world = world;
            foreach (Title title in titleRepository.GetAllTitles())
            {
                _rankOfTitle[title.Id] = title.Rank;
                if ((title.OccupantClanId ?? genealogy.GetHolderClanOf(title)) is not { } clan) continue;

                _controllerOfTitle[title.Id] = clan;
                if (!_highestOfClan.TryGetValue(clan, out Title? best) || Outranks(title, best)) _highestOfClan[clan] = title;
                if (title.Rank < TitleRank.King && (!_highestLandTitleOfClan.TryGetValue(clan, out Title? land) || NamesLandBefore(title, land)))
                    _highestLandTitleOfClan[clan] = title;
            }
        }

        /// <summary>
        /// The highest title a clan holds right now (occupied, or held and not occupied by
        /// another): highest rank, then held longest, then by id; null for a clan holding none.
        /// </summary>
        public Title? HighestTitleOf(string clanId) =>
            _highestOfClan.TryGetValue(clanId, out Title? title) ? title : null;

        /// <summary>The highest live dignity below a crown; ties favour its broader territorial tier, then tenure and id.</summary>
        public Title? HighestLandTitleOf(string clanId) =>
            _highestLandTitleOfClan.TryGetValue(clanId, out Title? title) ? title : null;

        public string? ControllerOf(string titleId) =>
            _controllerOfTitle.TryGetValue(titleId, out string? clan) ? clan : null;

        public TitleRank? RankOf(string titleId) =>
            _rankOfTitle.TryGetValue(titleId, out TitleRank rank) ? rank : null;

        /// <summary>The clan a clan answers to, null for a ruler, a kingdomless clan or a clan in a kingdom without a ruler.</summary>
        public string? LiegeOf(string clanId)
        {
            if (_world.KingdomOfClan(clanId) is not { } kingdom) return null;
            if (_world.RulerClanOf(kingdom) is not { } ruler || ruler == clanId) return null;

            return TitleLiegeOf(clanId) is { } liege && _world.KingdomOfClan(liege) == kingdom ? liege : ruler;
        }

        /// <summary>
        /// A clan and everyone above it, the clan first and the top (its ruler, or the clan
        /// itself) last.
        /// </summary>
        public IReadOnlyList<string> ChainOf(string clanId)
        {
            if (_chains.TryGetValue(clanId, out IReadOnlyList<string>? known)) return known;

            var chain = new List<string> { clanId };
            var seen = new HashSet<string>(StringComparer.Ordinal) { clanId };
            for (string? liege = LiegeOf(clanId); liege is not null; liege = LiegeOf(liege))
            {
                if (!seen.Add(liege))
                {
                    // A loop: the clan answers straight to its ruler.
                    chain.RemoveRange(1, chain.Count - 1);
                    if (_world.KingdomOfClan(clanId) is { } kingdom && _world.RulerClanOf(kingdom) is { } ruler && ruler != clanId)
                    {
                        chain.Add(ruler);
                    }

                    break;
                }

                chain.Add(liege);
            }

            _chains[clanId] = chain;
            return chain;
        }

        /// <summary>
        /// The clan whose region a settlement owned by <paramref name="ownerClanId"/> is painted
        /// in at a level. <see cref="PoliticalMapOwnerLevel.Vassals"/>: the ruler's direct vassal
        /// the owner is under (the owner itself when it is one), or the ruler for the ruler's own
        /// land. <see cref="PoliticalMapOwnerLevel.SubVassals"/>: that vassal's own vassal the
        /// owner is under, or the vassal itself for its own land, or the ruler for the ruler's. A
        /// kingdomless clan is always its own region.
        /// </summary>
        public string RegionHolderOf(string ownerClanId, PoliticalMapOwnerLevel level)
        {
            IReadOnlyList<string> chain = ChainOf(ownerClanId);
            int depth = level == PoliticalMapOwnerLevel.Vassals ? 2 : 3;
            return chain.Count < depth ? ownerClanId : chain[chain.Count - depth];
        }

        // The first strictly higher-ranking controller along the primary title's allegiance.
        private string? TitleLiegeOf(string clanId)
        {
            if (HighestTitleOf(clanId) is not { } highest) return null;

            var seen = new HashSet<string>(StringComparer.Ordinal) { highest.Id };
            for (string? current = _feudalStructure.GetAllegianceSuzerainTitleId(highest.Id);
                 current is not null && seen.Add(current);
                 current = _feudalStructure.GetAllegianceSuzerainTitleId(current))
            {
                if (_controllerOfTitle.TryGetValue(current, out string? controller) && controller != clanId
                    && HighestTitleOf(controller) is { } liege && liege.Rank > highest.Rank) return controller;
            }

            return null;
        }

        private bool NamesLandBefore(Title title, Title other)
        {
            TitleRank tier = _feudalStructure.GetRank(title.Id) ?? title.Rank;
            TitleRank otherTier = _feudalStructure.GetRank(other.Id) ?? other.Rank;
            // A regional lordship names the domain before an equally ranked castle barony.
            return title.Rank == other.Rank && tier != otherTier ? tier > otherTier : Outranks(title, other);
        }

        private static bool Outranks(Title title, Title other) =>
            title.Rank != other.Rank ? title.Rank > other.Rank
            : title.HeldSinceDay != other.HeldSinceDay ? title.HeldSinceDay < other.HeldSinceDay
            : string.CompareOrdinal(title.Id, other.Id) < 0;
    }
}

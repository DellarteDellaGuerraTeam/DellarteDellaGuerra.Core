using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>Actual title domains: owned land, attached vassals, and the primary title's remaining dependants.</summary>
    public sealed class PoliticalMapTitleDomains
    {
        private readonly IFeudalStructure _structure;
        private readonly IPoliticalMapWorld _world;
        private readonly PoliticalMapVassalHierarchy _hierarchy;

        public PoliticalMapTitleDomains(IFeudalStructure structure, IPoliticalMapWorld world,
            PoliticalMapVassalHierarchy hierarchy)
        {
            _structure = structure;
            _world = world;
            _hierarchy = hierarchy;
        }

        public string? KeyOf(string settlementId, PoliticalMapOwnerLevel level)
        {
            if (_world.OwnerClanOf(settlementId) is not { } owner) return null;
            string holder = _hierarchy.RegionHolderOf(owner, level);
            string? title = LocalDomainOf(settlementId, holder);
            return title is null || _hierarchy.RankOf(title) == TitleRank.King
                ? PoliticalMapPainter.SettlementTerritoryPrefix + (_world.SeatOf(settlementId) ?? settlementId)
                : level == PoliticalMapOwnerLevel.Vassals ? SuperiorOwnedTitle(title, holder) : title;
        }

        /// <summary>The local territory displayed at close zoom, rather than its holder's whole dignity.</summary>
        public bool ContainsLocal(string titleId, string settlementId) =>
            KeyOf(settlementId, PoliticalMapOwnerLevel.SubVassals) == titleId;

        /// <summary>Membership is independent of camera level and never includes an unrelated owner's land.</summary>
        public bool Contains(string titleId, string settlementId)
        {
            if (_hierarchy.ControllerOf(titleId) is not { } holder) return false;
            string? local = LocalDomainOf(settlementId, holder);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (string? current = local; current is not null && seen.Add(current);
                 current = _structure.GetDeJureSuzerainTitleId(current))
                if (current == titleId) return true;
            return local is not null && SuperiorOwnedTitle(local, holder) == titleId;
        }

        private string? LocalDomainOf(string settlementId, string holder)
        {
            if (_world.OwnerClanOf(settlementId) is not { } owner) return null;
            IReadOnlyList<string> chain = _hierarchy.ChainOf(owner);
            int holderIndex = -1;
            for (int i = 0; i < chain.Count; i++)
                if (chain[i] == holder) { holderIndex = i; break; }
            if (holderIndex < 0) return null;

            Title? primary = _hierarchy.HighestTitleOf(holder);
            if (owner == holder)
            {
                string seat = _world.SeatOf(settlementId) ?? settlementId;
                string? local = _structure.GetTitleIdBySeat(seat);
                if (local is not null && _hierarchy.ControllerOf(local) == holder) return local;
                // Personal land follows the holder's landed dignity. Realm vassals follow their allegiance below.
                return primary?.Rank == TitleRank.King ? _hierarchy.HighestLandTitleOf(holder)?.Id ?? primary.Id : primary?.Id;
            }

            // A whole vassal branch follows its direct lord's attachment, including distant holdings.
            string branch = chain[holderIndex - 1];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (string? current = _hierarchy.HighestTitleOf(branch)?.Id;
                 current is not null && seen.Add(current);
                 current = _structure.GetAllegianceSuzerainTitleId(current))
                if (_structure.GetRank(current) < TitleRank.King && _hierarchy.ControllerOf(current) == holder)
                    return current;

            // Ruling a faction does not attach all its subjects to the ruler's personal duchy.
            if (_world.KingdomOfClan(holder) is { } kingdom && _world.RulerClanOf(kingdom) == holder
                && primary?.Rank < TitleRank.King) return null;
            return primary?.Id;
        }

        private string SuperiorOwnedTitle(string title, string holder)
        {
            string result = title;
            var seen = new HashSet<string>(StringComparer.Ordinal) { title };
            for (string? current = _structure.GetDeJureSuzerainTitleId(title);
                 current is not null && seen.Add(current);
                 current = _structure.GetDeJureSuzerainTitleId(current))
                if (_structure.GetRank(current) < TitleRank.King && _hierarchy.ControllerOf(current) == holder)
                    result = current;
            // Allegiance can attach constituent estates whose geographic nodes are siblings.
            seen.Clear();
            seen.Add(title);
            for (string? current = _structure.GetAllegianceSuzerainTitleId(title);
                 current is not null && seen.Add(current);
                 current = _structure.GetAllegianceSuzerainTitleId(current))
                if (_hierarchy.RankOf(current) < TitleRank.King && _hierarchy.ControllerOf(current) == holder
                    && _hierarchy.RankOf(current) >= _hierarchy.RankOf(result))
                    result = current;
            // Lower personal titles belong to the main domain at broad zoom, even outside its de jure bounds.
            Title? primary = _hierarchy.HighestTitleOf(holder);
            return primary is not null && primary.Rank < TitleRank.King
                && _hierarchy.RankOf(result) < primary.Rank ? primary.Id : result;
        }
    }
}

using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.PoliticalMap
{
    /// <summary>
    /// Groups the political map's settlement cells into territories, the unit the overlay builds
    /// and recolours its meshes by. A territory is a county, named by its title id: the county's
    /// own seat and the seat of every barony below it. A seated title with no county above it (a
    /// duchy seated at a castle, a barony straight under a duchy) is a territory of its own.
    /// </summary>
    /// <remarks>
    /// Read from the de jure structure on every call rather than cached, so a title reattached to
    /// another realm is answered for where it now sits. Villages are not titles: the caller maps a
    /// village to its bound town or castle before asking.
    /// </remarks>
    public class PoliticalMapTerritories
    {
        private readonly IFeudalStructure _feudalStructure;
        private readonly ITitleRepository _titleRepository;

        public PoliticalMapTerritories(IFeudalStructure feudalStructure, ITitleRepository titleRepository)
        {
            _feudalStructure = feudalStructure;
            _titleRepository = titleRepository;
        }

        /// <summary>
        /// The territory a town or castle is painted with, or null for one that seats no title
        /// and so belongs to no territory.
        /// </summary>
        public string? GetTerritoryOfSeat(string settlementId)
        {
            string? titleId = _feudalStructure.GetTitleIdBySeat(settlementId);
            return titleId is null ? null : GetTerritoryOfTitle(titleId);
        }

        /// <summary>The nearest county at or above the title, or the title itself when none is.</summary>
        public string GetTerritoryOfTitle(string titleId)
        {
            var seen = new HashSet<string>();
            for (string? current = titleId; current is not null && seen.Add(current); current = _feudalStructure.GetDeJureSuzerainTitleId(current))
            {
                if (_feudalStructure.GetRank(current) == TitleRank.Earl) return current;
            }

            return titleId;
        }

        /// <summary>
        /// The territories whose meshes change when the title changes holder or occupant. A county
        /// or barony repaints its county. A title above the counties repaints only its own seat's
        /// cells: the counties below it are filled by their own holders, and the de jure borders
        /// between them do not move when a dignity changes hands. A seatless crown repaints nothing.
        /// </summary>
        public IReadOnlyList<string> GetTerritoriesAffectedByTitle(string titleId)
        {
            TitleRank? rank = _feudalStructure.GetRank(titleId);
            if (rank is null) return new string[0];
            if (rank <= TitleRank.Earl) return new[] { GetTerritoryOfTitle(titleId) };

            return string.IsNullOrEmpty(_titleRepository.GetTitle(titleId)?.SeatSettlementId)
                ? new string[0]
                : new[] { titleId };
        }

        /// <summary>The distinct territories of the given towns and castles, in first-seen order.</summary>
        public IReadOnlyList<string> GetTerritoriesOfSeats(IEnumerable<string> settlementIds)
        {
            var territories = new List<string>();
            var seen = new HashSet<string>();
            foreach (string settlementId in settlementIds)
            {
                string? territory = GetTerritoryOfSeat(settlementId);
                if (territory is not null && seen.Add(territory)) territories.Add(territory);
            }

            return territories;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Church.Port;

namespace DellarteDellaGuerra.Domain.Church.Hierarchy
{
    /**
     * <summary>
     * Builds the two-level church hierarchy tree from the configured dioceses: one see root per
     * diocese whose settlement id is the derived cathedral seat, with the non-cathedral members
     * as children ordered abbeys before priories.
     * </summary>
     */
    public class BuildChurchMapUseCase : IBuildChurchMapUseCase
    {
        private readonly IChurchSettlementsProvider _churchSettlementsProvider;

        public BuildChurchMapUseCase(IChurchSettlementsProvider churchSettlementsProvider)
        {
            _churchSettlementsProvider = churchSettlementsProvider;
        }

        public ChurchMap Execute()
        {
            var roots = new List<ChurchMapEntry>();
            foreach (var diocese in _churchSettlementsProvider.GetDioceses())
            {
                var seat = diocese.Members.FirstOrDefault(
                    member => member.Kind == ChurchSettlementKind.Cathedral);
                if (seat is null) continue;

                var children = diocese.Members
                    .Where(member => member.Kind != ChurchSettlementKind.Cathedral)
                    .OrderBy(member => member.Kind == ChurchSettlementKind.Abbey ? 0 : 1)
                    .Select(member => new ChurchMapEntry(
                        member.SettlementId,
                        string.Empty,
                        RankOf(member.Kind),
                        member.SettlementId,
                        new List<ChurchMapEntry>()))
                    .ToList();

                roots.Add(new ChurchMapEntry(
                    diocese.Id, diocese.Name, ChurchNodeRank.See, seat.SettlementId, children));
            }

            return new ChurchMap(roots);
        }

        private static ChurchNodeRank RankOf(ChurchSettlementKind kind) =>
            kind == ChurchSettlementKind.Priory ? ChurchNodeRank.Priory : ChurchNodeRank.Abbey;
    }
}

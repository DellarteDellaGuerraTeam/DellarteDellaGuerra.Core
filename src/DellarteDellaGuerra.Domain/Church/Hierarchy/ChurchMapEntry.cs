using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Church.Hierarchy
{
    /**
     * <summary>
     * One node of the church hierarchy tree: a diocese (see) root or a member church settlement.
     * For a see, <see cref="SettlementId"/> is the derived cathedral seat and <see cref="Name"/>
     * the diocese name; for a member, the settlement id doubles as the id and the display name is
     * resolved from the campaign through a domain port when building the ledger.
     * </summary>
     */
    public class ChurchMapEntry
    {
        public string Id { get; }
        public string Name { get; }
        public ChurchNodeRank Rank { get; }
        public string SettlementId { get; }
        public IReadOnlyList<ChurchMapEntry> Children { get; }

        public ChurchMapEntry(
            string id,
            string name,
            ChurchNodeRank rank,
            string settlementId,
            IReadOnlyList<ChurchMapEntry> children)
        {
            Id = id;
            Name = name;
            Rank = rank;
            SettlementId = settlementId;
            Children = children;
        }
    }
}

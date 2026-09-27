using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church.Hierarchy;

namespace DellarteDellaGuerra.Domain.Church.Ledger
{
    public class ChurchLedgerEntry
    {
        public ChurchLedgerEntry(
            string id,
            string name,
            string settlementId,
            string settlementName,
            ChurchNodeRank rank,
            bool isAvailable,
            string? clergyName,
            ClergyOffice? clergyOffice,
            int? playerRelation,
            float? clergyPower,
            string? ownerName,
            string? factionName,
            ChurchFoundationStatus status,
            IReadOnlyList<ChurchLedgerEntry> children)
        {
            Id = id;
            Name = name;
            SettlementId = settlementId;
            SettlementName = settlementName;
            Rank = rank;
            IsAvailable = isAvailable;
            ClergyName = clergyName;
            ClergyOffice = clergyOffice;
            PlayerRelation = playerRelation;
            ClergyPower = clergyPower;
            OwnerName = ownerName;
            FactionName = factionName;
            Status = status;
            Children = children;
        }

        public string Id { get; }
        public string Name { get; }
        public string SettlementId { get; }
        public string SettlementName { get; }
        public ChurchNodeRank Rank { get; }
        public bool IsAvailable { get; }
        public string? ClergyName { get; }
        public ClergyOffice? ClergyOffice { get; }
        public int? PlayerRelation { get; }
        public float? ClergyPower { get; }
        public string? OwnerName { get; }
        public string? FactionName { get; }
        public ChurchFoundationStatus Status { get; }
        public IReadOnlyList<ChurchLedgerEntry> Children { get; }
    }
}

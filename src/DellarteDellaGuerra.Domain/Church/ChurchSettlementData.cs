namespace DellarteDellaGuerra.Domain.Church
{
    public class ChurchSettlementData
    {
        public string SettlementId { get; }
        public ChurchSettlementKind Kind { get; }

        public ChurchSettlementData(string settlementId, ChurchSettlementKind kind)
        {
            SettlementId = settlementId;
            Kind = kind;
        }
    }
}

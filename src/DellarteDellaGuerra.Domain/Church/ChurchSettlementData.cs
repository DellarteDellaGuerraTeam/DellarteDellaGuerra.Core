namespace DellarteDellaGuerra.Domain.Church
{
    public class ChurchSettlementData
    {
        public string SettlementId { get; }
        public ChurchSettlementKind Kind { get; }
        public bool IsShrine { get; }

        public ChurchSettlementData(string settlementId, ChurchSettlementKind kind, bool isShrine = false)
        {
            SettlementId = settlementId;
            Kind = kind;
            IsShrine = isShrine;
        }
    }
}

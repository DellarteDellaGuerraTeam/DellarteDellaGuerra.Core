namespace DellarteDellaGuerra.Domain.Titles.Model
{
    public record Title(string Id, string Name, TitleRank Rank, string SeatSettlementId, string? HolderClanId)
    {
        public Title WithHolder(string? holderClanId) => this with { HolderClanId = holderClanId };
    }
}

namespace DellarteDellaGuerra.Domain.PoliticalMap.Grid
{
    /// <summary>A town, castle or village the political map grows a patch of land from, at its map position.</summary>
    public readonly struct PoliticalMapSeed
    {
        public PoliticalMapSeed(string settlementId, float x, float y)
        {
            SettlementId = settlementId;
            X = x;
            Y = y;
        }

        public string SettlementId { get; }

        public float X { get; }

        public float Y { get; }
    }
}

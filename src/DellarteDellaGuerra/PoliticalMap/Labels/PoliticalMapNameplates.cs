namespace DellarteDellaGuerra.PoliticalMap.Labels
{
    /// <summary>
    /// Whether the map's settlement nameplates give way to the political map's region labels. Read
    /// by a nameplate patch on the nameplate worker threads every frame, so clearing it brings the
    /// nameplates back on the next frame.
    /// </summary>
    public static class PoliticalMapNameplates
    {
        private static volatile bool _hideSettlementNameplates;

        public static bool HideSettlementNameplates
        {
            get => _hideSettlementNameplates;
            set => _hideSettlementNameplates = value;
        }
    }
}

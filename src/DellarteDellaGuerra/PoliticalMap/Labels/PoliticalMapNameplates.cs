namespace DellarteDellaGuerra.PoliticalMap.Labels
{
    /// <summary>
    /// Whether native map overlays give way to the political map's region labels. Read by
    /// visibility patches, including on nameplate worker threads; clearing it restores native UI.
    /// </summary>
    public static class PoliticalMapNameplates
    {
        private static volatile bool _hideNativeOverlays;

        public static bool HideNativeOverlays
        {
            get => _hideNativeOverlays;
            set => _hideNativeOverlays = value;
        }
    }
}

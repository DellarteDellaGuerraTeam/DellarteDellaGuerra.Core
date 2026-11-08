namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>What the political map overlay paints, in the order the toggle cycles through them.</summary>
    public enum PoliticalMapView
    {
        /// <summary>Each settlement in its controlling faction's colour.</summary>
        Kingdom,

        /// <summary>
        /// The vassal hierarchy: each kingdom's land split by who answers to whom
        /// (<see cref="PoliticalMapVassalHierarchy"/>), a level deeper as the camera comes in.
        /// </summary>
        Owners,

        /// <summary>Nothing drawn.</summary>
        Off,

        /// <summary>The configured geographic title hierarchy, independent of actual ownership.</summary>
        DeJure
    }

    public static class PoliticalMapViews
    {
        /// <summary>The view the toggle moves to next, wrapping from Off back to the first.</summary>
        public static PoliticalMapView Next(PoliticalMapView view) => view switch
        {
            PoliticalMapView.Kingdom => PoliticalMapView.Owners,
            PoliticalMapView.Owners => PoliticalMapView.DeJure,
            PoliticalMapView.DeJure => PoliticalMapView.Off,
            _ => PoliticalMapView.Kingdom
        };
    }
}

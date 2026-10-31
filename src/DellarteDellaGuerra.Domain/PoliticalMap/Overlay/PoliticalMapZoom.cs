namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    public enum PoliticalMapZoomBand
    {
        Near,
        Mid,
        Far
    }

    /// <summary>
    /// Whether the wash shows at the camera's distance. Close in, it would hide the terrain and the
    /// parties on it, so it is switched off; further out it shows.
    /// </summary>
    /// <remarks>
    /// The wash cannot fade smoothly: its translucency lives in the vertex alpha alone, so fading
    /// it would mean rebuilding every mesh. It is switched off instead.
    /// </remarks>
    public static class PoliticalMapZoom
    {
        /// <summary>Camera distance below which the camera is near (the campaign's default distance is 38).</summary>
        public const float NearBelow = 20f;

        /// <summary>Camera distance above which the camera is far.</summary>
        public const float FarAbove = 120f;

        public static PoliticalMapZoomBand BandOf(float cameraDistance) =>
            cameraDistance < NearBelow ? PoliticalMapZoomBand.Near
            : cameraDistance > FarAbove ? PoliticalMapZoomBand.Far
            : PoliticalMapZoomBand.Mid;

        public static bool ShowsWash(PoliticalMapZoomBand band) => band != PoliticalMapZoomBand.Near;

        /// <summary>
        /// How far back below <see cref="FarAbove"/> the camera must come before the owners view
        /// splits its vassals again, so a camera resting on the threshold does not flip the map.
        /// </summary>
        public const float OwnerLevelHysteresis = 10f;

        /// <summary>
        /// The owners view's level at a camera distance, given the current one: the direct vassals
        /// once the camera is far (beyond <see cref="FarAbove"/>), their own vassals once it is
        /// back under <see cref="FarAbove"/> less <see cref="OwnerLevelHysteresis"/>; in between it
        /// keeps the current level.
        /// </summary>
        public static PoliticalMapOwnerLevel OwnerLevelFor(float cameraDistance, PoliticalMapOwnerLevel current) =>
            cameraDistance > FarAbove ? PoliticalMapOwnerLevel.Vassals
            : cameraDistance < FarAbove - OwnerLevelHysteresis ? PoliticalMapOwnerLevel.SubVassals
            : current;
    }
}

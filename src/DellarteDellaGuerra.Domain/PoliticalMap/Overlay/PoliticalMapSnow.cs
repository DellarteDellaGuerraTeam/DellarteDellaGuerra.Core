using System;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>
    /// Whether the campaign map's snow is held back while the overlay is drawn, so the washes read
    /// over the northern counties in winter. The map's snow follows a season factor (0 midsummer,
    /// 1 midwinter) the map screen sets every frame; while hidden it is capped at
    /// <see cref="SeasonFactor"/>, and the moment the overlay is off the season's own value returns.
    /// </summary>
    public sealed class PoliticalMapSnow
    {
        /// <summary>Midsummer: the least snow the season ever shows.</summary>
        public const float DefaultSeasonFactor = 0f;

        public bool HideWhileShown { get; set; } = true;

        /// <summary>The season factor snow is capped at while hidden; below 0 also clears the snowiest peaks.</summary>
        public float SeasonFactor { get; set; } = DefaultSeasonFactor;

        /// <summary>The snow season factor to draw the map with, given the season's own.</summary>
        public float SeasonFactorFor(PoliticalMapView view, float seasonFactor) =>
            HideWhileShown && view != PoliticalMapView.Off ? Math.Min(seasonFactor, SeasonFactor) : seasonFactor;
    }
}

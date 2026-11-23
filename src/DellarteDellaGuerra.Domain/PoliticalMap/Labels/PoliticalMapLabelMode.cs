using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Labels
{
    /// <summary>Whether ownership domains are grouped by titles or by controlling clans.</summary>
    public enum PoliticalMapLabelMode
    {
        /// <summary>Actual title domains (the realm in Kingdoms), including their attached vassals.</summary>
        Titles,

        /// <summary>Each region's holder clan.</summary>
        Clans
    }

    /// <summary>
    /// Each view keeps its own label choice for the session. Kingdoms start on titles;
    /// owners start on clans.
    /// </summary>
    public sealed class PoliticalMapLabelModes
    {
        private readonly Dictionary<PoliticalMapView, PoliticalMapLabelMode> _modes = new();

        /// <summary>Whether a view's labels can name either titles or clans.</summary>
        public static bool HasChoice(PoliticalMapView view) => view is PoliticalMapView.Kingdom or PoliticalMapView.Owners;

        /// <summary>The naming a view starts on.</summary>
        public static PoliticalMapLabelMode DefaultOf(PoliticalMapView view) =>
            view is PoliticalMapView.Kingdom or PoliticalMapView.DeJure ? PoliticalMapLabelMode.Titles : PoliticalMapLabelMode.Clans;

        /// <summary>Reads "title(s)" or "clan(s)", any case.</summary>
        public static bool TryParse(string? text, out PoliticalMapLabelMode mode)
        {
            switch (text?.Trim().ToLowerInvariant())
            {
                case "title":
                case "titles":
                    mode = PoliticalMapLabelMode.Titles;
                    return true;
                case "clan":
                case "clans":
                    mode = PoliticalMapLabelMode.Clans;
                    return true;
                default:
                    mode = default;
                    return false;
            }
        }

        /// <summary>Raised when a view's mode changes.</summary>
        public event Action? Changed;

        /// <summary>A view's mode; off always answers its default.</summary>
        public PoliticalMapLabelMode Of(PoliticalMapView view) =>
            HasChoice(view) && _modes.TryGetValue(view, out PoliticalMapLabelMode mode) ? mode : DefaultOf(view);

        /// <summary>Sets a view's mode. Does nothing for a view without the choice.</summary>
        public void Set(PoliticalMapView view, PoliticalMapLabelMode mode)
        {
            if (!HasChoice(view) || Of(view) == mode) return;
            _modes[view] = mode;
            Changed?.Invoke();
        }

        /// <summary>Flips a view between titles and clans.</summary>
        public void Toggle(PoliticalMapView view) =>
            Set(view, Of(view) == PoliticalMapLabelMode.Titles ? PoliticalMapLabelMode.Clans : PoliticalMapLabelMode.Titles);
    }
}

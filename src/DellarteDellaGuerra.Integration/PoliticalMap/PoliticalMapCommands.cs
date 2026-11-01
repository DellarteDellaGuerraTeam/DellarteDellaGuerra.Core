using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.PoliticalMap.Api.Campaign;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.PoliticalMap
{
    /// <summary>Console switches for the political map overlay, the same ones as its map bar panel.</summary>
    public static class PoliticalMapCommands
    {
        // polmap.view [next|Kingdom|Owners|Off]
        [CommandLineFunctionality.CommandLineArgumentFunction("view", "polmap")]
        public static string View(List<string> args)
        {
            PoliticalMapOverlayCampaignBehavior? overlay = Overlay();
            if (overlay is null) return "Start or load a campaign first.";

            string names = string.Join("|", Enum.GetNames(typeof(PoliticalMapView)));
            if (args.Count == 0) return $"Political map view: {overlay.View}. Usage: polmap.view next|{names}";

            if (args[0].Equals("next", StringComparison.OrdinalIgnoreCase)) overlay.CycleView();
            else if (Enum.TryParse(args[0], true, out PoliticalMapView view)) overlay.SetView(view);
            else return $"Unknown view '{args[0]}'. Usage: polmap.view next|{names}";

            return $"Political map view: {overlay.View} (owners level {overlay.OwnerLevel}).";
        }

        // polmap.snow [0|1] [season factor]: 0 hides the map's snow while a view is drawn (the
        // default), capped at the season factor (0 midsummer, the default; -1 also clears the
        // peaks); 1 leaves the snow to the season.
        [CommandLineFunctionality.CommandLineArgumentFunction("snow", "polmap")]
        public static string Snow(List<string> args)
        {
            PoliticalMapOverlayCampaignBehavior? overlay = Overlay();
            if (overlay is null) return "Start or load a campaign first.";

            const string usage = "Usage: polmap.snow [0|1] [season factor, 0 midsummer .. 1 midwinter]";
            PoliticalMapSnow snow = overlay.Snow;
            if (args.Count > 0)
            {
                if (args[0] == "0") snow.HideWhileShown = true;
                else if (args[0] == "1") snow.HideWhileShown = false;
                else return usage;

                if (args.Count > 1)
                {
                    if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float factor)) return usage;
                    snow.SeasonFactor = factor;
                }
            }

            return snow.HideWhileShown
                ? $"Map snow: hidden while a view is drawn (season factor capped at {snow.SeasonFactor.ToString(CultureInfo.InvariantCulture)}). {usage}"
                : $"Map snow: follows the season. {usage}";
        }

        // polmap.opacity [0..1]: the fill opacity of every view, repainted at once.
        [CommandLineFunctionality.CommandLineArgumentFunction("opacity", "polmap")]
        public static string Opacity(List<string> args)
        {
            PoliticalMapOverlayCampaignBehavior? overlay = Overlay();
            if (overlay is null) return "Start or load a campaign first.";

            string usage = $"Usage: polmap.opacity <0..1> (default {PoliticalMapPalette.DefaultOpacity.ToString(CultureInfo.InvariantCulture)}; 1 is a flat paper map)";
            if (args.Count > 0)
            {
                if (!float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float opacity)
                    || float.IsNaN(opacity) || opacity < 0f || opacity > 1f) return usage;
                overlay.SetOpacity(opacity);
            }

            return $"Political map opacity: {PoliticalMapPalette.Opacity.ToString("0.00", CultureInfo.InvariantCulture)}. {usage}";
        }

        // polmap.labels [title|clan]: what the current view's region labels name (session only;
        // only the text changes, never the regions).
        [CommandLineFunctionality.CommandLineArgumentFunction("labels", "polmap")]
        public static string Labels(List<string> args)
        {
            PoliticalMapOverlayCampaignBehavior? overlay = Overlay();
            PoliticalMapLabelsCampaignBehavior? labels = Campaign.Current?.GetCampaignBehavior<PoliticalMapLabelsCampaignBehavior>();
            if (overlay is null || labels is null) return "Start or load a campaign first.";

            const string usage = "Usage: polmap.labels title|clan (kingdom or owners view)";
            PoliticalMapView view = overlay.View;
            if (!PoliticalMapLabelModes.HasChoice(view)) return $"The {view} view's labels have no choice. {usage}";

            if (args.Count > 0)
            {
                if (!PoliticalMapLabelModes.TryParse(args[0], out PoliticalMapLabelMode mode)) return $"Unknown label mode '{args[0]}'. {usage}";
                labels.Modes.Set(view, mode);
            }

            return $"Political map labels ({view} view): {labels.Modes.Of(view)}. {usage}";
        }

        private static PoliticalMapOverlayCampaignBehavior? Overlay() =>
            Campaign.Current?.GetCampaignBehavior<PoliticalMapOverlayCampaignBehavior>();
    }
}

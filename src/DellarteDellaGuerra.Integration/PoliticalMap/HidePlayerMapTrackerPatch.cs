using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.PoliticalMap.Labels;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate;

namespace DellarteDellaGuerra.Integration.PoliticalMap
{
    /// <summary>
    /// Hides the player's portrait tracker without changing the player party or its map mesh.
    /// Native visibility runs first every frame, so clearing the overlay flag restores its
    /// current camera-dependent visibility, including on an already-created widget.
    /// </summary>
    public class HidePlayerMapTrackerPatch : IPatch
    {
        private readonly ILogger _logger;

        public HidePlayerMapTrackerPatch(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HidePlayerMapTrackerPatch>();
        }

        public MethodInfo? TargetMethod
        {
            get
            {
                MethodInfo? method = AccessTools.DeclaredMethod(typeof(PartyPlayerNameplateWidget),
                    "UpdateNameplatesVisibility", new[] { typeof(float) });
                if (method is null) _logger.Error("PartyPlayerNameplateWidget.UpdateNameplatesVisibility changed: the player portrait tracker stays over the political map.");
                return method;
            }
        }

        public MethodInfo? PatchMethod => AccessTools.Method(typeof(HidePlayerMapTrackerPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(PartyPlayerNameplateWidget __instance)
        {
            if (!PoliticalMapNameplates.HideNativeOverlays) return;

            // Native resets these three properties on every call; leave the widget itself visible
            // so its late update continues running and can restore the tracker when switched Off.
            __instance.MainPartyArrowWidget.IsVisible = false;
            __instance.TrackerFrame.IsVisible = false;
            __instance.IsEnabled = false;
        }
    }
}

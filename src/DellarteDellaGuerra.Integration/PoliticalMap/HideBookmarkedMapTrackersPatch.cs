using System;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.PoliticalMap.Labels;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;

namespace DellarteDellaGuerra.Integration.PoliticalMap
{
    /// <summary>Hides tracker pins without removing the player's settlement or party bookmarks.</summary>
    public class HideBookmarkedMapTrackersPatch : IPatch
    {
        private readonly ILogger _logger;

        public HideBookmarkedMapTrackersPatch(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HideBookmarkedMapTrackersPatch>();
        }

        public MethodInfo? TargetMethod
        {
            get
            {
                MethodInfo? method = AccessTools.DeclaredMethod(typeof(MapTrackerItemVM),
                    nameof(MapTrackerItemVM.RefreshBinding), Type.EmptyTypes);
                if (method is null) _logger.Error("MapTrackerItemVM.RefreshBinding changed: bookmarked map trackers stay over the political map.");
                return method;
            }
        }

        public MethodInfo? PatchMethod => AccessTools.Method(typeof(HideBookmarkedMapTrackersPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(MapTrackerItemVM __instance)
        {
            // Native refreshes IsEnabled each frame, restoring current visibility when switched Off.
            if (PoliticalMapNameplates.HideNativeOverlays) __instance.IsEnabled = false;
        }
    }
}

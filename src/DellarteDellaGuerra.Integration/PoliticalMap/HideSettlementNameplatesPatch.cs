using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.PoliticalMap.Labels;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.PoliticalMap
{
    /// <summary>
    /// Hides the settlement nameplates while the political map names its regions
    /// (<see cref="PoliticalMapNameplates"/>), by answering "not visible" from the nameplate's own
    /// per-frame visibility check. Settlements under siege or raid and hideouts keep theirs,
    /// unless bookmarked; bookmark state itself is unchanged.
    /// </summary>
    /// <remarks>
    /// The check runs every frame on the nameplate worker threads, so clearing the flag brings
    /// every nameplate back on the next frame, exactly as vanilla would show it.
    /// </remarks>
    public class HideSettlementNameplatesPatch : IPatch
    {
        private readonly ILogger _logger;

        public HideSettlementNameplatesPatch(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HideSettlementNameplatesPatch>();
        }

        public MethodInfo? TargetMethod
        {
            get
            {
                MethodInfo? method = AccessTools.DeclaredMethod(typeof(SettlementNameplateVM), "IsVisible", new[] { typeof(Vec3).MakeByRefType() });
                if (method is null) _logger.Error("SettlementNameplateVM.IsVisible changed: settlement nameplates stay over the political map's labels.");
                return method;
            }
        }

        public MethodInfo? PatchMethod => AccessTools.Method(typeof(HideSettlementNameplatesPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(SettlementNameplateVM __instance, ref bool __result)
        {
            if (!__result || !PoliticalMapNameplates.HideNativeOverlays) return;
            if (!__instance.IsTracked && (__instance.MapEventVisualType != 0 || __instance.Settlement.IsHideout)) return;

            __result = false;
        }
    }
}

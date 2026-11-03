using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.PoliticalMap.Labels;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate;

namespace DellarteDellaGuerra.Integration.PoliticalMap
{
    /// <summary>
    /// The player widget ignores IsVisibleOnMap. Hide its banner and labels after its own
    /// visibility update; leave its portrait and tracking controls to their separate patch.
    /// </summary>
    public class HidePlayerPartyLabelsPatch : IPatch
    {
        private readonly ILogger _logger;

        public HidePlayerPartyLabelsPatch(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HidePlayerPartyLabelsPatch>();
        }

        public MethodInfo? TargetMethod
        {
            get
            {
                MethodInfo? method = AccessTools.DeclaredMethod(typeof(PartyPlayerNameplateWidget),
                    "UpdateNameplatesVisibility", new[] { typeof(float) });
                if (method is null) _logger.Error("PartyPlayerNameplateWidget.UpdateNameplatesVisibility changed: player party labels stay over the political map.");
                return method;
            }
        }

        public MethodInfo? PatchMethod => AccessTools.Method(typeof(HidePlayerPartyLabelsPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(PartyPlayerNameplateWidget __instance)
        {
            if (!PoliticalMapNameplates.HideNativeOverlays) return;

            __instance.PartyBannerWidget.IsVisible = false;
            __instance.NameplateTextWidget.IsVisible = false;
            __instance.NameplateFullNameTextWidget.IsVisible = false;
            __instance.NameplateExtraInfoTextWidget.IsVisible = false;
            __instance.SpeedTextWidget.IsVisible = false;
            __instance.SpeedIconWidget.IsVisible = false;
            __instance.DisorganizedWidget.IsVisible = false;
            // Vanilla updates this icon's alpha, not its visibility, so let that restore it.
            __instance.ParleyIconWidget.AlphaFactor = 0f;
        }
    }
}

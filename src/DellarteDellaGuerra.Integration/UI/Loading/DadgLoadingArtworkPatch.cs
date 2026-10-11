using System;
using HarmonyLib;

namespace DellarteDellaGuerra.Integration.UI.Loading;

internal static class DadgLoadingArtworkPatch
{
    internal static void Apply()
    {
        // War Sails installs its loading manager on the first application tick,
        // after OnSubModuleLoad. Do not reference the optional DLC assembly.
        var navalManager = AccessTools.TypeByName("NavalDLC.GauntletUI.GauntletNavalLoadingWindowManager");
        if (navalManager == null) return;

        var target = AccessTools.DeclaredMethod(navalManager, "GetSpriteCategoryName")
                     ?? throw new MissingMethodException(navalManager.FullName, "GetSpriteCategoryName");
        new HarmonyLib.Harmony("com.dadg.loading-artwork").Patch(target,
            prefix: new HarmonyMethod(typeof(DadgLoadingArtworkPatch), nameof(UseDadgArtwork)));
    }

    private static bool UseDadgArtwork(ref string __result)
    {
        // DADG Map replaces all twelve ui_loading textures. Keep the native
        // partial-load/unload lifecycle and image rotation, only change its pool.
        __result = "ui_loading";
        return false;
    }
}

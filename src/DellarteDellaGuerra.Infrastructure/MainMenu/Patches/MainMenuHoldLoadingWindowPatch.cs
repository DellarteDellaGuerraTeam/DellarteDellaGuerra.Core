using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Engine;

namespace DellarteDellaGuerra.Infrastructure.MainMenu.Patches;

/// <summary>
/// 1.4's MBInitialScreenBase.OnFrameTick (sealed) drops the loading window every frame, a leftover of the
/// video menu. With the 3D scene back that shows a black, half-loaded menu, so the drop is vetoed until
/// <see cref="DadgInitialScreen"/> reports its scene ready, as 1.2 did.
/// </summary>
public class MainMenuHoldLoadingWindowPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(LoadingWindow), nameof(LoadingWindow.DisableGlobalLoadingWindow));

    public MethodInfo? PatchMethod => AccessTools.Method(typeof(MainMenuHoldLoadingWindowPatch), nameof(Hold));

    public PatchType PatchType => PatchType.Prefix;

    private static bool Hold() => !DadgInitialScreen.IsHoldingLoadingWindow;
}

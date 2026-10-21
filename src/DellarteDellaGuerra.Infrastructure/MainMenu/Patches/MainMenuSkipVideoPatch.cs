using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.MainMenu.Patches;

/// <summary>
/// RefreshScene picks a random Native/NavalDLC menu video and starts it; its audio is audible for a moment
/// before anything managed can stop it. The method is private and non-virtual, so skipping it for
/// <see cref="DadgInitialScreen"/> is the only way to keep the video from starting.
/// </summary>
public class MainMenuSkipVideoPatch : IPatch
{
    public MethodInfo? TargetMethod => AccessTools.Method(typeof(MBInitialScreenBase), "RefreshScene");

    public MethodInfo? PatchMethod => AccessTools.Method(typeof(MainMenuSkipVideoPatch), nameof(SkipVideo));

    public PatchType PatchType => PatchType.Prefix;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static bool SkipVideo(MBInitialScreenBase __instance)
    {
        if (__instance is not DadgInitialScreen screen)
            return true;

        screen.EnsureInertVideoView();
        return false;
    }
}

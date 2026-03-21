using System.Reflection;
using DellarteDellaGuerra.Infrastructure.Utils;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using psai.net;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.Music.Patches;

public class MBMusicManagerInitializePatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(MBMusicManager), "Initialize");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(MBMusicManagerInitializePatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    private static void Prefix()
    {
        if (!NativeConfig.DisableSound)
        {
            var path = ResourceLocator.GetSoundtrackFilePath();
            if (path != null)
                PsaiCore.Instance.LoadSoundtrackFromProjectFile(path);
        }
    }
}

using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Infrastructure.Utils;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using psai.net;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Integration.Music.Patches;

public class MBMusicManagerInitializePatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(MBMusicManager), "ProcessCreation");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(MBMusicManagerInitializePatch), nameof(FilterNativeModules));

    public PatchType PatchType => PatchType.Postfix;

    // After MBMusicManager is created with all modules, reload PSAI with only DaDG soundtracks.
    // The list is ordered by module load order, so everything before DellarteDellaGuerra
    // is native Bannerlord music that we want to exclude.
    // Logik.LoadSoundtrackFromProjectFile resets psaiProject to null on each call,
    // so this second call effectively replaces the native soundtrack state.
    static void FilterNativeModules()
    {
        if (NativeConfig.DisableSound) return;

        var list = XmlResource.MbprojXmls
            .Where(x => x.Id == "soln_soundtrack" && x.ModuleName.StartsWith(ModuleIdHelper.GetModuleIdPrefix()))
            .Select(x => x.ModuleName)
            .ToList();

        PsaiCore.Instance.LoadSoundtrackFromProjectFile(list);
    }
}

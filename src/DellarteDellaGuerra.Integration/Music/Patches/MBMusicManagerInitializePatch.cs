using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Infrastructure.Utils;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using psai.net;

namespace DellarteDellaGuerra.Integration.Music.Patches;

public class MBMusicManagerInitializePatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(PsaiCore), nameof(PsaiCore.LoadSoundtrackFromProjectFile));

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(MBMusicManagerInitializePatch), nameof(FilterNativeModules));

    public PatchType PatchType => PatchType.Prefix;

    // Intercept the soundtrack load and replace the module list with only DaDG soundtracks,
    // stripping out native Bannerlord music before PSAI ever processes the list.
    static void FilterNativeModules(ref List<string> pathToProjectFiles)
    {
        pathToProjectFiles = pathToProjectFiles
            .Where(x => x.StartsWith(ModuleIdHelper.GetModuleIdPrefix()))
            .ToList();
    }
}

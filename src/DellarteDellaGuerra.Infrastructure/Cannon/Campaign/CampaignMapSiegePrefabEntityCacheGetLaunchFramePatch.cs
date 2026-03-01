using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Patches;
using HarmonyLib;
using SandBox;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Campaign;

public class CampaignMapSiegePrefabEntityCacheGetLaunchFramePatch : IPatch
{
    public MethodInfo? TargetMethod => AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCache),
        nameof(CampaignMapSiegePrefabEntityCache.GetLaunchEntitialFrameForSiegeEngine));

    public MethodInfo? PatchMethod => AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCacheGetLaunchFramePatch),
        nameof(GetLaunchEntitialFramePostfix));

    public PatchType PatchType => PatchType.Postfix;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static void GetLaunchEntitialFramePostfix(SiegeEngineType type, BattleSideEnum side, ref MatrixFrame __result)
    {
        if (CampaignMapSiegePrefabEntityCacheState.SiegeLaunchFrames.TryGetValue(type.StringId, out var frame))
            __result = frame;
    }
}

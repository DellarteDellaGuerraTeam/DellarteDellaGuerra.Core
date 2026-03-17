using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Campaign;

public class CampaignMapSiegePrefabEntityCacheGetScalePatch : IPatch
{
    public MethodInfo? TargetMethod => AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCache),
        nameof(CampaignMapSiegePrefabEntityCache.GetScaleForSiegeEngine));

    public MethodInfo? PatchMethod => AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCacheGetScalePatch),
        nameof(GetScaleForSiegeEnginePostfix));

    public PatchType PatchType => PatchType.Postfix;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static void GetScaleForSiegeEnginePostfix(SiegeEngineType type, BattleSideEnum side, ref Vec3 __result)
    {
        if (CampaignMapSiegePrefabEntityCacheState.SiegeProjectileScales.TryGetValue(type.StringId, out var scale))
            __result = scale;
    }
}

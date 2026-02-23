using System.Collections.Generic;
using System.Reflection;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Patches;
using HarmonyLib;
using SandBox;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Campaign;

public class CampaignMapSiegePrefabEntityCachePatches
{
    private static IPrefabSiegeEngineRepository _prefabSiegeEngineRepository;

    public CampaignMapSiegePrefabEntityCachePatches(IPrefabSiegeEngineRepository prefabSiegeEngineRepository)
    {
        _prefabSiegeEngineRepository = prefabSiegeEngineRepository;
    }

    private static readonly Dictionary<string, MatrixFrame> _siegeEngineLaunchEntitialFrames = new();
    private static readonly Dictionary<string, Vec3> _siegeEngineProjectileScales = new();

    public IList<IPatch> GetPatches()
    {
        return new List<IPatch>
        {
            new OnInitPatch(),
            new GetLaunchEntitialFrameForSiegeEnginePatch(),
            new GetScaleForSiegeEnginePatch()
        };
    }

    private class OnInitPatch : IPatch
    {
        public MethodInfo? TargetMethod => AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCache), "OnInit");

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCachePatches), nameof(OnInitPostfix));

        public PatchType PatchType => PatchType.Postfix;
    }

    private static void OnInitPostfix(CampaignMapSiegePrefabEntityCache __instance)
    {
        _siegeEngineLaunchEntitialFrames.Clear();
        _siegeEngineProjectileScales.Clear();

        foreach (var prefabSiegeEngine in _prefabSiegeEngineRepository.GetPrefabSiegeEngines())
        {
            var gameEntity = GameEntity.Instantiate(((MapScene)TaleWorlds.CampaignSystem.Campaign.Current.MapSceneWrapper).Scene,
                prefabSiegeEngine.SiegeEngineMapPrefabName, true);
            var launchFrame = gameEntity.GetChild(0)
                .GetFirstChildEntityWithTag("projectile_position")
                .GetGlobalFrame();
            var launchScale = gameEntity.GetChild(0).GetFrame().rotation.GetScaleVector();

            _siegeEngineLaunchEntitialFrames[prefabSiegeEngine.SiegeEngineId] = launchFrame;
            _siegeEngineProjectileScales[prefabSiegeEngine.SiegeEngineId] = launchScale;
        }
    }

    private class GetLaunchEntitialFrameForSiegeEnginePatch : IPatch
    {
        public MethodInfo? TargetMethod => AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCache),
            nameof(CampaignMapSiegePrefabEntityCache.GetLaunchEntitialFrameForSiegeEngine));

        public MethodInfo? PatchMethod => AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCachePatches),
            nameof(GetLaunchEntitialFramePostfix));

        public PatchType PatchType => PatchType.Postfix;
    }

    private static void GetLaunchEntitialFramePostfix(SiegeEngineType type,
        BattleSideEnum side,
        ref MatrixFrame __result)
    {
        if (_siegeEngineLaunchEntitialFrames.TryGetValue(type.StringId, out var frame)) __result = frame;
    }

    private class GetScaleForSiegeEnginePatch : IPatch
    {
        public MethodInfo? TargetMethod => AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCache),
            nameof(CampaignMapSiegePrefabEntityCache.GetScaleForSiegeEngine));

        public MethodInfo? PatchMethod => AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCachePatches),
            nameof(GetScaleForSiegeEnginePostfix));

        public PatchType PatchType => PatchType.Postfix;
    }

    private static void GetScaleForSiegeEnginePostfix(SiegeEngineType type,
        BattleSideEnum side,
        ref Vec3 __result)
    {
        if (_siegeEngineProjectileScales.TryGetValue(type.StringId, out var scale)) __result = scale;
    }
}
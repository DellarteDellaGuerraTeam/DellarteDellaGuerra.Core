using System.Reflection;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Campaign;

public class CampaignMapSiegePrefabEntityCacheOnInitPatch : IPatch
{
    private static IPrefabSiegeEngineRepository _prefabSiegeEngineRepository;

    public CampaignMapSiegePrefabEntityCacheOnInitPatch(IPrefabSiegeEngineRepository prefabSiegeEngineRepository)
    {
        _prefabSiegeEngineRepository = prefabSiegeEngineRepository;
    }

    public MethodInfo? TargetMethod => AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCache), "OnInit");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCacheOnInitPatch), nameof(OnInitPostfix));

    public PatchType PatchType => PatchType.Postfix;

    private static void OnInitPostfix(CampaignMapSiegePrefabEntityCache __instance)
    {
        CampaignMapSiegePrefabEntityCacheState.SiegeLaunchFrames.Clear();
        CampaignMapSiegePrefabEntityCacheState.SiegeProjectileScales.Clear();

        foreach (var prefabSiegeEngine in _prefabSiegeEngineRepository.GetPrefabSiegeEngines())
        {
            var gameEntity = GameEntity.Instantiate(
                ((MapScene)TaleWorlds.CampaignSystem.Campaign.Current.MapSceneWrapper).Scene,
                prefabSiegeEngine.SiegeEngineMapPrefabName, true);
            var launchFrame = gameEntity.GetChild(0)
                .GetFirstChildEntityWithTag("projectile_position")
                .GetGlobalFrame();
            var launchScale = gameEntity.GetChild(0).GetFrame().rotation.GetScaleVector();

            CampaignMapSiegePrefabEntityCacheState.SiegeLaunchFrames[prefabSiegeEngine.SiegeEngineId] = launchFrame;
            CampaignMapSiegePrefabEntityCacheState.SiegeProjectileScales[prefabSiegeEngine.SiegeEngineId] = launchScale;
        }
    }
}

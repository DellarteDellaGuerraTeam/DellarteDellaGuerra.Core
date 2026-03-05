using System;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Domain.SiegeEngines;
using DellarteDellaGuerra.Domain.Tournament.Reward;
using DellarteDellaGuerra.Domain.Tournament.Reward.Port;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Firearm.Reload;
using DellarteDellaGuerra.Infrastructure.Cannon.Campaign;
using DellarteDellaGuerra.Infrastructure.Cannon.Campaign.UI;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Battle;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.UI;
using DellarteDellaGuerra.Infrastructure.Cannon.Util.UI;
using DellarteDellaGuerra.Infrastructure.CharacterCreation.Patches;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Infrastructure.Events;
using DellarteDellaGuerra.Infrastructure.Firearm;
using DellarteDellaGuerra.Infrastructure.Firearm.Patches;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Poc.Patches;
using DellarteDellaGuerra.Infrastructure.Steam.Patches;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Tournament.Reward.Spi;
using DellarteDellaGuerra.Tournament.Reward.Spi.Mapper;
using Harmony.DependencyInjection;
using Harmony.DependencyInjection.Patches;
using Microsoft.Extensions.DependencyInjection;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using ILoggerFactory = DellarteDellaGuerra.Domain.Common.Logging.Port.ILoggerFactory;
using LoggerFactory = DellarteDellaGuerra.Infrastructure.Logging.LoggerFactory;

namespace DellarteDellaGuerra.Infrastructure.DI;

public class DadgServiceContainer
{
    public IServiceProvider Build()
    {
        var services = new ServiceCollection();
        RegisterCoreServices(services);
        RegisterCannonServices(services);
        RegisterTournamentServices(services);
        RegisterDisplayServices(services);
        RegisterMissionServices(services);
        RegisterPatches(services);
        services.AddLogging();
        services.AddHarmonyPatching();
        return services.BuildServiceProvider();
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddSingleton<ILoggerFactory>(_ => new LoggerFactory(new LoggerConfigPathProvider()));
        services.AddSingleton<DadgConfigWatcher>();
        RegisterEvent<SubModuleLoadEvent>(services);
        services.AddSingleton<CampaignBehaviourDisabler>();
        services.AddSingleton<FirearmSkillProvider>();
        services.AddSingleton<IMBObjectProvider<SkillObject>>(sp => sp.GetRequiredService<FirearmSkillProvider>());
    }

    private static void RegisterEvent<TEvent>(IServiceCollection services)
    {
        services.AddSingleton<EventBus<TEvent>>();
        services.AddSingleton<IEventPublisher<TEvent>>(sp => sp.GetRequiredService<EventBus<TEvent>>());
        services.AddSingleton<IEventSubscriber<TEvent>>(sp => sp.GetRequiredService<EventBus<TEvent>>());
    }

    private static void RegisterCannonServices(IServiceCollection services)
    {
        services.AddSingleton<IDeploymentSiegeEngineIconRepository, DeploymentSiegeEngineIconRepository>();
        services.AddSingleton<IMapSiegeEngineIconRepository, MapSiegeEngineIconRepository>();
        services.AddSingleton<IPrefabSiegeEngineRepository, PrefabSiegeEngineRepository>();
        services.AddSingleton<BrushStyleExtender>(sp => new BrushStyleExtender(
            sp.GetRequiredService<ILoggerFactory>(),
            UIResourceManager.BrushFactory,
            UIResourceManager.SpriteData));
        services.AddSingleton<SiegeEngineDeploymentIconEnricher>();
        services.AddSingleton<CampaignMapSiegeEngineDeploymentIconEnricher>();
        services.AddSingleton<SiegeEngineIconRegistrationUseCase>();
        services.AddSingleton<GetDefaultSiegeEngine>();
    }

    private static void RegisterTournamentServices(IServiceCollection services)
    {
        services.AddSingleton<IItemTierMapper, ItemTierMapper>();
        services.AddSingleton<IItemRepository, ItemRepository>();
        services.AddSingleton<ITroopRepository, TroopRepository>();
        services.AddSingleton<ITownRepository, TownRepository>();
        services.AddSingleton<IRandomProvider, RandomProvider>();
        services.AddSingleton<IHighestTownProsperityProvider, HighestTownProsperityProvider>();
        services.AddSingleton<IGetTournamentRewardUseCase, GetTournamentRewardUseCase>();
        services.AddTransient<DadgTournamentModel>();
    }

    private static void RegisterDisplayServices(IServiceCollection services)
    {
        services.AddSingleton<ICompilingShaderDisplayer, CompilingShaderDisplayer>();
        services.AddSingleton<ICompilingShaderNumberProvider, CompilingShaderNumberProvider>();
        services.AddSingleton<ICompilingShaderNotifierConfig>(sp =>
            new CompilingShaderNotifierConfig(sp.GetRequiredService<DadgConfigWatcher>()));
        services.AddSingleton<DisplayShaderNumber>();
    }

    private static void RegisterMissionServices(IServiceCollection services)
    {
        services.AddTransient<IWeaponEntityRepository, InMemoryWeaponEntityRepository>();
        services.AddTransient<FirearmReloadMissionLogic>();
        services.AddTransient<FirearmSmokeMissionLogic>();
        services.AddTransient<CannonTeamMissionLogic>();
    }

    private static void RegisterPatches(IServiceCollection services)
    {
        // General
        services.AddSingleton<IPatch, GeneralPatches>();
        // Character creation
        services.AddSingleton<IPatch, MainMenuOptionPatches>();
        services.AddSingleton<IPatch, DisableSortingBehaviourInCultureMenuPatch>();
        // Firearm
        services.AddSingleton<IPatch, AddFirearmSkillAsRelevantSkillPatch>();
        services.AddSingleton<IPatch, GetHolsterImageForBuIletsInInventoryPatch>();
        // Cannon mission
        services.AddSingleton<IPatch, MissionSiegeWeaponsControllerPatch>();
        services.AddSingleton<IPatch, OrderSiegeMachineVM_GetSiegeTypePatch>();
        // Steam
        services.AddSingleton<IPatch, FixSettlementFilePathPatch>();
        services.AddSingleton<IPatch, FixSettlementDistanceCacheFilePathPatch>();
        // POC
        services.AddSingleton<IPatch, PocConfigReaderOverriderPatch>();
        // Cannon UI
        services.AddSingleton<IPatch, OrderSiegeMachineItemButtonWidgetPatch>();
        services.AddSingleton<IPatch>(sp => new MapSiegePOIBrushWidgetManualPatch(
            sp.GetRequiredService<IMapSiegeEngineIconRepository>(),
            UIResourceManager.SpriteData));
        services.AddSingleton<IPatch, MapSiegePOIVMPatch>();
        // Campaign siege prefab cache
        services.AddSingleton<IPatch, CampaignMapSiegePrefabEntityCacheOnInitPatch>();
        services.AddSingleton<IPatch, CampaignMapSiegePrefabEntityCacheGetLaunchFramePatch>();
        services.AddSingleton<IPatch, CampaignMapSiegePrefabEntityCacheGetScalePatch>();
    }
}
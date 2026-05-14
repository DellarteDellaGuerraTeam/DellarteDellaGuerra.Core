using System;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.CharacterCreation.Ports;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Domain.SiegeEngines;
using DellarteDellaGuerra.Domain.Tournament.Reward;
using DellarteDellaGuerra.Domain.Tournament.Reward.Port;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Firearm.Reload;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;
using DellarteDellaGuerra.Infrastructure.CharacterCreation.Patches;
using DellarteDellaGuerra.Infrastructure.CharacterCreation.Providers;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Infrastructure.Events;
using DellarteDellaGuerra.Infrastructure.Firearm;
using DellarteDellaGuerra.Infrastructure.Firearm.Patches;
using DellarteDellaGuerra.Infrastructure.Logging;
using DellarteDellaGuerra.Infrastructure.MbObjects;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Poc.Patches;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using DellarteDellaGuerra.Infrastructure.Steam.Patches;
using DellarteDellaGuerra.Integration.Music.Patches;
using DellarteDellaGuerra.Integration.SiegeEngines.Campaign;
using DellarteDellaGuerra.Integration.SiegeEngines.Campaign.UI;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Battle;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Reposition;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.UI;
using DellarteDellaGuerra.Integration.SiegeEngines.Util;
using DellarteDellaGuerra.Integration.SiegeEngines.Util.UI;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Tournament.Reward.Spi;
using DellarteDellaGuerra.Tournament.Reward.Spi.Mapper;
using Harmony.DependencyInjection;
using Harmony.DependencyInjection.Patches;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;

namespace DellarteDellaGuerra.Integration.DI;

public class DadgServiceContainer
{
    public IServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => { b.AddNLog(new LoggerConfigPathProvider().Config); });
        RegisterCoreServices(services);
        RegisterCannonServices(services);
        RegisterTournamentServices(services);
        RegisterDisplayServices(services);
        RegisterMissionServices(services);
        RegisterPatches(services);
        services.AddHarmonyPatching();
        return services.BuildServiceProvider();
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddSingleton<ILoggerFactory>(sp =>
            new LoggerFactory(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()));
        services.AddSingleton<DadgConfigWatcher>();
        services.AddSingleton<IAdvancedBannerBuilderConfig>(sp =>
            new AdvancedBannerBuilderConfig(sp.GetRequiredService<DadgConfigWatcher>()));
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
        services.AddSingleton<ValidateCannonsUseCase>();
        services.AddSingleton<ICannonRegistry>(sp =>
        {
            var registry = new CannonRegistry();
            var configuration = new XmlCannonConfigurationReader(sp.GetRequiredService<ILoggerFactory>());
            var validateCannons = sp.GetRequiredService<ValidateCannonsUseCase>();
            foreach (var cannon in validateCannons.GetValidCannons(configuration.LoadCannons()))
            {
                var dynamicType = CannonTypeEmitter.EmitCannonType(cannon.Id);
                registry.RegisterCannon(cannon, new GenericCannonFactory(cannon.Id, dynamicType));
            }
            return registry;
        });
        services.AddSingleton<ICannonIconProvider, CannonIconProvider>();
        services.AddSingleton<CannonPrefabProvider>();
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
        services.AddTransient<CannonPushMissionBehavior>();
    }

    private static void RegisterPatches(IServiceCollection services)
    {
        // Music
        services.AddSingleton<IPatch, MBMusicManagerInitializePatch>();
        // General
        services.AddSingleton<IPatch, GeneralPatches>();
        // Character creation
        services.AddSingleton<IPatch, MainMenuOptionPatches>();
        services.AddSingleton<IPatch, DisableSortingBehaviourInCultureMenuPatch>();
        services.AddSingleton<IPatch, RedirectBannerEditorStateToBannerBuilderPatch>();
        services.AddSingleton<IPatch, PersistBannerBuilderResultPatch>();
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

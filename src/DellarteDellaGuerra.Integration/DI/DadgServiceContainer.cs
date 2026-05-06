using System;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Domain.Tournament.Reward;
using DellarteDellaGuerra.Domain.Tournament.Reward.Port;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Firearm.Reload;
using DellarteDellaGuerra.Heraldry;
using DellarteDellaGuerra.Infrastructure.CharacterCreation.Patches;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DI;
using DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Infrastructure.Events;
using DellarteDellaGuerra.Infrastructure.Firearm;
using DellarteDellaGuerra.Infrastructure.Firearm.Patches;
using DellarteDellaGuerra.Infrastructure.Logging;
using DellarteDellaGuerra.Infrastructure.MbObjects;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Poc.Patches;
using DellarteDellaGuerra.Infrastructure.Steam.Patches;
using DellarteDellaGuerra.Integration.Initialisation;
using DellarteDellaGuerra.Integration.Music.Patches;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Patches;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Port;
using DellarteDellaGuerra.Infrastructure.Tournament.Jousting;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Tournament.Reward.Spi;
using DellarteDellaGuerra.Tournament.Reward.Spi.Mapper;
using Harmony.DependencyInjection;
using Harmony.DependencyInjection.Patches;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Integration.DI;

public class DadgServiceContainer
{
    public IServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => { b.AddNLog(new LoggerConfigPathProvider().Config); });
        RegisterCoreServices(services);
        services.AddDadgInfrastructure();
        RegisterTournamentServices(services);
        RegisterDisplayServices(services);
        RegisterMissionServices(services);
        RegisterPatches(services);
        services.AddHarmonyPatching();
        var provider = services.BuildServiceProvider();
        return provider.InitializeDadgInfrastructure();
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddSingleton<ILoggerFactory>(sp =>
            new LoggerFactory(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()));
        services.AddSingleton<DadgConfigWatcher>();
        RegisterEvent<SubModuleLoadEvent>(services);
        services.AddSingleton<DadgScriptComponentRegistrar>();
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

    private static void RegisterTournamentServices(IServiceCollection services)
    {
        services.AddSingleton<IItemTierMapper, ItemTierMapper>();
        services.AddSingleton<IItemRepository, ItemRepository>();
        services.AddSingleton<ITroopRepository, TroopRepository>();
        services.AddSingleton<ITownRepository, TownRepository>();
        services.AddSingleton<IRandomProvider, RandomProvider>();
        services.AddSingleton<IHighestTownProsperityProvider, HighestTownProsperityProvider>();
        services.AddSingleton<IGetTournamentRewardUseCase, GetTournamentRewardUseCase>();
        services.AddSingleton<IJoustRequirementsProvider>(sp =>
            new JoustRequirementsConfig(sp.GetRequiredService<DadgConfigWatcher>()));
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
        services.AddTransient<RemoveSiegeTowerSpawnersMissionLogic>();
        services.AddTransient<BannerSurcoatMissionLogic>();
        services.AddTransient<TournamentRecapBannerMissionLogic>();
        services.AddTransient<LiveWallBreachingMissionLogic>();
    }

    private static void RegisterPatches(IServiceCollection services)
    {
        // Music
        services.AddSingleton<IPatch, MBMusicManagerInitializePatch>();
        services.AddSingleton<IPatch, CampaignMusicHandlerTickPatch>();
        // General
        services.AddSingleton<IPatch, GeneralPatches>();
        // Character creation
        services.AddSingleton<IPatch, MainMenuOptionPatches>();
        services.AddSingleton<IPatch, DisableSortingBehaviourInCultureMenuPatch>();
        // Firearm
        services.AddSingleton<IPatch, AddFirearmSkillAsRelevantSkillPatch>();
        services.AddSingleton<IPatch, GetHolsterImageForBuIletsInInventoryPatch>();
        // Steam
        services.AddSingleton<IPatch, FixSettlementFilePathPatch>();
        services.AddSingleton<IPatch, FixSettlementDistanceCacheFilePathPatch>();
        // POC
        services.AddSingleton<IPatch, PocConfigReaderOverriderPatch>();
        // Siege engines
        services.AddSingleton<IPatch, CannonballTrailCleanupPatch>();
    }
}

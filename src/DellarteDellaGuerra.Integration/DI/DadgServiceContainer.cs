using System;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.MainMenu;
using DellarteDellaGuerra.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Domain.Levy;
using DellarteDellaGuerra.Domain.Levy.Port;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Levy;
using DellarteDellaGuerra.Levy.Api;
using DellarteDellaGuerra.Domain.Tournament.Reward;
using DellarteDellaGuerra.Domain.Tournament.Reward.Port;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Firearm.Reload;
using DellarteDellaGuerra.Infrastructure.CharacterCreation.Patches;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DI;
using DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Infrastructure.Events;
using DellarteDellaGuerra.Infrastructure.Firearm;
using DellarteDellaGuerra.Infrastructure.Firearm.Patches;
using DellarteDellaGuerra.Infrastructure.Logging;
using DellarteDellaGuerra.Infrastructure.MbObjects;
using DellarteDellaGuerra.Infrastructure.Poc.Patches;
using DellarteDellaGuerra.Infrastructure.Steam.Patches;
using DellarteDellaGuerra.Infrastructure.Titles;
using DellarteDellaGuerra.Integration.Music.Patches;
using DellarteDellaGuerra.Integration.Music.Patches;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Patches;
using DellarteDellaGuerra.Integration.Titles;
using DellarteDellaGuerra.Titles.Api.Campaign;
using DellarteDellaGuerra.Titles.Api.GameModels;
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
        RegisterTitleServices(services);
        RegisterTournamentServices(services);
        RegisterDisplayServices(services);
        RegisterMissionServices(services);
        RegisterLevyServices(services);
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
        services.AddSingleton<CampaignBehaviourDisabler>();
        services.AddSingleton<DadgCampaignStartButtonAdder>();
        services.AddSingleton<VanillaCampaignButtonsRemover>();
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
    }

    private static void RegisterTitleServices(IServiceCollection services)
    {
        // Infrastructure: persistent registries and de jure structure
        services.AddSingleton<FeudalStructureConfigReader>();
        services.AddSingleton(sp => sp.GetRequiredService<FeudalStructureConfigReader>().CreateStructure());
        services.AddSingleton<IFeudalStructure>(sp => sp.GetRequiredService<XmlFeudalStructure>());
        services.AddSingleton<InMemoryTitleRegistry>();
        services.AddSingleton<ITitleRepository>(sp => sp.GetRequiredService<InMemoryTitleRegistry>());
        services.AddSingleton<InMemoryClaimRegistry>();
        services.AddSingleton<IClaimRepository>(sp => sp.GetRequiredService<InMemoryClaimRegistry>());
        services.AddSingleton<InMemoryTensionRegistry>();
        services.AddSingleton<ITensionRepository>(sp => sp.GetRequiredService<InMemoryTensionRegistry>());
        services.AddSingleton<IFeudalStateStore, FeudalStateStoreAdapter>();

        // Domain use cases
        services.AddSingleton<IAssignTitleUseCase, AssignTitleUseCase>();
        services.AddSingleton<IGetSuzerainUseCase, GetSuzerainUseCase>();
        services.AddSingleton<IGetDirectVassalsUseCase, GetDirectVassalsUseCase>();
        services.AddSingleton<IEvaluateClaimUseCase, EvaluateClaimUseCase>();
        services.AddSingleton<IComputeFeudalSupportUseCase, ComputeFeudalSupportUseCase>();
        services.AddSingleton<IComputeInfluenceTierBonusUseCase, ComputeInfluenceTierBonusUseCase>();
        services.AddSingleton<IAccumulateTensionUseCase, AccumulateTensionUseCase>();
        services.AddSingleton<IGenerateInheritanceClaimsUseCase, GenerateInheritanceClaimsUseCase>();

        // Campaign behaviours and game models (resolved lazily in SubModule)
        services.AddSingleton<FeudalTitleCampaignBehavior>(sp => new FeudalTitleCampaignBehavior(
            sp.GetRequiredService<IAssignTitleUseCase>(),
            sp.GetRequiredService<IGenerateInheritanceClaimsUseCase>(),
            sp.GetRequiredService<IFeudalStateStore>(),
            () => sp.GetRequiredService<XmlFeudalStructure>().BuildInitialTitles()));
        services.AddSingleton<InternalConflictCampaignBehavior>();
        services.AddSingleton<FeudalTitleSwapBehavior>();
        services.AddTransient<DadgDiplomacyModel>();
        services.AddTransient<DadgClanPoliticsModel>();
        services.AddTransient<DadgSettlementLoyaltyModel>();
    }

    private static void RegisterLevyServices(IServiceCollection services)
    {
        services.AddSingleton<InMemoryLevyRegistry>();
        services.AddSingleton<ILevyRepository>(sp => sp.GetRequiredService<InMemoryLevyRegistry>());
        services.AddSingleton<IIssueLevyUseCase, IssueLevyUseCase>();
        services.AddSingleton<IExpireLeviesUseCase, ExpireLeviesUseCase>();
        services.AddSingleton<LevyCampaignBehavior>();
    }

    private static void RegisterPatches(IServiceCollection services)
    {
        // Character creation
        services.AddSingleton<IPatch, DisableSortingBehaviourInCultureMenuPatch>();
        // Firearm
        services.AddSingleton<IPatch, AddFirearmSkillAsRelevantSkillPatch>();
        services.AddSingleton<IPatch, GetHolsterImageForBuIletsInInventoryPatch>();
        // Steam
        services.AddSingleton<IPatch, FixSettlementFilePathPatch>();
        services.AddSingleton<IPatch, FixSettlementDistanceCacheFilePathPatch>();
        // Music
        services.AddSingleton<IPatch, MBMusicManagerInitializePatch>();
        // POC
        services.AddSingleton<IPatch, PocConfigReaderOverriderPatch>();
        // Siege engines
        services.AddSingleton<IPatch, CannonballTrailCleanupPatch>();
    }
}

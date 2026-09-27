using System;
using Bannerlord.PrivateWars.Api;
using Bannerlord.PrivateWars.Domain;
using DellarteDellaGuerra.Integration.PrivateWars;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.MainMenu;
using DellarteDellaGuerra.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Domain.Tournament.Reward;
using DellarteDellaGuerra.Domain.Tournament.Reward.Port;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Firearm.Reload;
using DellarteDellaGuerra.Heraldry;
using DellarteDellaGuerra.Infrastructure.CharacterCreation.Patches;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DI;
using DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Infrastructure.Events;
using DellarteDellaGuerra.Infrastructure.Firearm;
using DellarteDellaGuerra.Infrastructure.Firearm.Patches;
using DellarteDellaGuerra.Infrastructure.Heraldry.Patches;
using DellarteDellaGuerra.Infrastructure.Logging;
using DellarteDellaGuerra.Infrastructure.MbObjects;
using DellarteDellaGuerra.Infrastructure.Poc.Patches;
using DellarteDellaGuerra.Infrastructure.Steam.Patches;
using DellarteDellaGuerra.Integration.CampaignTime;
using DellarteDellaGuerra.Infrastructure.Titles;
using DellarteDellaGuerra.Integration.Initialisation;
using DellarteDellaGuerra.Integration.Music.Patches;
using DellarteDellaGuerra.Integration.Music.Patches;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Patches;
using DellarteDellaGuerra.Integration.Titles;
using DellarteDellaGuerra.Titles.Api.Campaign;
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
        // The logger's config path is looked up in the module folders, so reading it needs the
        // game running. Nothing else in the composition does, which is what lets it be tested.
        var provider = Compose(new LoggerConfigPathProvider().Config).BuildServiceProvider();

        return provider.InitializeDadgInfrastructure();
    }

    /**
     * <summary>
     *  Every registration DADG makes, and nothing else: no service is built here and nothing
     *  reads the running game.
     * </summary>
     */
    public IServiceCollection Compose(string? loggerConfigPath)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => { b.AddNLog(loggerConfigPath); });
        RegisterCoreServices(services);
        services.AddDadgInfrastructure();
        RegisterTitleServices(services);
        RegisterTournamentServices(services);
        RegisterDisplayServices(services);
        RegisterMissionServices(services);
        RegisterPatches(services);
        services.AddHarmonyPatching();

        return services;
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddSingleton<ILoggerFactory>(sp =>
            new LoggerFactory(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()));
        services.AddSingleton<DadgConfigWatcher>();
        services.AddSingleton<IConfigurationProvider<DadgConfig>>(sp => sp.GetRequiredService<DadgConfigWatcher>());
        RegisterEvent<SubModuleLoadEvent>(services);
        services.AddSingleton<DadgScriptComponentRegistrar>();
        services.AddSingleton<CampaignBehaviourDisabler>();
        services.AddSingleton<DadgCampaignTimeModel>();
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
        services.AddSingleton<IFeudalStateStore, FeudalStateStoreAdapter>();
        services.AddSingleton<IGenealogy, CampaignGenealogy>();
        services.AddSingleton<ICadetBranch, CadetBranchAdapter>();
        services.AddSingleton<IPrivateWarDeclaration>(sp => new PrivateWarDeclarationAdapter(
            sp.GetRequiredService<IPrivateWarsApi>(),
            new MainGoalSelector()));

        // Domain use cases
        services.AddSingleton<IAssignTitleUseCase, AssignTitleUseCase>();
        services.AddSingleton<SuzeraintyPolicy>();
        services.AddSingleton<IGetSuzerainUseCase, GetSuzerainUseCase>();
        services.AddSingleton<IGetDirectVassalsUseCase, GetDirectVassalsUseCase>();
        services.AddSingleton<IEvaluateClaimUseCase, EvaluateClaimUseCase>();
        services.AddSingleton<IGenerateBloodClaimsUseCase, GenerateBloodClaimsUseCase>();
        services.AddSingleton<IExecuteSuccessionUseCase, ExecuteSuccessionUseCase>();
        services.AddSingleton<IBuildFeudalMapUseCase, BuildFeudalMapUseCase>();
        services.AddSingleton<IGetDeJureSettlementsUseCase, GetDeJureSettlementsUseCase>();
        services.AddSingleton<IAwardWonClaimUseCase, AwardWonClaimUseCase>();
        services.AddSingleton<IEvaluatePressClaimUseCase, EvaluatePressClaimUseCase>();
        services.AddSingleton<ISolicitSupportUseCase, SolicitSupportUseCase>();
        services.AddSingleton<IPersonalBondPolicy, PersonalBondPolicy>();

        // Campaign behaviour (resolved lazily in SubModule)
        services.AddSingleton<FeudalTitleCampaignBehavior>(sp => new FeudalTitleCampaignBehavior(
            sp.GetRequiredService<IAssignTitleUseCase>(),
            sp.GetRequiredService<IGenerateBloodClaimsUseCase>(),
            sp.GetRequiredService<IExecuteSuccessionUseCase>(),
            sp.GetRequiredService<IFeudalStateStore>(),
            () => sp.GetRequiredService<XmlFeudalStructure>()
                    .BuildInitialTitles(sp.GetRequiredService<IGenealogy>())));
        services.AddSingleton<ClaimPressureCampaignBehavior>();
    }


    private static void RegisterPatches(IServiceCollection services)
    {
        // Music
        services.AddSingleton<IPatch, MBMusicManagerInitializePatch>();
        services.AddSingleton<IPatch, CampaignMusicHandlerTickPatch>();
        // Character creation
        services.AddSingleton<IPatch, DisableSortingBehaviourInCultureMenuPatch>();
        // Heraldry
        services.AddSingleton<IPatch, AllowSingleplayerExtendedBannerCodeParsingPatch>();
        services.AddSingleton<IPatch, AllowSingleplayerExtendedBannerAppendLayerPatch>();
        services.AddSingleton<IPatch, AllowSingleplayerExtendedBannerInsertLayerPatch>();
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

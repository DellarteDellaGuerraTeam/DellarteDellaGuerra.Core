using System;
using Bannerlord.PrivateWars.Api;
using Bannerlord.PrivateWars.Domain;
using DellarteDellaGuerra.Integration.Naval;
using DellarteDellaGuerra.Integration.PrivateWars;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.MainMenu;
using DellarteDellaGuerra.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Port;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Domain.Tournament.Reward;
using DellarteDellaGuerra.Domain.Tournament.Reward.Port;
using DellarteDellaGuerra.Domain.Visor;
using DellarteDellaGuerra.Domain.Visor.Port;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Firearm.Reload;
using DellarteDellaGuerra.Heraldry;
using DellarteDellaGuerra.Integration.Visor.Mission;
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
using DellarteDellaGuerra.Infrastructure.MainMenu.Patches;
using DellarteDellaGuerra.Infrastructure.Steam.Patches;
using DellarteDellaGuerra.Infrastructure.Visor.Spi;
using DellarteDellaGuerra.Integration.CampaignTime;
using DellarteDellaGuerra.Infrastructure.PoliticalMap;
using DellarteDellaGuerra.Infrastructure.Titles;
using DellarteDellaGuerra.Integration.Initialisation;
using DellarteDellaGuerra.Integration.Music.Patches;
using DellarteDellaGuerra.Integration.Music.Patches;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Patches;
using DellarteDellaGuerra.Integration.SiegeTactics.Mission;
using DellarteDellaGuerra.Integration.Titles;
using DellarteDellaGuerra.Integration.PoliticalMap;
using DellarteDellaGuerra.PoliticalMap.Api.Campaign;
using DellarteDellaGuerra.Titles.Api.Campaign;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Port;
using DellarteDellaGuerra.Infrastructure.Tournament.Jousting;
using DellarteDellaGuerra.Integration.Tournament.Jousting;
using DellarteDellaGuerra.Integration.Tournament.Jousting.UI;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Tournament.Jousting.Api.Missions;
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
        RegisterPoliticalMapServices(services);
        RegisterTournamentServices(services);
        RegisterDisplayServices(services);
        RegisterMissionServices(services);
        services.AddDadgCombat();
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
        services.AddSingleton<DadgUIExtenderRegistrar>();
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
        services.AddSingleton<IJoustingFeatureToggle>(sp =>
            new JoustingFeatureToggleConfig(sp.GetRequiredService<DadgConfigWatcher>()));
        services.AddSingleton<JoustingMissionManager>();
        services.AddTransient<DadgTournamentModel>();
        services.AddSingleton<JoustTournamentNameplateBrushRegistrar>();
        services.AddSingleton<JoustingFeatureInstaller>();
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
        services.AddTransient<SiegeAssaultDiagnosticsMissionLogic>();
        services.AddTransient<CapacityWeightedAssaultMissionLogic>();
        services.AddTransient<BannerSurcoatMissionLogic>();
        services.AddTransient<TournamentRecapBannerMissionLogic>();
        services.AddSingleton<IVisorVariantRepository, VisorVariantXmlRepository>();
        services.AddSingleton<VisorVariantCatalog>();
        services.AddTransient<VisorToggleMissionLogic>();
        services.AddTransient<ShipSwivelGunCrewMissionLogic>();
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
        services.AddSingleton<IGetDirectVassalsUseCase, GetDirectVassalsUseCase>();
        services.AddSingleton<IEvaluateClaimUseCase, EvaluateClaimUseCase>();
        services.AddSingleton<IGenerateBloodClaimsUseCase, GenerateBloodClaimsUseCase>();
        services.AddSingleton<IExecuteSuccessionUseCase, ExecuteSuccessionUseCase>();
        services.AddSingleton<IBuildFeudalMapUseCase, BuildFeudalMapUseCase>();
        services.AddSingleton<IGetDeJureSettlementsUseCase, GetDeJureSettlementsUseCase>();
        services.AddSingleton<IAwardWonClaimUseCase, AwardWonClaimUseCase>();
        services.AddSingleton<IScreenClaimUseCase, ScreenClaimUseCase>();
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
                    .BuildInitialTitles(sp.GetRequiredService<IGenealogy>()),
            () => sp.GetRequiredService<XmlFeudalStructure>().BuildInitialClaims()));
        services.AddSingleton<ClaimPressureCampaignBehavior>();
    }


    private static void RegisterPoliticalMapServices(IServiceCollection services)
    {
        services.AddSingleton<PoliticalMapTerritories>();
        services.AddSingleton(_ => new PoliticalMapRefreshScheduler());
        services.AddSingleton<PoliticalMapPaintLedger>();
        // The settlement grid is built from the shipped map terrain at session launch, off the main thread.
        services.AddSingleton<IPoliticalMapTerrain, MainMapTerrain>();
        services.AddSingleton<PoliticalMapGridCampaignBehavior>();
        // Every title change is saved through the registry, so the behaviour hears them all there.
        services.AddSingleton(sp =>
        {
            var behavior = ActivatorUtilities.CreateInstance<PoliticalMapRefreshCampaignBehavior>(sp);
            sp.GetRequiredService<InMemoryTitleRegistry>().HolderOrOccupantChanged += behavior.OnTitleChanged;
            return behavior;
        });
        services.AddSingleton<PoliticalMapPainter>();
        services.AddSingleton<PoliticalMapHoverDescriber>();
        // Draws the overlay; ticked every frame from SubModule.OnApplicationTick.
        services.AddSingleton<PoliticalMapOverlayCampaignBehavior>();
        // Names the regions over the overlay; ticked right after it.
        services.AddSingleton<PoliticalMapLabelsCampaignBehavior>();
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
        services.AddSingleton<IPatch, KeepBannerTabardMeshUntilTextureAppliedPatch>();
        services.AddSingleton<IPatch, ReleaseBannerTabardMeshAfterTextureAppliedPatch>();
        // Firearm
        services.AddSingleton<IPatch, AddFirearmSkillAsRelevantSkillPatch>();
        services.AddSingleton<IPatch, GetHolsterImageForBuIletsInInventoryPatch>();
        // Steam
        services.AddSingleton<IPatch, FixSettlementFilePathPatch>();
        // Main menu
        services.AddSingleton<IPatch, MainMenuSkipVideoPatch>();
        services.AddSingleton<IPatch, MainMenuHoldLoadingWindowPatch>();
            // Music
        services.AddSingleton<IPatch, MBMusicManagerInitializePatch>();
        // Siege engines
        services.AddSingleton<IPatch, CannonballTrailCleanupPatch>();
        // Political map: native overlays give way to region labels
        services.AddSingleton<IPatch, HideSettlementNameplatesPatch>();
        services.AddSingleton<IPatch, SelectPoliticalMapRegionPatch>();
        services.AddSingleton<IPatch, HidePartyNameplatesPatch>();
        services.AddSingleton<IPatch, HidePlayerPartyLabelsPatch>();
        services.AddSingleton<IPatch, HidePlayerMapTrackerPatch>();
        services.AddSingleton<IPatch, HideBookmarkedMapTrackersPatch>();
    }
}

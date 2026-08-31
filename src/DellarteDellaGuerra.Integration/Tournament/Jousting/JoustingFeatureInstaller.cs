using System.Linq;
using Bannerlord.UIExtenderEx;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Port;
using DellarteDellaGuerra.Integration.Tournament.Jousting.UI;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Tournament.Jousting.Api.Campaign;
using DellarteDellaGuerra.Tournament.Jousting.Api.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace DellarteDellaGuerra.Integration.Tournament.Jousting;

/// <summary>
/// Installs the jousting feature, or leaves it out entirely when it is turned off in the
/// configuration.
///
/// The toggle is read once, on construction, so every phase agrees on it. Turning jousting on
/// or off therefore takes effect on the next game start.
/// </summary>
public class JoustingFeatureInstaller
{
    private const string UiExtenderModuleName = "DellarteDellaGuerra.Core";

    private readonly IJoustRequirementsProvider _requirementsProvider;
    private readonly JoustingMissionManager _missionManager;
    private readonly JoustTournamentNameplateBrushRegistrar _brushRegistrar;
    private readonly ILogger _logger;
    private readonly bool _joustingEnabled;

    public JoustingFeatureInstaller(IJoustingFeatureToggle featureToggle,
        IJoustRequirementsProvider requirementsProvider,
        JoustingMissionManager missionManager,
        JoustTournamentNameplateBrushRegistrar brushRegistrar,
        ILoggerFactory loggerFactory)
    {
        _requirementsProvider = requirementsProvider;
        _missionManager = missionManager;
        _brushRegistrar = brushRegistrar;
        _logger = loggerFactory.CreateLogger<JoustingFeatureInstaller>();
        _joustingEnabled = featureToggle.IsJoustingEnabled;
    }

    public void InstallOnSubModuleLoad()
    {
        JoustingMissionManagerProvider.Init(_missionManager);

        if (!_joustingEnabled)
        {
            _logger.Info("Jousting is disabled in the configuration. Its models, dialogs and nameplate icon will not be installed.");
            return;
        }

        var uiExtender = UIExtender.Create(UiExtenderModuleName);
        uiExtender.Register(typeof(JoustingFeatureInstaller).Assembly);
        uiExtender.Enable();
    }

    public void RegisterNameplateBrush()
    {
        if (!_joustingEnabled) return;
        _brushRegistrar.Register();
    }

    public void InstallCampaign(CampaignGameStarter campaignGameStarter)
    {
        if (!_joustingEnabled) return;

        campaignGameStarter.AddModel(new DadgSettlementAccessModel(
            campaignGameStarter.Models.OfType<SettlementAccessModel>().Last(),
            _requirementsProvider));
        campaignGameStarter.AddBehavior(new JoustTournamentCampaignBehavior(_requirementsProvider));
    }
}

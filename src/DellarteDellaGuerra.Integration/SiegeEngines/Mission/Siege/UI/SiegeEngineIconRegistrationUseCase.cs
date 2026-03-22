using DellarteDellaGuerra.Integration.SiegeEngines.Campaign.UI;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;
using DellarteDellaGuerra.Infrastructure.Events;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.UI;

public class SiegeEngineIconRegistrationUseCase
{
    private readonly SiegeEngineDeploymentIconEnricher _siegeEngineDeploymentIconEnricher;
    private readonly IDeploymentSiegeEngineIconRepository _iconRepository;
    private readonly CampaignMapSiegeEngineDeploymentIconEnricher _campaignMapSiegeEngineDeploymentIconEnricher;

    public SiegeEngineIconRegistrationUseCase(
        IEventSubscriber<SubModuleLoadEvent> subModuleEventSubscriber,
        SiegeEngineDeploymentIconEnricher siegeEngineDeploymentIconEnricher,
        CampaignMapSiegeEngineDeploymentIconEnricher campaignMapSiegeEngineDeploymentIconEnricher,
        IDeploymentSiegeEngineIconRepository iconRepository)
    {
        _siegeEngineDeploymentIconEnricher = siegeEngineDeploymentIconEnricher;
        _campaignMapSiegeEngineDeploymentIconEnricher = campaignMapSiegeEngineDeploymentIconEnricher;
        _iconRepository = iconRepository;
        subModuleEventSubscriber.Subscribe(OnSubModuleLoaded);
    }

    private void OnSubModuleLoaded(SubModuleLoadEvent _)
    {
        foreach (var icon in _iconRepository.SiegeEngineIcons)
        {
            _siegeEngineDeploymentIconEnricher.AddSiegeEngineDeploymentIcon(icon.Name, icon.SiegeDeploymentSelectionIconSpriteId);
            _campaignMapSiegeEngineDeploymentIconEnricher.AddCampaignMapSiegeEngineDeploymentIcon(icon.Name,
                icon.CampaignMapSelectionIconSpriteId);
        }
    }
}

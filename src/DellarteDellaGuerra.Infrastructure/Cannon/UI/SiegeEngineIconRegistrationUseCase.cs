using DellarteDellaGuerra.Infrastructure.Events;

namespace DellarteDellaGuerra.Cannon.UI;

public class SiegeEngineIconRegistrationUseCase
{
    private readonly IOnSubModuleLoadEventSubscriber _subModuleEventSubscriber;
    private readonly SiegeEngineDeploymentIconEnricher _siegeEngineDeploymentIconEnricher;
    private readonly IDeploymentSiegeEngineIconRepository _iconRepository;
    private readonly CampaignMapSiegeEngineDeploymentIconEnricher _campaignMapSiegeEngineDeploymentIconEnricher;

    public SiegeEngineIconRegistrationUseCase(
        IOnSubModuleLoadEventSubscriber subModuleEventSubscriber,
        SiegeEngineDeploymentIconEnricher siegeEngineDeploymentIconEnricher,
        CampaignMapSiegeEngineDeploymentIconEnricher campaignMapSiegeEngineDeploymentIconEnricher,
        IDeploymentSiegeEngineIconRepository iconRepository)
    {
        _subModuleEventSubscriber = subModuleEventSubscriber;
        _siegeEngineDeploymentIconEnricher = siegeEngineDeploymentIconEnricher;
        _campaignMapSiegeEngineDeploymentIconEnricher = campaignMapSiegeEngineDeploymentIconEnricher;
        _iconRepository = iconRepository;
    }

    public void RegisterSiegeEngineIcons()
    {
        _subModuleEventSubscriber.Subscribe(OnSubModuleLoaded);
    }

    private void OnSubModuleLoaded()
    {
        foreach (var icon in _iconRepository.SiegeEngineIcons)
        {
            _siegeEngineDeploymentIconEnricher.AddSiegeEngineDeploymentIcon(icon.Name, icon.SpriteId);
            _campaignMapSiegeEngineDeploymentIconEnricher.AddCampaignMapSiegeEngineDeploymentIcon(icon.Name,
                icon.SpriteId);
        }
    }
}
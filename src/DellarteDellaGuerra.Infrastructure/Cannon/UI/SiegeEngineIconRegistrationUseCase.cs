using DellarteDellaGuerra.Infrastructure.Events;

namespace DellarteDellaGuerra.Cannon.UI;

public class SiegeEngineIconRegistrationUseCase
{
    private readonly IOnSubModuleLoadEventSubscriber _subModuleEventSubscriber;
    private readonly SiegeIconBrushExtender _brushExtender;
    private readonly IDeploymentSiegeEngineIconRepository _iconRepository;

    public SiegeEngineIconRegistrationUseCase(
        IOnSubModuleLoadEventSubscriber subModuleEventSubscriber,
        SiegeIconBrushExtender brushExtender,
        IDeploymentSiegeEngineIconRepository iconRepository)
    {
        _subModuleEventSubscriber = subModuleEventSubscriber;
        _brushExtender = brushExtender;
        _iconRepository = iconRepository;
    }

    public void RegisterSiegeEngineIcons()
    {
        _subModuleEventSubscriber.Subscribe(OnSubModuleLoaded);
    }

    private void OnSubModuleLoaded()
    {
        foreach (var icon in _iconRepository.SiegeEngineIcons)
            _brushExtender.AddSiegeEngineDeploymentIcon(icon.Name, icon.SpriteId);
    }
}
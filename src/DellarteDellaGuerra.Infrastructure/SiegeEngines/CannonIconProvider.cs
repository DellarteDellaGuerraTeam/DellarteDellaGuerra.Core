using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class CannonIconProvider : ICannonIconProvider
{
    private readonly ICannonRegistry _cannonRegistry;

    public CannonIconProvider(ICannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public IEnumerable<DeploymentSiegeEngineIcon> GetSiegeEngineIcons() =>
        _cannonRegistry.GetAllCannonTypes()
            .Select(c => new DeploymentSiegeEngineIcon(
                c.DisplayName,
                c.SiegeDeploymentSelectionIconSpriteId,
                c.CampaignMapSelectionIconSpriteId,
                c.MachineType));
}

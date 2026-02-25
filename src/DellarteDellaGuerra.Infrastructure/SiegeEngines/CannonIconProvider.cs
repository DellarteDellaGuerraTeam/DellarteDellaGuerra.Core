using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.SiegeEngines;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Model;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class CannonIconProvider
{
    private readonly CannonRegistry _cannonRegistry;

    public CannonIconProvider(CannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public IEnumerable<DeploymentSiegeEngineIcon> GetSiegeEngineIcons()
    {
        return _cannonRegistry.GetAllCannonTypes()
            .Select(c => new DeploymentSiegeEngineIcon(c.DisplayName, c.SpriteId, c.MachineType));
    }

    public string GetSpriteId(string cannonId)
    {
        var cannonType = _cannonRegistry.GetCannonType(cannonId);
        return cannonType?.SpriteId;
    }
}
using DellarteDellaGuerra.Infrastructure.Cannon.Util.UI;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.UI;

public class SiegeEngineDeploymentIconEnricher
{
    private const string SiegeEngineDeploymentIconBrushName = "Order.Siege.Deployment.MachineIcon";
    private const string SiegeEngineDeploymentIconSpritePrefix = "Order\\SiegeIcons";

    private readonly BrushStyleExtender _brushStyleExtender;

    public SiegeEngineDeploymentIconEnricher(BrushStyleExtender brushStyleExtender)
    {
        _brushStyleExtender = brushStyleExtender;
    }


    public void AddSiegeEngineDeploymentIcon(string siegeEngineName, string siegeEngineId)
    {
        _brushStyleExtender.AddBrushStyle(siegeEngineName, siegeEngineId,
            SiegeEngineDeploymentIconBrushName, SiegeEngineDeploymentIconSpritePrefix);
    }
}
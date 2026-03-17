using DellarteDellaGuerra.Integration.SiegeEngines.Util.UI;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.UI;

public class SiegeEngineDeploymentIconEnricher
{
    private const string SiegeEngineDeploymentIconBrushName = "Order.Siege.Deployment.MachineIcon";

    private readonly BrushStyleExtender _brushStyleExtender;

    public SiegeEngineDeploymentIconEnricher(BrushStyleExtender brushStyleExtender)
    {
        _brushStyleExtender = brushStyleExtender;
    }

    public void AddSiegeEngineDeploymentIcon(string siegeEngineName, string siegeOrderIconSpritePath)
    {
        _brushStyleExtender.AddBrushStyle(siegeEngineName, siegeOrderIconSpritePath,
            SiegeEngineDeploymentIconBrushName);
    }
}
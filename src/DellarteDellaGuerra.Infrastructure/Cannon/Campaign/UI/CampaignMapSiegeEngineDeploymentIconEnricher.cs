using DellarteDellaGuerra.Infrastructure.Cannon.Util.UI;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Campaign.UI;

public class CampaignMapSiegeEngineDeploymentIconEnricher
{
    private const string SiegeEngineDeploymentIconBrushName = "CustomBattle.Siege.MachineIcon";
    private const string SiegeEngineDeploymentIconSpritePrefix = "SPGeneral\\Siege";

    private readonly BrushStyleExtender _brushStyleExtender;

    public CampaignMapSiegeEngineDeploymentIconEnricher(BrushStyleExtender brushStyleExtender)
    {
        _brushStyleExtender = brushStyleExtender;
    }

    public void AddCampaignMapSiegeEngineDeploymentIcon(string siegeEngineName, string siegeEngineId)
    {
        _brushStyleExtender.AddBrushStyle(siegeEngineName, siegeEngineId,
            SiegeEngineDeploymentIconBrushName, SiegeEngineDeploymentIconSpritePrefix);
    }
}
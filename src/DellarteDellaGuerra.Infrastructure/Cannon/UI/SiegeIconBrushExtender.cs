using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.GauntletUI;
using TaleWorlds.TwoDimension;

namespace DellarteDellaGuerra.Cannon.UI;

public class SiegeIconBrushExtender
{
    private const string SiegeEngineDeploymentIconsBrushName = "Order.Siege.Deployment.MachineIcon";
    private const string BrushLayerName = "Default";

    private readonly Brush _siegeEngineDeploymentIconsBrush;
    private readonly SpriteData _spriteData;
    private readonly ILogger _logger;

    public SiegeIconBrushExtender(ILoggerFactory loggerFactory, BrushFactory brushFactory, SpriteData spriteData)
    {
        _logger = loggerFactory.CreateLogger<SiegeIconBrushExtender>();
        _siegeEngineDeploymentIconsBrush = brushFactory.GetBrush(SiegeEngineDeploymentIconsBrushName);
        _spriteData = spriteData;

        if (_siegeEngineDeploymentIconsBrush is null)
            _logger.Error($"Could not find any Brush with name {SiegeEngineDeploymentIconsBrushName}");
    }

    public void AddSiegeEngineDeploymentIcon(string siegeEngineName, string siegeEngineSpriteId)
    {
        string spriteName = GetCompleteSiegeIconSpriteName(siegeEngineSpriteId);
        Sprite sprite = _spriteData.GetSprite(spriteName);

        if (sprite is null)
            _logger.Error(
                $"Could not find any Sprite with name {spriteName}. Siege engine icon will not show up during siege deployment.");

        _siegeEngineDeploymentIconsBrush.AddStyle(new Style(new List<BrushLayer>
        {
            new() { Name = BrushLayerName, Sprite = sprite }
        })
        {
            Name = siegeEngineName,
            DefaultStyle = _siegeEngineDeploymentIconsBrush.DefaultStyle
        });
    }

    private static string GetCompleteSiegeIconSpriteName(string spriteId)
    {
        return $"Order\\SiegeIcons\\{spriteId}";
    }
}
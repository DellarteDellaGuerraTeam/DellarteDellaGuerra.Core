using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.GauntletUI;
using TaleWorlds.TwoDimension;

namespace DellarteDellaGuerra.Cannon.UI;

public class BrushStyleExtender
{
    private const string BrushLayerName = "Default";

    private readonly SpriteData _spriteData;
    private readonly ILogger _logger;
    private readonly BrushFactory _brushFactory;

    public BrushStyleExtender(ILoggerFactory loggerFactory, BrushFactory brushFactory, SpriteData spriteData)
    {
        _logger = loggerFactory.CreateLogger<SiegeEngineDeploymentIconEnricher>();
        _brushFactory = brushFactory;
        _spriteData = spriteData;
    }

    public void AddBrushStyle(string siegeEngineName, string siegeEngineId, string brushName, string spriteNamePrefix)
    {
        Brush brush = _brushFactory.GetBrush(brushName);


        if (brush is null)
        {
            _logger.Error($"Could not find any Brush with name {brushName}");
            return;
        }

        string spriteName = GetCompleteSpriteName(spriteNamePrefix, siegeEngineId);
        Sprite sprite = _spriteData.GetSprite(spriteName);

        if (sprite is null)
        {
            _logger.Error($"Could not find any Sprite with name {spriteName}. Icon will not show up.");
            return;
        }

        brush.AddStyle(new Style(new List<BrushLayer>
        {
            new() { Name = BrushLayerName, Sprite = sprite }
        })
        {
            Name = siegeEngineName,
            DefaultStyle = brush.DefaultStyle
        });
    }

    private string GetCompleteSpriteName(string spriteNamePrefix, string spriteId)
    {
        return $"{spriteNamePrefix}\\{spriteId}";
    }
}
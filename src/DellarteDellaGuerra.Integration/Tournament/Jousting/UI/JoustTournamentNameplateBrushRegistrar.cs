using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.Tournament.Jousting.UI;

/// <summary>
/// Injects the "JoustTournament" style into the shared "Settlement.Event.Type.Image" brush.
/// Once registered, a nameplate event widget calling SetState("JoustTournament") renders the
/// horse icon instead of the vanilla tournament helmet — no brush XML override needed.
/// </summary>
public class JoustTournamentNameplateBrushRegistrar
{
    /// <summary>Name of the brush style used for jousting tournament nameplates.</summary>
    internal const string JoustTournamentState = "JoustTournament";

    private const string BrushName = "Settlement.Event.Type.Image";
    private const string HorseSpriteName = @"StdAssets\ItemIcons\war_horse";

    private readonly ILogger _logger;

    public JoustTournamentNameplateBrushRegistrar(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<JoustTournamentNameplateBrushRegistrar>();
    }

    public void Register()
    {
        var brush = UIResourceManager.BrushFactory?.GetBrush(BrushName);
        if (brush is null)
        {
            _logger.Error($"Could not find any Brush with name {BrushName}. Jousting icon will not show up.");
            return;
        }

        if (brush.GetStyle(JoustTournamentState) != null) return;

        var horseSprite = UIResourceManager.SpriteData?.GetSprite(HorseSpriteName);
        if (horseSprite is null)
        {
            _logger.Error($"Could not find any Sprite with name {HorseSpriteName}. Jousting icon will not show up.");
            return;
        }

        // Build a new style by cloning the brush's default layer set, then override the sprite.
        // Style(brush.Layers) copies each BrushLayer into a StyleLayer — modifying those
        // StyleLayers only affects this style's rendering, not the brush's default state.
        var joustStyle = new Style(brush.Layers)
        {
            Name = JoustTournamentState,
            DefaultStyle = brush.DefaultStyle
        };

        foreach (var layer in joustStyle.GetLayers())
        {
            layer.Sprite = horseSprite;
            layer.Color = new Color(0f, 0f, 0f, 1f); // black, fully opaque
        }

        brush.AddStyle(joustStyle);
    }
}

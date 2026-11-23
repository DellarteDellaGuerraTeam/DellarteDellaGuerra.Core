using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;

namespace DellarteDellaGuerra.Integration.Naval;

internal static class ShipThumbnailBrushes
{
    internal static void Register()
    {
        var brush = UIResourceManager.BrushFactory?.GetBrush("Naval.ShipThumbnails");
        if (brush is null) return;

        AddAlias(brush, "dadg_ws_carrack_ship_nested", "roundship_nested");
        AddAlias(brush, "dadg_ws_kogge_ship_nested", "cog_ship_nested");
        AddAlias(brush, "dadg_ws_english_boat_b_ship_nested", "fishing_ship_nested");
    }

    private static void AddAlias(Brush brush, string name, string nativeLayer)
    {
        var sprite = brush.GetLayer(nativeLayer)?.Sprite;
        if (sprite is null || brush.GetLayer(name) is not null) return;

        brush.AddLayer(new BrushLayer { Name = name, Sprite = sprite });
    }
}

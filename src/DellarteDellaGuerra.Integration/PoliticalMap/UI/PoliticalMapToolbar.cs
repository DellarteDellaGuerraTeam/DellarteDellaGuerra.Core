using System;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.PoliticalMap.UI;

internal static class PoliticalMapToolbar
{
    internal const string ItemId = "dadg_political_map";
    internal const string SpriteName = @"DADG\PoliticalMap\map";

    internal static void RegisterIcon(ILogger logger)
    {
        Brush? icons = UIResourceManager.BrushFactory?.GetBrush("MapBar.Left.Icons");
        Brush? backgrounds = UIResourceManager.BrushFactory?.GetBrush("MapBar.Left.Button.Backgrounds");
        BrushLayer? background = backgrounds?.GetLayer("clan");
        if (icons is null || backgrounds is null || background is null)
        {
            logger.Error("Political map toolbar brushes are unavailable.");
            return;
        }
        if (icons.GetLayer(ItemId) is not null) return;

        var sprite = UIResourceManager.SpriteData?.GetSprite(SpriteName);
        if (sprite is null)
        {
            logger.Error($"Political map toolbar sprite '{SpriteName}' is unavailable. Import its atlas with the Modding Tools.");
            return;
        }

        var frame = new BrushLayer();
        frame.FillFrom(background);
        frame.Name = ItemId;
        backgrounds.AddLayer(frame);
        icons.AddLayer(new BrushLayer
        {
            Name = ItemId,
            Sprite = sprite,
            ImageFitType = ImageFit.ImageFitTypes.Contain,
            ImageFitHorizontalAlignment = ImageFit.ImageHorizontalAlignments.Center,
            ImageFitVerticalAlignment = ImageFit.ImageVerticalAlignments.Center,
            SaturationFactor = 0f,
            ValueFactor = 0.65f,
            ExtendTop = -2,
            ExtendBottom = -2
        });
    }
}

internal sealed class PoliticalMapNavigationElement(Action open, Func<bool> isActive) : INavigationElement
{
    public string StringId => PoliticalMapToolbar.ItemId;
    public NavigationPermissionItem Permission => new(true, new TextObject(string.Empty));
    public bool IsLockingNavigation => false;
    public bool IsActive => isActive();
    public TextObject Tooltip => new("{=dadg_polmap_button_shortcut}Political Map (M)");
    public bool HasAlert => false;
    public TextObject AlertTooltip => new(string.Empty);
    public void OpenView() => open();
    public void OpenView(params object[] parameters) => OpenView();
    public void GoToLink() => OpenView();
}

using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Patches;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Siege;
using TaleWorlds.TwoDimension;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Campaign.UI;

public class MapSiegePOIBrushWidgetManualPatch : IPatch
{
    private static IMapSiegeEngineIconRepository _iconRepository;
    private static SpriteData _spriteData;

    public MapSiegePOIBrushWidgetManualPatch(
        IMapSiegeEngineIconRepository iconRepository,
        SpriteData spriteData
    )
    {
        _spriteData = spriteData;
        _iconRepository = iconRepository;
    }

    public MethodInfo? TargetMethod => ResolveOriginalMethod();
    public MethodInfo? PatchMethod => ResolvePatchMethod();
    public PatchType PatchType => PatchType.Postfix;

    public static void SetSprite(MapSiegePOIBrushWidget __instance, int machineType)
    {
        var cannonIcon = _iconRepository.MapSiegeEngineIcons.FirstOrDefault(icon => icon.MachineType == machineType);

        if (cannonIcon is null) return;

        __instance.MachineTypeIconWidget.Sprite =
            _spriteData.GetSprite($"SPGeneral\\MapSiege\\{cannonIcon.SpriteId}");
    }

    private MethodInfo? ResolvePatchMethod()
    {
        return typeof(MapSiegePOIBrushWidgetManualPatch).GetMethod(
            "SetSprite",
            BindingFlags.Public | BindingFlags.Static
        );
    }

    private MethodInfo? ResolveOriginalMethod()
    {
        return typeof(MapSiegePOIBrushWidget).GetMethod(
            "SetMachineTypeIcon",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
    }
}
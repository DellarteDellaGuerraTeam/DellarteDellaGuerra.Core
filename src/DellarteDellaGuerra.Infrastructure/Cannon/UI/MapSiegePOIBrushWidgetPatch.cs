using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Patches;
using HarmonyLib;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Siege;
using TaleWorlds.TwoDimension;

namespace DellarteDellaGuerra.Cannon.UI;

public class MapSiegePOIBrushWidgetManualPatch : IPatch
{
    private static IMapSiegeEngineIconRepository _iconRepository;
    private static SpriteData _spriteData;
    private readonly Harmony _harmony;
    private readonly ILogger _logger;

    public MapSiegePOIBrushWidgetManualPatch(
        ILoggerFactory loggerFactory,
        Harmony harmony,
        IMapSiegeEngineIconRepository iconRepository,
        SpriteData spriteData
    )
    {
        _spriteData = spriteData;
        _logger = loggerFactory.CreateLogger<MapSiegePOIBrushWidgetManualPatch>();
        _harmony = harmony;
        _iconRepository = iconRepository;
    }

    public void Patch()
    {
        MethodInfo? originalMethod = ResolveOriginalMethod();
        MethodInfo? patchMethod = ResolvePatchMethod();
        if (originalMethod == null || patchMethod == null)
        {
            _logger.Warn(
                $"{nameof(MapSiegePOIBrushWidgetManualPatch)} failed to resolve the original method or the patch method");
            return;
        }

        _harmony.Patch(originalMethod, postfix: new HarmonyMethod(patchMethod));
    }

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

    public MethodInfo? TargetMethod => ResolveOriginalMethod();
    public MethodInfo? PatchMethod => ResolvePatchMethod();
    public PatchType PatchType => PatchType.Postfix;
}
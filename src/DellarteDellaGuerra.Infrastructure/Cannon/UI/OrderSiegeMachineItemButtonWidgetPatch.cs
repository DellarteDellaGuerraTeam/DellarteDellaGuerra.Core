using System;
using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Patches;
using HarmonyLib;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

namespace DellarteDellaGuerra.Cannon.UI;

public class OrderSiegeMachineItemButtonWidgetManualPatch : IPatch
{
    private static IDeploymentSiegeEngineIconRepository _iconRepository;
    private readonly Harmony _harmony;
    private readonly ILogger _logger;

    public OrderSiegeMachineItemButtonWidgetManualPatch(ILoggerFactory loggerFactory, Harmony harmony,
        IDeploymentSiegeEngineIconRepository iconRepository)
    {
        _logger = loggerFactory.CreateLogger<OrderSiegeMachineItemButtonWidgetManualPatch>();
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
                $"{nameof(OrderSiegeMachineItemButtonWidgetManualPatch)} failed to resolve the original method or the patch method");
            return;
        }

        _harmony.Patch(originalMethod, postfix: new HarmonyMethod(patchMethod));
    }

    private static void TryPatchWidget(OrderSiegeMachineItemButtonWidget __instance)
    {
        var machineClass =
            AccessTools.Property(typeof(OrderSiegeMachineItemButtonWidget), "MachineClass")
                .GetValue(__instance) as string;
        var machineIconWidget =
            AccessTools.Property(typeof(OrderSiegeMachineItemButtonWidget), "MachineIconWidget")
                .GetValue(__instance) as Widget;

        if (machineClass != null && machineIconWidget != null)
        {
            var icon = _iconRepository.SiegeEngineIcons
                .FirstOrDefault(i => i.Name.Equals(machineClass, StringComparison.InvariantCultureIgnoreCase));
            if (icon != null)
            {
                var isRemainingCountVisibleField =
                    AccessTools.Field(typeof(OrderSiegeMachineItemButtonWidget), "_isRemainingCountVisible");
                isRemainingCountVisibleField.SetValue(__instance, true);
                machineIconWidget.SetState(icon.Name);
            }
        }
    }

    private MethodInfo? ResolvePatchMethod()
    {
        return typeof(OrderSiegeMachineItemButtonWidgetManualPatch).GetMethod("SetSprite", AccessTools.all);
    }

    private MethodInfo? ResolveOriginalMethod()
    {
        return typeof(OrderSiegeMachineItemButtonWidget).GetMethod("UpdateMachineIcon", AccessTools.all);
    }
}
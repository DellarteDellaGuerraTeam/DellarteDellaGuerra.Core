using System;
using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Patches;
using HarmonyLib;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.UI;

public class OrderSiegeMachineItemButtonWidgetPatch : IPatch
{
    private static IDeploymentSiegeEngineIconRepository _iconRepository;

    public OrderSiegeMachineItemButtonWidgetPatch(IDeploymentSiegeEngineIconRepository iconRepository)
    {
        _iconRepository = iconRepository;
    }

    public MethodInfo? TargetMethod => ResolveOriginalMethod();
    public MethodInfo? PatchMethod => ResolvePatchMethod();
    public PatchType PatchType => PatchType.Postfix;

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
        return typeof(OrderSiegeMachineItemButtonWidgetPatch).GetMethod("TryPatchWidget", AccessTools.all);
    }

    private MethodInfo? ResolveOriginalMethod()
    {
        return typeof(OrderSiegeMachineItemButtonWidget).GetMethod("UpdateMachineIcon", AccessTools.all);
    }
}
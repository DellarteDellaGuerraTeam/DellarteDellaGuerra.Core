using System;
using System.Reflection;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.UI;

public class OrderSiegeMachineVM_GetSiegeTypePatch : IPatch
{
    private static ICannonRegistry _cannonRegistry;

    public OrderSiegeMachineVM_GetSiegeTypePatch(ICannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(OrderSiegeMachineVM), nameof(OrderSiegeMachineVM.GetSiegeType));

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(OrderSiegeMachineVM_GetSiegeTypePatch), nameof(Postfix));

    public PatchType PatchType => PatchType.Postfix;

    private static void Postfix(Type t, BattleSideEnum side, ref SiegeEngineType __result)
    {
        var cannonType = _cannonRegistry?.GetCannonTypeByScriptType(t);
        if (cannonType != null)
            __result = MBObjectManager.Instance.GetObject<SiegeEngineType>(cannonType.Id);
    }
}

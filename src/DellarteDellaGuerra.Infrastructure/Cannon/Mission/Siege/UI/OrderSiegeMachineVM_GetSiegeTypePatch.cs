using System;
using System.Reflection;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.UI;

public class OrderSiegeMachineVM_GetSiegeTypePatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(OrderSiegeMachineVM), nameof(OrderSiegeMachineVM.GetSiegeType));

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(OrderSiegeMachineVM_GetSiegeTypePatch), nameof(Postfix));

    public PatchType PatchType => PatchType.Postfix;

    private static void Postfix(Type t, BattleSideEnum side, ref SiegeEngineType __result)
    {
        if (t == typeof(Falconet))
            __result = MBObjectManager.Instance.GetObject<SiegeEngineType>("falconet");
    }
}
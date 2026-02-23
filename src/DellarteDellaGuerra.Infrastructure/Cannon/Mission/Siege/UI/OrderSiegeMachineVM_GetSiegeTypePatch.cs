using System;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.UI
{
    [HarmonyPatch(typeof(OrderSiegeMachineVM))]
    public static class OrderSiegeMachineVM_GetSiegeTypePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(OrderSiegeMachineVM.GetSiegeType))]
        public static void Postfix(Type t, BattleSideEnum side, ref SiegeEngineType __result)
        {
            if (t == typeof(Falconet))
                __result = MBObjectManager.Instance.GetObject<SiegeEngineType>("falconet");
        }
    }
}
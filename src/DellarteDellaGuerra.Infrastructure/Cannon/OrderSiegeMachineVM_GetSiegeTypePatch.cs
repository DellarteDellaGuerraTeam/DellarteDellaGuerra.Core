using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Cannon
{
    [HarmonyPatch(typeof(OrderSiegeMachineVM))]
    public static class OrderSiegeMachineVM_GetSiegeTypePatch
    {
        // Target the static method
        [HarmonyPostfix]
        [HarmonyPatch(nameof(OrderSiegeMachineVM.GetSiegeType))]
        public static void Postfix(Type t, BattleSideEnum side, ref SiegeEngineType __result)
        {
            // Check for your custom type
            if (t == typeof(Falconet))
                // Set your own custom SiegeEngineType here
                __result = MBObjectManager.Instance.GetObject<SiegeEngineType>("falconet");
        }
    }
}
using System;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.UI
{
    [HarmonyPatch(typeof(OrderSiegeMachineVM))]
    public static class OrderSiegeMachineVM_GetSiegeTypePatch
    {
        private static ICannonRegistry _cannonRegistry;

        public static void SetRegistry(ICannonRegistry cannonRegistry)
        {
            _cannonRegistry = cannonRegistry;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(OrderSiegeMachineVM.GetSiegeType))]
        public static void Postfix(Type t, BattleSideEnum side, ref SiegeEngineType __result)
        {
            var cannonType = _cannonRegistry?.GetCannonTypeByScriptType(t);
            if (cannonType != null)
                __result = MBObjectManager.Instance.GetObject<SiegeEngineType>(cannonType.Id);
        }
    }
}

using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Missions;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn
{
    [HarmonyPatch(typeof(MissionSiegeWeaponsController), "GetSiegeWeaponBaseType")]
    public static class MissionSiegeWeaponsControllerPatch
    {
        private static void Postfix(SiegeEngineType siegeWeaponType, ref Type __result)
        {
            if (siegeWeaponType == MBObjectManager.Instance.GetObject<SiegeEngineType>("falconet"))
                __result = typeof(Falconet);
        }
    }
}
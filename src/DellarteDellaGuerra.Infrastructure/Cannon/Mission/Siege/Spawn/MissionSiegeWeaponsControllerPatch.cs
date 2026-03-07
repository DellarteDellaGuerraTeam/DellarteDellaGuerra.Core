using System;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Missions;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn;

public class MissionSiegeWeaponsControllerPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(MissionSiegeWeaponsController), "GetSiegeWeaponBaseType");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(MissionSiegeWeaponsControllerPatch), nameof(Postfix));

    public PatchType PatchType => PatchType.Postfix;

    private static void Postfix(SiegeEngineType siegeWeaponType, ref Type __result)
    {
        if (siegeWeaponType == MBObjectManager.Instance.GetObject<SiegeEngineType>("falconet"))
            __result = typeof(Falconet);
    }
}
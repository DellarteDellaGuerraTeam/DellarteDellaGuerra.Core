using System;
using System.Reflection;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Missions;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;

public class MissionSiegeWeaponsControllerPatch : IPatch
{
    private static ICannonRegistry _cannonRegistry;

    public MissionSiegeWeaponsControllerPatch(ICannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(MissionSiegeWeaponsController), "GetSiegeWeaponBaseType");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(MissionSiegeWeaponsControllerPatch), nameof(Postfix));

    public PatchType PatchType => PatchType.Postfix;

    private static void Postfix(SiegeEngineType siegeWeaponType, ref Type __result)
    {
        var factory = _cannonRegistry?.GetFactory(siegeWeaponType.StringId);
        if (factory != null)
            __result = factory.CannonScriptType;
    }
}

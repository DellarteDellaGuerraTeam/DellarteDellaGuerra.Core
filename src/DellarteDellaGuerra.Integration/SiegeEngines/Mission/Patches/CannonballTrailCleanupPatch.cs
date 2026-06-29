using System.Linq;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Patches;

public class CannonballTrailCleanupPatch : IPatch
{
    private const string CannonballTrailParticleName = "psys_cannonball_trail";

    public MethodInfo? TargetMethod =>
        AccessTools.Method(
            typeof(TaleWorlds.MountAndBlade.Mission),
            nameof(TaleWorlds.MountAndBlade.Mission.SpawnWeaponAsDropFromMissile));

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(CannonballTrailCleanupPatch), nameof(Postfix));

    public PatchType PatchType => PatchType.Postfix;

    private static void Postfix(TaleWorlds.MountAndBlade.Mission __instance, int missileIndex)
    {
        var missile = __instance.MissilesList.FirstOrDefault(m => m.Index == missileIndex);
        if (missile?.Weapon.CurrentUsageItem?.TrailParticleName != CannonballTrailParticleName)
            return;

        missile.Entity.SetVisibilityExcludeParents(false);
    }
}

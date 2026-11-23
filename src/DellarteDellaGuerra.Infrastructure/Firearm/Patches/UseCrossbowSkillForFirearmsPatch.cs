using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.Firearm.Patches;

public class UseCrossbowSkillForFirearmsPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(WeaponComponentData), "GetRelevantSkillFromWeaponClass");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(UseCrossbowSkillForFirearmsPatch), nameof(UseCrossbowSkill));

    public PatchType PatchType => PatchType.Prefix;

    private static bool UseCrossbowSkill(ref SkillObject __result, WeaponClass weaponClass)
    {
        if (weaponClass == WeaponClass.Cartridge || weaponClass == WeaponClass.Musket ||
            weaponClass == WeaponClass.Pistol)
        {
            __result = DefaultSkills.Crossbow;
            return false;
        }

        return true;
    }
}

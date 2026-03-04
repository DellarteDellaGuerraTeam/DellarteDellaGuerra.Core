using System.Reflection;
using DellarteDellaGuerra.Firearm;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.Firearm.Patches;

public class AddFirearmSkillAsRelevantSkillPatch : IPatch
{
    private static FirearmSkill? _firearmSkill;

    public AddFirearmSkillAsRelevantSkillPatch(FirearmSkill firearmSkill)
    {
        _firearmSkill = firearmSkill;
    }

    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(WeaponComponentData), "GetRelevantSkillFromWeaponClass");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(AddFirearmSkillAsRelevantSkillPatch), nameof(AddGunpowderRelevantSkill));

    public PatchType PatchType => PatchType.Prefix;

    private static bool AddGunpowderRelevantSkill(ref SkillObject __result, WeaponClass weaponClass)
    {
        if (_firearmSkill?.GetNativeFirearmSkill() is not null && (weaponClass == WeaponClass.Cartridge ||
                                                                   weaponClass == WeaponClass.Musket ||
                                                                   weaponClass == WeaponClass.Pistol))
        {
            __result = _firearmSkill.GetNativeFirearmSkill()!;
            return false;
        }

        return true;
    }
}

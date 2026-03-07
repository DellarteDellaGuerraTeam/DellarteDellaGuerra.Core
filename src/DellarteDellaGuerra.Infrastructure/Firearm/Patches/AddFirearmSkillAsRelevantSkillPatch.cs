using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.Firearm.Patches;

public class AddFirearmSkillAsRelevantSkillPatch : IPatch
{
    private static FirearmSkillProvider? _firearmSkillProvider;

    public AddFirearmSkillAsRelevantSkillPatch(FirearmSkillProvider firearmSkillProvider)
    {
        _firearmSkillProvider = firearmSkillProvider;
    }

    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(WeaponComponentData), "GetRelevantSkillFromWeaponClass");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(AddFirearmSkillAsRelevantSkillPatch), nameof(AddGunpowderRelevantSkill));

    public PatchType PatchType => PatchType.Prefix;

    private static bool AddGunpowderRelevantSkill(ref SkillObject __result, WeaponClass weaponClass)
    {
        if (_firearmSkillProvider?.GetMbObject() is not null && (weaponClass == WeaponClass.Cartridge ||
                                                                 weaponClass == WeaponClass.Musket ||
                                                                 weaponClass == WeaponClass.Pistol))
        {
            __result = _firearmSkillProvider.GetMbObject();
            return false;
        }

        return true;
    }
}

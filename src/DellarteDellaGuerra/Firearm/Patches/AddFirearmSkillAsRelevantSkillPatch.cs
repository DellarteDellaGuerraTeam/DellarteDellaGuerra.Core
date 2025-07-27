using HarmonyLib;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Firearm.Patches
{
    [HarmonyPatch]
    public static class AddFirearmSkillAsRelevantSkillPatch
    {
        private static FirearmSkill? _firearmSkill;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WeaponComponentData), "GetRelevantSkillFromWeaponClass")]
        public static bool AddGunpowderRelevantSkill(ref SkillObject __result, WeaponClass weaponClass)
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

        public static void SetFirearmSkill(FirearmSkill firearmSkill)
        {
            _firearmSkill = firearmSkill;
        }
    }
}
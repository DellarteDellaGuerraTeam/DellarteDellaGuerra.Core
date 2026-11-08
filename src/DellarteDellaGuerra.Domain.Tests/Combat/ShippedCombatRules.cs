using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Combat;

namespace DellarteDellaGuerra.Domain.Tests.Combat;

/// <summary>
/// A copy of the rules in DellarteDellaGuerra's ModuleData/CustomXml/dadg_combat_items.xml, so the balance
/// tests check the shipped numbers. Update both together.
/// </summary>
internal static class ShippedCombatRules
{
    public static readonly ArmourAbsorption Absorption = new(new Dictionary<(ArmourMaterial, BlowType, CombatWeaponClass), float>
    {
        [(ArmourMaterial.Cloth, BlowType.Cut, CombatWeaponClass.Default)] = 0f,
        [(ArmourMaterial.Cloth, BlowType.Pierce, CombatWeaponClass.Default)] = 0.03f,
        [(ArmourMaterial.Cloth, BlowType.Blunt, CombatWeaponClass.Default)] = 0.03f,
        [(ArmourMaterial.Padded, BlowType.Cut, CombatWeaponClass.Default)] = 0.05f,
        [(ArmourMaterial.Padded, BlowType.Pierce, CombatWeaponClass.Default)] = 0.02f,
        [(ArmourMaterial.Padded, BlowType.Blunt, CombatWeaponClass.Default)] = 0.1f,
        [(ArmourMaterial.Mail, BlowType.Cut, CombatWeaponClass.Default)] = 0.15f,
        [(ArmourMaterial.Mail, BlowType.Pierce, CombatWeaponClass.Default)] = 0f,
        [(ArmourMaterial.Mail, BlowType.Blunt, CombatWeaponClass.Default)] = 0.11f,
        [(ArmourMaterial.Brigandine, BlowType.Cut, CombatWeaponClass.Default)] = 0.25f,
        [(ArmourMaterial.Brigandine, BlowType.Pierce, CombatWeaponClass.Default)] = 0.03f,
        [(ArmourMaterial.Brigandine, BlowType.Blunt, CombatWeaponClass.Default)] = 0.1f,
        [(ArmourMaterial.Plate, BlowType.Cut, CombatWeaponClass.Default)] = 0.4f,
        [(ArmourMaterial.Plate, BlowType.Pierce, CombatWeaponClass.Default)] = 0.18f,
        [(ArmourMaterial.Plate, BlowType.Blunt, CombatWeaponClass.Default)] = 0.16f,
        [(ArmourMaterial.Plate, BlowType.Cut, CombatWeaponClass.OneHandedSword)] = 0.88f,
        [(ArmourMaterial.Plate, BlowType.Cut, CombatWeaponClass.TwoHandedSword)] = 0.94f,
        [(ArmourMaterial.Plate, BlowType.Cut, CombatWeaponClass.TwoHandedPolearm)] = 0.52f
    });

    public static readonly HalfSword HalfSword = new(0.7f);

    public static readonly NonTipThrust NonTipThrust = new(new Dictionary<string, float>
    {
        ["09_ddg_lance"] = 0.45f,
        ["10_Pike"] = 0.3f
    });
}

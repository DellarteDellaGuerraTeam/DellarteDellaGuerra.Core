using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Combat;

namespace DellarteDellaGuerra.Domain.Tests.Combat;

public class ArmourAbsorptionTests
{
    [Fact]
    public void BetterMaterialsAbsorbMoreOfACut()
    {
        var previous = -1f;
        foreach (ArmourMaterial material in System.Enum.GetValues(typeof(ArmourMaterial)))
        {
            var absorbed = ShippedCombatRules.Absorption.Absorbed(material, BlowType.Cut);
            Assert.True(absorbed > previous);
            previous = absorbed;
        }
    }

    [Fact]
    public void AgainstPlate_AMaceBeatsABetterSword()
    {
        Assert.True(BluntOnPlate(54f) > 2f * CutOnPlate(82f));
    }

    [Fact]
    public void AgainstPlate_APollaxeStillBites()
    {
        Assert.True(CutOnPlate(130f, CombatWeaponClass.TwoHandedPolearm) > 2f * CutOnPlate(82f));
        Assert.True(CutOnPlate(130f, CombatWeaponClass.TwoHandedPolearm) >
                    2f * CutOnPlate(122f, CombatWeaponClass.TwoHandedSword));
    }

    [Fact]
    public void AWeaponOverride_ReplacesTheDefaultFraction()
    {
        Assert.Equal(0.88f, ShippedCombatRules.Absorption.Absorbed(
            ArmourMaterial.Plate, BlowType.Cut, CombatWeaponClass.OneHandedSword));
        Assert.Equal(0.94f, ShippedCombatRules.Absorption.Absorbed(
            ArmourMaterial.Plate, BlowType.Cut, CombatWeaponClass.TwoHandedSword));
        Assert.Equal(0.52f, ShippedCombatRules.Absorption.Absorbed(
            ArmourMaterial.Plate, BlowType.Cut, CombatWeaponClass.TwoHandedPolearm));
    }

    [Fact]
    public void OnlyPlateCuts_HaveWeaponOverrides()
    {
        foreach (ArmourMaterial material in System.Enum.GetValues(typeof(ArmourMaterial)))
        foreach (BlowType blow in System.Enum.GetValues(typeof(BlowType)))
        foreach (CombatWeaponClass weapon in System.Enum.GetValues(typeof(CombatWeaponClass)))
        {
            if (material == ArmourMaterial.Plate && blow == BlowType.Cut) continue;
            Assert.Equal(ShippedCombatRules.Absorption.Absorbed(material, blow),
                ShippedCombatRules.Absorption.Absorbed(material, blow, weapon));
        }
    }

    [Fact]
    public void ShippedAbsorption_OnlyReducesDamageAndLeavesSomeDamageThrough()
    {
        foreach (ArmourMaterial material in System.Enum.GetValues(typeof(ArmourMaterial)))
        foreach (BlowType blow in System.Enum.GetValues(typeof(BlowType)))
        foreach (CombatWeaponClass weapon in System.Enum.GetValues(typeof(CombatWeaponClass)))
        {
            var absorbed = ShippedCombatRules.Absorption.Absorbed(material, blow, weapon);
            Assert.True(absorbed >= 0f && absorbed < 1f, $"{material} / {blow}: {absorbed}");
        }
    }

    [Fact]
    public void AMissingBlowAbsorbsNothing()
    {
        var absorption = new ArmourAbsorption(new Dictionary<(ArmourMaterial, BlowType, CombatWeaponClass), float>
            { [(ArmourMaterial.Mail, BlowType.Blunt, CombatWeaponClass.Default)] = 0.5f });

        Assert.Equal(0.5f, absorption.Absorbed(ArmourMaterial.Mail, BlowType.Blunt));
        Assert.Equal(0.5f, absorption.Absorbed(ArmourMaterial.Mail, BlowType.Blunt, CombatWeaponClass.OneHandedSword));
        Assert.Equal(0f, absorption.Absorbed(ArmourMaterial.Mail, BlowType.Pierce));
    }

    [Fact]
    public void FirearmsAreNotAbsorbed()
    {
        var absorption = new ArmourAbsorption(new Dictionary<(ArmourMaterial, BlowType, CombatWeaponClass), float>
            {
                [(ArmourMaterial.Plate, BlowType.Firearm, CombatWeaponClass.Default)] = 0.5f,
                [(ArmourMaterial.Plate, BlowType.Firearm, CombatWeaponClass.OneHandedSword)] = 0.9f
            });

        Assert.Equal(0f, ShippedCombatRules.Absorption.Absorbed(ArmourMaterial.Plate, BlowType.Firearm));
        Assert.Equal(0f, absorption.Absorbed(ArmourMaterial.Plate, BlowType.Firearm));
        Assert.Equal(0f, absorption.Absorbed(ArmourMaterial.Plate, BlowType.Firearm, CombatWeaponClass.OneHandedSword));
    }

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidFractions_AreRejected(float fraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArmourAbsorption(
            new Dictionary<(ArmourMaterial, BlowType, CombatWeaponClass), float>
                { [(ArmourMaterial.Plate, BlowType.Cut, CombatWeaponClass.Default)] = fraction }));
    }

    private const float PlateArmour = 69f;

    private static float CutOnPlate(float magnitude, CombatWeaponClass weapon = CombatWeaponClass.OneHandedSword) =>
        Damage(magnitude, PlateArmour, 0.5f, 0.1f)
        * (1f - ShippedCombatRules.Absorption.Absorbed(ArmourMaterial.Plate, BlowType.Cut, weapon));

    private static float BluntOnPlate(float magnitude) =>
        Damage(magnitude, PlateArmour, 0.2f, 0.6f)
        * (1f - ShippedCombatRules.Absorption.Absorbed(ArmourMaterial.Plate, BlowType.Blunt));

    // Vanilla's armour formula, before body-part and perk modifiers.
    private static float Damage(float magnitude, float armour, float armourFactor, float bluntFactor)
    {
        var reduced = magnitude * 50f / (50f + armour);
        return bluntFactor * reduced + (1f - bluntFactor) * Math.Max(0f, reduced - armourFactor * armour);
    }
}

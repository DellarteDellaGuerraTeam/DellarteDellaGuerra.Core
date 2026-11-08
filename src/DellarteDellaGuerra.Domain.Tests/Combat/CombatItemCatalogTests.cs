using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Combat;
using DellarteDellaGuerra.Domain.Combat.Port;

namespace DellarteDellaGuerra.Domain.Tests.Combat;

public class CombatItemCatalogTests
{
    private class FakeRepository : ICombatItemRepository
    {
        public CombatItems Load() => new(
            new Dictionary<string, int> { ["castillon_c"] = 6 },
            new Dictionary<string, ArmourPiece> { ["jack"] = ArmourPiece.Covering(ArmourMaterial.Padded, HitLocation.Torso) },
            new ArmourAbsorption(new Dictionary<(ArmourMaterial, BlowType, CombatWeaponClass), float> { [(ArmourMaterial.Mail, BlowType.Cut, CombatWeaponClass.Default)] = 0.3f }),
            new HalfSword(0.5f), NonTipThrust.None);
    }

    [Fact]
    public void TheRules_ComeFromTheRepository()
    {
        var catalog = new CombatItemCatalog(new FakeRepository());

        Assert.Equal(0.3f, catalog.Absorption.Absorbed(ArmourMaterial.Mail, BlowType.Cut));
        Assert.Equal(0.5f, catalog.HalfSword.SwingMagnitudeScale(HalfSword.WeaponDescription));
    }

    [Fact]
    public void EmptyRules_LeaveEveryBlowAsVanillaDealsIt()
    {
        var items = CombatItems.Empty;

        Assert.Equal(0f, items.Absorption.Absorbed(ArmourMaterial.Plate, BlowType.Cut));
        Assert.Equal(1f, items.HalfSword.SwingMagnitudeScale(HalfSword.WeaponDescription));
        Assert.Equal(1f, items.NonTipThrust.MagnitudeScale("10_Pike"));
    }

    [Fact]
    public void ListedItems_AreFound()
    {
        var catalog = new CombatItemCatalog(new FakeRepository());

        Assert.True(catalog.TryGetTier("castillon_c", out var tier));
        Assert.Equal(6, tier);
        Assert.True(catalog.TryGetArmour("jack", out var piece));
        Assert.Equal(ArmourMaterial.Padded, piece.MaterialAt(HitLocation.Torso));
    }

    [Fact]
    public void UnlistedItems_AreNotFound()
    {
        var catalog = new CombatItemCatalog(new FakeRepository());

        Assert.False(catalog.TryGetTier("jack", out _));
        Assert.False(catalog.TryGetArmour("castillon_c", out _));
    }
}

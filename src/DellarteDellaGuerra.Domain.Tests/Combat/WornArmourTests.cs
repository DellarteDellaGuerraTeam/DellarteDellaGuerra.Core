using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Combat;

namespace DellarteDellaGuerra.Domain.Tests.Combat;

public class WornArmourTests
{
    private static readonly ArmourPiece ClothSleevedBrigandine = new(new Dictionary<HitLocation, ArmourMaterial>
    {
        [HitLocation.Torso] = ArmourMaterial.Brigandine,
        [HitLocation.Shoulder] = ArmourMaterial.Cloth,
        [HitLocation.Arm] = ArmourMaterial.Cloth
    });

    private static readonly ArmourPiece Gauntlets = ArmourPiece.Covering(ArmourMaterial.Plate, HitLocation.Arm);

    private static readonly ArmourPiece Harness = ArmourPiece.Covering(ArmourMaterial.Plate, HitLocation.Torso,
        HitLocation.Shoulder, HitLocation.Arm, HitLocation.Legs);

    [Fact]
    public void TorsoHit_MeetsTheBodyMaterial()
    {
        var worn = new WornArmour(ArmourPiece.Unarmoured, ClothSleevedBrigandine, ArmourPiece.Unarmoured, ArmourPiece.Unarmoured);

        Assert.Equal(ArmourMaterial.Brigandine, worn.MaterialAt(HitLocation.Torso));
    }

    [Fact]
    public void ShoulderHit_MeetsTheSleeveEvenWithGauntlets()
    {
        var worn = new WornArmour(ArmourPiece.Unarmoured, ClothSleevedBrigandine, Gauntlets, ArmourPiece.Unarmoured);

        Assert.Equal(ArmourMaterial.Cloth, worn.MaterialAt(HitLocation.Shoulder));
    }

    [Fact]
    public void ArmHit_MeetsTheStrongerOfSleeveAndGlove()
    {
        var gauntlets = new WornArmour(ArmourPiece.Unarmoured, ClothSleevedBrigandine, Gauntlets, ArmourPiece.Unarmoured);
        var bareHands = new WornArmour(ArmourPiece.Unarmoured, Harness, ArmourPiece.Unarmoured, ArmourPiece.Unarmoured);

        Assert.Equal(ArmourMaterial.Plate, gauntlets.MaterialAt(HitLocation.Arm));
        Assert.Equal(ArmourMaterial.Plate, bareHands.MaterialAt(HitLocation.Arm));
    }

    [Fact]
    public void HeadAndLegHits_MeetTheirOwnPiece()
    {
        var worn = new WornArmour(ArmourPiece.Covering(ArmourMaterial.Plate, HitLocation.Head), ArmourPiece.Unarmoured,
            ArmourPiece.Unarmoured, ArmourPiece.Covering(ArmourMaterial.Mail, HitLocation.Legs));

        Assert.Equal(ArmourMaterial.Plate, worn.MaterialAt(HitLocation.Head));
        Assert.Equal(ArmourMaterial.Mail, worn.MaterialAt(HitLocation.Legs));
    }

    [Fact]
    public void LegHit_MeetsABodyPieceThatReachesTheLegs()
    {
        var worn = new WornArmour(ArmourPiece.Unarmoured, Harness, ArmourPiece.Unarmoured,
            ArmourPiece.Covering(ArmourMaterial.Cloth, HitLocation.Legs));

        Assert.Equal(ArmourMaterial.Plate, worn.MaterialAt(HitLocation.Legs));
    }

    [Fact]
    public void LegHit_IgnoresABodyPieceThatStopsAtTheWaist()
    {
        var worn = new WornArmour(ArmourPiece.Unarmoured, ClothSleevedBrigandine, ArmourPiece.Unarmoured,
            ArmourPiece.Covering(ArmourMaterial.Cloth, HitLocation.Legs));

        Assert.Equal(ArmourMaterial.Cloth, worn.MaterialAt(HitLocation.Legs));
    }
}

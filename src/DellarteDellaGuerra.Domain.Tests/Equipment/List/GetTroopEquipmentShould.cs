using DellarteDellaGuerra.Domain.Equipment.Get;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;
using DellarteDellaGuerra.Domain.Equipment.Get.Port;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.Equipment.List;

public class GetTroopEquipmentShould
{
    private const string TroopId = "unrelevant troop id";
    private Mock<IList<EquipmentPool>> _equipmentPools;
    private Mock<ITroopBattleEquipmentProvider> _troopBattleEquipmentProvider;
    private Mock<ITroopSiegeEquipmentProvider> _troopSiegeEquipmentProvider;
    private Mock<ITroopCivilianEquipmentProvider> _troopCivilianEquipmentProvider;
    private Mock<IEncounterTypeProvider> _encounterTypeProvider;
    private GetTroopEquipment _getTroopEquipment;

    [SetUp]
    public void Setup()
    {
        _equipmentPools = new Mock<IList<EquipmentPool>>();
        _troopBattleEquipmentProvider = new Mock<ITroopBattleEquipmentProvider>();
        _troopSiegeEquipmentProvider = new Mock<ITroopSiegeEquipmentProvider>();
        _troopCivilianEquipmentProvider = new Mock<ITroopCivilianEquipmentProvider>();
        _encounterTypeProvider = new Mock<IEncounterTypeProvider>();
        _getTroopEquipment = new GetTroopEquipment(_encounterTypeProvider.Object,
            _troopBattleEquipmentProvider.Object, _troopSiegeEquipmentProvider.Object,
            _troopCivilianEquipmentProvider.Object);
    }

    [Test]
    public void ReturnBattleEquipmentPoolsWhenEncounterIsBattle()
    {
        _encounterTypeProvider.Setup(encounterProvider => encounterProvider.GetEncounterType())
            .Returns(EncounterType.Battle);
        _troopBattleEquipmentProvider.Setup(listBattleEquipment => listBattleEquipment
                .GetBattleTroopEquipmentPools(TroopId))
            .Returns(_equipmentPools.Object);

        var equipmentPools = _getTroopEquipment.GetEquipmentPools(TroopId);

        Assert.That(equipmentPools, Is.EqualTo(_equipmentPools.Object));
    }

    [Test]
    public void ReturnSiegeEquipmentPoolsWhenEncounterIsSiege()
    {
        _encounterTypeProvider.Setup(encounterProvider => encounterProvider.GetEncounterType())
            .Returns(EncounterType.Siege);
        _troopSiegeEquipmentProvider.Setup(troopSiegeEquipmentProvider =>
                troopSiegeEquipmentProvider.GetSiegeTroopEquipmentPools(TroopId))
            .Returns(_equipmentPools.Object);

        var equipmentPools = _getTroopEquipment.GetEquipmentPools(TroopId);

        Assert.That(equipmentPools, Is.EqualTo(_equipmentPools.Object));
    }

    [Test]
    public void ReturnCivilianEquipmentPoolsWhenEncounterIsCivilian()
    {
        _encounterTypeProvider.Setup(encounterProvider => encounterProvider.GetEncounterType())
            .Returns(EncounterType.Civilian);
        _troopCivilianEquipmentProvider.Setup(troopCivilianEquipmentProvider =>
                troopCivilianEquipmentProvider.GetCivilianTroopEquipmentPools(TroopId))
            .Returns(_equipmentPools.Object);

        var equipmentPools = _getTroopEquipment.GetEquipmentPools(TroopId);

        Assert.That(equipmentPools, Is.EqualTo(_equipmentPools.Object));
    }
}
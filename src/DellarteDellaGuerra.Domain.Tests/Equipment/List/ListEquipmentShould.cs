using DellarteDellaGuerra.Domain.Equipment.List;
using DellarteDellaGuerra.Domain.Equipment.List.Model;
using DellarteDellaGuerra.Domain.Equipment.List.Port;
using DellarteDellaGuerra.Domain.Equipment.List.Util;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.Equipment.List;

public class ListEquipmentShould
{
    private Mock<IDictionary<string, IList<EquipmentPool>>> _equipmentPools;
    private Mock<IListBattleEquipment> _listBattleEquipment;
    private Mock<IListCivilianEquipment> _listCivilianEquipment;
    private Mock<IListSiegeEquipment> _listSiegeEquipment;
    private Mock<IEncounterTypeProvider> _encounterProvider;
    private ListEquipment _listEquipment;

    [SetUp]
    public void Setup()
    {
        _equipmentPools = new Mock<IDictionary<string, IList<EquipmentPool>>>();
        _listBattleEquipment = new Mock<IListBattleEquipment>();
        _listCivilianEquipment = new Mock<IListCivilianEquipment>();
        _listSiegeEquipment = new Mock<IListSiegeEquipment>();
        _encounterProvider = new Mock<IEncounterTypeProvider>();
        _listEquipment = new ListEquipment(_encounterProvider.Object,
            _listBattleEquipment.Object,
            _listCivilianEquipment.Object, _listSiegeEquipment.Object);
    }

    [Test]
    public void ReturnBattleEquipmentPoolsWhenEncounterIsBattle()
    {
        _encounterProvider.Setup(encounterProvider => encounterProvider.GetEncounterType())
            .Returns(EncounterType.Battle);
        _listBattleEquipment.Setup(listBattleEquipment => listBattleEquipment.ListBattleEquipmentPools())
            .Returns(_equipmentPools.Object);

        var equipmentPools = _listEquipment.ListEquipmentPools();

        Assert.That(equipmentPools, Is.EqualTo(_equipmentPools.Object));
    }

    [Test]
    public void ReturnSiegeEquipmentPoolsWhenEncounterIsSiege()
    {
        _encounterProvider.Setup(encounterProvider => encounterProvider.GetEncounterType())
            .Returns(EncounterType.Siege);
        _listSiegeEquipment.Setup(listSiegeEquipment => listSiegeEquipment.ListSiegeEquipmentPools())
            .Returns(_equipmentPools.Object);

        var equipmentPools = _listEquipment.ListEquipmentPools();

        Assert.That(equipmentPools, Is.EqualTo(_equipmentPools.Object));
    }

    [Test]
    public void ReturnCivilianEquipmentPoolsWhenEncounterIsCivilian()
    {
        _encounterProvider.Setup(encounterProvider => encounterProvider.GetEncounterType())
            .Returns(EncounterType.Civilian);
        _listCivilianEquipment.Setup(listCivilianEquipment => listCivilianEquipment.ListCivilianEquipmentPools())
            .Returns(_equipmentPools.Object);

        var equipmentPools = _listEquipment.ListEquipmentPools();

        Assert.That(equipmentPools, Is.EqualTo(_equipmentPools.Object));
    }
}
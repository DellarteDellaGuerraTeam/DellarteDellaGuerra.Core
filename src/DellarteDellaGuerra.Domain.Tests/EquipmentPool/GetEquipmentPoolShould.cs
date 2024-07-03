using System.Xml.Linq;
using DellarteDellaGuerra.Domain.EquipmentPool;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;
using DellarteDellaGuerra.Domain.EquipmentPool.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Util;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.EquipmentPool;

public class GetEquipmentPoolShould
{
    private const string TroopId = "unrelevant troop id";
    private readonly Domain.EquipmentPool.Model.EquipmentPool _equipmentPool = CreateEquipmentPool();
    private Mock<IList<Domain.EquipmentPool.Model.EquipmentPool>> _equipmentPools;
    private Mock<ITroopBattleEquipmentProvider> _troopBattleEquipmentProvider;
    private Mock<ITroopSiegeEquipmentProvider> _troopSiegeEquipmentProvider;
    private Mock<ITroopCivilianEquipmentProvider> _troopCivilianEquipmentProvider;
    private Mock<IEncounterTypeProvider> _encounterTypeProvider;
    private Mock<IEquipmentPoolPicker> _equipmentPicker;
    private GetEquipmentPool _getEquipmentPool;

    [SetUp]
    public void Setup()
    {
        _equipmentPools = new Mock<IList<Domain.EquipmentPool.Model.EquipmentPool>>();
        _troopBattleEquipmentProvider = new Mock<ITroopBattleEquipmentProvider>();
        _troopSiegeEquipmentProvider = new Mock<ITroopSiegeEquipmentProvider>();
        _troopCivilianEquipmentProvider = new Mock<ITroopCivilianEquipmentProvider>();
        _encounterTypeProvider = new Mock<IEncounterTypeProvider>();
        _equipmentPicker = new Mock<IEquipmentPoolPicker>();
        _getEquipmentPool = new GetEquipmentPool(_encounterTypeProvider.Object,
            _troopBattleEquipmentProvider.Object, _troopSiegeEquipmentProvider.Object,
            _troopCivilianEquipmentProvider.Object, _equipmentPicker.Object);
    }

    [Test]
    public void ReturnBattleEquipmentPoolsWhenEncounterIsBattle()
    {
        _encounterTypeProvider.Setup(encounterProvider => encounterProvider.GetEncounterType())
            .Returns(EncounterType.Battle);
        _troopBattleEquipmentProvider.Setup(listBattleEquipment => listBattleEquipment
                .GetBattleTroopEquipmentPools(TroopId))
            .Returns(_equipmentPools.Object);
        _equipmentPicker.Setup(equipmentPicker => equipmentPicker.PickEquipmentPool(_equipmentPools.Object))
            .Returns(_equipmentPool);

        var equipment = _getEquipmentPool.GetTroopEquipmentPool(TroopId);

        Assert.That(equipment, Is.EqualTo(_equipmentPool));
    }

    [Test]
    public void ReturnSiegeEquipmentPoolsWhenEncounterIsSiege()
    {
        _encounterTypeProvider.Setup(encounterProvider => encounterProvider.GetEncounterType())
            .Returns(EncounterType.Siege);
        _troopSiegeEquipmentProvider.Setup(troopSiegeEquipmentProvider =>
                troopSiegeEquipmentProvider.GetSiegeTroopEquipmentPools(TroopId))
            .Returns(_equipmentPools.Object);
        _equipmentPicker.Setup(equipmentPicker => equipmentPicker.PickEquipmentPool(_equipmentPools.Object))
            .Returns(_equipmentPool);

        var equipment = _getEquipmentPool.GetTroopEquipmentPool(TroopId);

        Assert.That(equipment, Is.EqualTo(_equipmentPool));
    }

    [Test]
    public void ReturnCivilianEquipmentPoolsWhenEncounterIsCivilian()
    {
        _encounterTypeProvider.Setup(encounterProvider => encounterProvider.GetEncounterType())
            .Returns(EncounterType.Civilian);
        _troopCivilianEquipmentProvider.Setup(troopCivilianEquipmentProvider =>
                troopCivilianEquipmentProvider.GetCivilianTroopEquipmentPools(TroopId))
            .Returns(_equipmentPools.Object);
        _equipmentPicker.Setup(equipmentPicker => equipmentPicker.PickEquipmentPool(_equipmentPools.Object))
            .Returns(_equipmentPool);

        var equipment = _getEquipmentPool.GetTroopEquipmentPool(TroopId);

        Assert.That(equipment, Is.EqualTo(_equipmentPool));
    }

    private static Domain.EquipmentPool.Model.EquipmentPool CreateEquipmentPool()
    {
        return new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment> { CreateEquipmentNode() }, 0);
    }

    private static Equipment CreateEquipmentNode()
    {
        return new Equipment(XDocument.Parse("<EquipmentLoadout/>").Root!);
    }
}
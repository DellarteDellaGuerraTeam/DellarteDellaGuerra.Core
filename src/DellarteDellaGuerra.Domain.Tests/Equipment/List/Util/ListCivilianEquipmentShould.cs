using DellarteDellaGuerra.Domain.Equipment.List.Model;
using DellarteDellaGuerra.Domain.Equipment.List.Port;
using DellarteDellaGuerra.Domain.Equipment.List.Util;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.Equipment.List.Util;

public class ListCivilianEquipmentShould
{
    private Mock<Dictionary<string, IList<EquipmentPool>>> _equipmentPools;

    private Mock<ICivilianEquipmentRepository> _characterCivilianEquipmentRepository;

    private ListCivilianEquipment _listCivilianEquipment;

    [SetUp]
    public void Setup()
    {
        _equipmentPools = new Mock<Dictionary<string, IList<EquipmentPool>>>();
        _characterCivilianEquipmentRepository = new Mock<ICivilianEquipmentRepository>();
        _listCivilianEquipment = new ListCivilianEquipment(_characterCivilianEquipmentRepository.Object);
    }

    [Test]
    public void ReturnSiegeEquipment()
    {
        _characterCivilianEquipmentRepository.Setup(repository => repository.GetCivilianEquipmentByCharacterAndPool())
            .Returns(_equipmentPools.Object);

        var equipmentPools = _listCivilianEquipment.ListCivilianEquipmentPools();

        Assert.That(equipmentPools, Is.EqualTo(_equipmentPools.Object));
    }
}
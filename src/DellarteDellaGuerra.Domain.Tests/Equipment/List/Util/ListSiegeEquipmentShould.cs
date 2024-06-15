using DellarteDellaGuerra.Domain.Equipment.List.Model;
using DellarteDellaGuerra.Domain.Equipment.List.Port;
using DellarteDellaGuerra.Domain.Equipment.List.Util;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.Equipment.List.Util;

public class ListSiegeEquipmentShould
{
    private Mock<Dictionary<string, IList<EquipmentPool>>> _equipmentPools;

    private Mock<ISiegeEquipmentRepository> _characterSiegeEquipmentRepository;

    private ListSiegeEquipment _listSiegeEquipment;

    [SetUp]
    public void Setup()
    {
        _equipmentPools = new Mock<Dictionary<string, IList<EquipmentPool>>>();
        _characterSiegeEquipmentRepository = new Mock<ISiegeEquipmentRepository>();
        _listSiegeEquipment = new ListSiegeEquipment(_characterSiegeEquipmentRepository.Object);
    }

    [Test]
    public void ReturnSiegeEquipment()
    {
        _characterSiegeEquipmentRepository.Setup(repository => repository.GetSiegeEquipmentByCharacterAndPool())
            .Returns(_equipmentPools.Object);

        var equipmentPools = _listSiegeEquipment.ListSiegeEquipmentPools();

        Assert.That(equipmentPools, Is.EqualTo(_equipmentPools.Object));
    }
}
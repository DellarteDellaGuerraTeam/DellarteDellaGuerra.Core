using System.Xml.Linq;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;
using DellarteDellaGuerra.Domain.EquipmentPool.Util;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.EquipmentPool.Util;

public class EquipmentPoolPickerShould
{
    private Mock<IRandom> _random;

    private IEquipmentPoolPicker _equipmentPoolPicker;

    [SetUp]
    public void SetUp()
    {
        _random = new Mock<IRandom>();
        _equipmentPoolPicker = new EquipmentPoolPoolPicker(_random.Object);
    }

    [Test]
    public void PickFirstEquipmentFromEquipmentPoolsWhenRandomReturnsZero()
    {
        var firstEquipmentPool = new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment>
        {
            CreateEquipmentNode("0")
        }, 0);
        var secondEquipmentPool = new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment>
        {
            CreateEquipmentNode("1"),
            CreateEquipmentNode("2")
        }, 1);
        var equipmentPools = new List<Domain.EquipmentPool.Model.EquipmentPool> { firstEquipmentPool, secondEquipmentPool };

        _random.Setup(random => random.Next(0, 3)).Returns(0);

        var equipment = _equipmentPoolPicker.PickEquipmentPool(equipmentPools);

        Assert.That(equipment, Is.EqualTo(firstEquipmentPool));
    }

    [Test]
    public void PickSecondEquipmentFromEquipmentPoolsWhenRandomReturnsOne()
    {
        var firstEquipmentPool = new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment>
        {
            CreateEquipmentNode("0")
        }, 0);
        var secondEquipmentPool = new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment>
        {
            CreateEquipmentNode("1"),
            CreateEquipmentNode("2")
        }, 1);
        var equipmentPools = new List<Domain.EquipmentPool.Model.EquipmentPool> { firstEquipmentPool, secondEquipmentPool };

        _random.Setup(random => random.Next(0, 3)).Returns(1);

        var equipment = _equipmentPoolPicker.PickEquipmentPool(equipmentPools);

        Assert.That(equipment, Is.EqualTo(secondEquipmentPool));
    }

    [Test]
    public void PickThirdEquipmentFromEquipmentPoolsWhenRandomReturnsTwo()
    {
        var firstEquipmentPool = new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment>
        {
            CreateEquipmentNode("0")
        }, 0);
        var secondEquipmentPool = new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment>
        {
            CreateEquipmentNode("1"),
            CreateEquipmentNode("2")
        }, 1);
        var equipmentPools = new List<Domain.EquipmentPool.Model.EquipmentPool> { firstEquipmentPool, secondEquipmentPool };

        _random.Setup(random => random.Next(0, 3)).Returns(2);

        var equipment = _equipmentPoolPicker.PickEquipmentPool(equipmentPools);

        Assert.That(equipment, Is.EqualTo(secondEquipmentPool));
    }

    [Test]
    public void ReturnEmptyEquipmentPoolWhenEquipmentPoolsIsEmpty()
    {
        var equipmentPools = new List<Domain.EquipmentPool.Model.EquipmentPool>(new List<Domain.EquipmentPool.Model.EquipmentPool>
        {
            new(new List<Equipment>(), 0),
            new(new List<Equipment>(), 1)
        });

        var equipment = _equipmentPoolPicker.PickEquipmentPool(equipmentPools);

        Assert.That(equipment, Is.EqualTo(new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment>(), 0)));
    }

    [Test]
    public void ReturnEmptyEquipmentPoolWhenEquipmentPoolsIsNull()
    {
        var equipment = _equipmentPoolPicker.PickEquipmentPool(null);

        Assert.That(equipment, Is.EqualTo(new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment>(), 0)));
    }

    private Equipment CreateEquipmentNode(string id)
    {
        XNode node = XDocument.Parse($"<EquipmentLoadout><HeroId>{id}</HeroId></EquipmentLoadout>").Root!;
        return new Equipment(node);
    }
}
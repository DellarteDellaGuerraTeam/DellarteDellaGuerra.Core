using System.Xml.Linq;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.EquipmentPool.Model;

public class EquipmentPoolShould
{
    [Test]
    public void EqualEquipmentPoolWithSameEquipmentReference()
    {
        int poolId = 0;
        var equipment = CreateEquipment("1");
        var leftEquipmentPool =
            new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment> { equipment }, poolId);
        var rightEquipmentPool =
            new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment> { equipment }, poolId);

        var isEqual = leftEquipmentPool.Equals(rightEquipmentPool);

        Assert.That(isEqual, Is.True);
    }

    [Test]
    public void EqualEquipmentPoolWithSameEquipmentContent()
    {
        int poolId = 0;
        var leftEquipment = CreateEquipment("1");
        var rightEquipment = CreateEquipment("1");
        var leftEquipmentPool =
            new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment> { leftEquipment }, poolId);
        var rightEquipmentPool =
            new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment> { rightEquipment }, poolId);

        var isEqual = leftEquipmentPool.Equals(rightEquipmentPool);

        Assert.That(isEqual, Is.True);
    }

    [Test]
    public void NotEqualEquipmentPoolWithDifferentEquipmentContent()
    {
        int poolId = 0;
        var leftEquipment = CreateEquipment("1");
        var rightEquipment = CreateEquipment("2");
        var leftEquipmentPool =
            new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment> { leftEquipment }, poolId);
        var rightEquipmentPool =
            new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment> { rightEquipment }, poolId);

        var isEqual = leftEquipmentPool.Equals(rightEquipmentPool);

        Assert.That(isEqual, Is.False);
    }

    [Test]
    public void NotEqualEquipmentPoolWithDifferentPoolId()
    {
        int leftPoolId = 0;
        int rightPoolId = 1;
        var leftEquipment = CreateEquipment("1");
        var rightEquipment = CreateEquipment("1");
        var leftEquipmentPool =
            new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment> { leftEquipment }, leftPoolId);
        var rightEquipmentPool =
            new Domain.EquipmentPool.Model.EquipmentPool(new List<Equipment> { rightEquipment }, rightPoolId);

        var isEqual = leftEquipmentPool.Equals(rightEquipmentPool);

        Assert.That(isEqual, Is.False);
    }

    private Equipment CreateEquipment(string id)
    {
        return new Equipment(XDocument
            .Parse($"<EquipmentRoster id=\"EquipmentLoadout{id}\"/>").Root!);
    }
}

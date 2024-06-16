using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.Equipment.List.Model;

public class EquipmentPoolShould
{
    [Test]
    public void EqualEquipmentPoolWithSameEquipmentReference()
    {
        int poolId = 0;
        var equipment = CreateEquipment("1");
        var leftEquipmentPool =
            new EquipmentPool(new List<Domain.Equipment.Get.Model.Equipment> { equipment }, poolId);
        var rightEquipmentPool =
            new EquipmentPool(new List<Domain.Equipment.Get.Model.Equipment> { equipment }, poolId);

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
            new EquipmentPool(new List<Domain.Equipment.Get.Model.Equipment> { leftEquipment }, poolId);
        var rightEquipmentPool =
            new EquipmentPool(new List<Domain.Equipment.Get.Model.Equipment> { rightEquipment }, poolId);

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
            new EquipmentPool(new List<Domain.Equipment.Get.Model.Equipment> { leftEquipment }, poolId);
        var rightEquipmentPool =
            new EquipmentPool(new List<Domain.Equipment.Get.Model.Equipment> { rightEquipment }, poolId);

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
            new EquipmentPool(new List<Domain.Equipment.Get.Model.Equipment> { leftEquipment }, leftPoolId);
        var rightEquipmentPool =
            new EquipmentPool(new List<Domain.Equipment.Get.Model.Equipment> { rightEquipment }, rightPoolId);

        var isEqual = leftEquipmentPool.Equals(rightEquipmentPool);

        Assert.That(isEqual, Is.False);
    }

    private Domain.Equipment.Get.Model.Equipment CreateEquipment(string id)
    {
        return new Domain.Equipment.Get.Model.Equipment(XDocument
            .Parse($"<EquipmentRoster id=\"Equipment{id}\"/>").Root!);
    }
}

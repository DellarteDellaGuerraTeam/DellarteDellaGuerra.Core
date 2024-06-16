using System.Xml.Linq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.Equipment.List.Model;

public class EquipmentShould
{
    [Test]
    public void EqualEquipmentWithSameEquipmentReference()
    {
        var equipment = new Domain.Equipment.Get.Model.Equipment(CreateEquipmentNode("1"));

        var isEqual = equipment.Equals(equipment);

        Assert.That(isEqual, Is.True);
    }

    [Test]
    public void EqualEquipmentWithSameEquipmentContent()
    {
        var leftEquipment = new Domain.Equipment.Get.Model.Equipment(CreateEquipmentNode("1"));
        var rightEquipment = new Domain.Equipment.Get.Model.Equipment(CreateEquipmentNode("1"));

        var isEqual = leftEquipment.Equals(rightEquipment);

        Assert.That(isEqual, Is.True);
    }

    [Test]
    public void NotEqualEquipmentWithDifferentContent()
    {
        var leftEquipment = new Domain.Equipment.Get.Model.Equipment(CreateEquipmentNode("1"));
        var rightEquipment = new Domain.Equipment.Get.Model.Equipment(CreateEquipmentNode("2"));

        var isEqual = leftEquipment.Equals(rightEquipment);

        Assert.That(isEqual, Is.False);
    }

    private XNode CreateEquipmentNode(string id)
    {
        return XDocument.Parse($"<EquipmentRoster id=\"Equipment{id}\"/>").Root!;
    }
}
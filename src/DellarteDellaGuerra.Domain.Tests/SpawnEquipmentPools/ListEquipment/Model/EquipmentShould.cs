using System.Xml.Linq;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Model;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.SpawnEquipmentPools.ListEquipment.Model;

public class EquipmentShould
{
    [Test]
    public void EqualEquipmentWithSameEquipmentReference()
    {
        var equipment = new Equipment(CreateEquipmentNode("1"));
            
        Assert.That(equipment, Is.EqualTo(equipment));
    }

    [Test]
    public void EqualEquipmentWithSameEquipmentContent()
    {
        var leftEquipment = new Equipment(CreateEquipmentNode("1"));
        var rightEquipment = new Equipment(CreateEquipmentNode("1"));

        Assert.That(leftEquipment, Is.EqualTo(rightEquipment));
    }

    [Test]
    public void NotEqualEquipmentWithDifferentContent()
    {
        var leftEquipment = new Equipment(CreateEquipmentNode("1"));
        var rightEquipment = new Equipment(CreateEquipmentNode("2"));

        Assert.That(leftEquipment, Is.Not.EqualTo(rightEquipment));
    }

    private XNode CreateEquipmentNode(string id)
    {
        return XDocument.Parse($"<EquipmentRoster id=\"Equipment{id}\"/>").Root!;
    }
}
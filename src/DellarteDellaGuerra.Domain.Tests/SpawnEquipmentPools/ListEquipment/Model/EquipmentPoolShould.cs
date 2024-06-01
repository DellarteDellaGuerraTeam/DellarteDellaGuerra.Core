using System.Xml.Linq;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Model;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.SpawnEquipmentPools.ListEquipment.Model;

public class EquipmentPoolShould
{
    [Test]
    public void EqualEquipmentPoolWithSameEquipmentReference()
    {
        int poolId = 0;
        Equipment equipment = CreateEquipment("1");

        var leftEquipmentPool = new EquipmentPool(new List<Equipment> { equipment }, poolId);
        var rightEquipmentPool = new EquipmentPool(new List<Equipment> { equipment }, poolId);
            
        Assert.That(leftEquipmentPool, Is.EqualTo(rightEquipmentPool));
    }

    [Test]
    public void EqualEquipmentPoolWithSameEquipmentContent()
    {
        int poolId = 0;
        Equipment leftEquipment = CreateEquipment("1");
        Equipment rightEquipment = CreateEquipment("1");

        var leftEquipmentPool = new EquipmentPool(new List<Equipment> { leftEquipment }, poolId);
        var rightEquipmentPool = new EquipmentPool(new List<Equipment> { rightEquipment }, poolId);

        Assert.That(leftEquipmentPool, Is.EqualTo(rightEquipmentPool));
    }

    [Test]
    public void NotEqualEquipmentPoolWithDifferentEquipmentContent()
    {
        int poolId = 0;
        Equipment leftEquipment = CreateEquipment("1");
        Equipment rightEquipment = CreateEquipment("2");

        var leftEquipmentPool = new EquipmentPool(new List<Equipment> { leftEquipment }, poolId);
        var rightEquipmentPool = new EquipmentPool(new List<Equipment> { rightEquipment }, poolId);

        Assert.That(leftEquipmentPool, Is.Not.EqualTo(rightEquipmentPool));
    }

    [Test]
    public void NotEqualEquipmentPoolWithDifferentPoolId()
    {
        int leftPoolId = 0;
        int rightPoolId = 1;
        Equipment leftEquipment = CreateEquipment("1");
        Equipment rightEquipment = CreateEquipment("1");

        var leftEquipmentPool = new EquipmentPool(new List<Equipment> { leftEquipment }, leftPoolId);
        var rightEquipmentPool = new EquipmentPool(new List<Equipment> { rightEquipment }, rightPoolId);

        Assert.That(leftEquipmentPool, Is.Not.EqualTo(rightEquipmentPool));
    }
    
    private Equipment CreateEquipment(string id)
    {
        return new Equipment(XDocument.Parse($"<EquipmentRoster id=\"Equipment{id}\"/>").Root!);
    }
}

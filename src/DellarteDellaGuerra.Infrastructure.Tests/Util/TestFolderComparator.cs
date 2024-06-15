using System.Collections;
using System.Xml.Linq;
using System.Xml.XPath;
using DellarteDellaGuerra.Domain.Equipment.List.Model;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.Util;

public class TestFolderComparator
{
    protected void AssertCharacterEquipmentPools(
        string expectedXmlFolderPath,
        IDictionary<string,IList<EquipmentPool>> allTroopEquipmentPools,
        string troopId,
        int poolId)
    {
        var expectedEquipmentPool = EvaluateFileXPath(
            $"{expectedXmlFolderPath}/{troopId}_pool{poolId}.xml", "Equipments/*");

        Assert.IsTrue(allTroopEquipmentPools[troopId]
            .Any(pool => Enumerable.SequenceEqual(pool.GetEquipmentLoadouts().Select(equipment => equipment.GetEquipmentNode()), expectedEquipmentPool, new XNodeEqualityComparer())));
    }

    protected IList<XNode> EvaluateFileXPath(string xmlFilePath, string xpath)
    {
        using var xmlStream = new FileStream(xmlFilePath, FileMode.Open);
        var document = XDocument.Load(xmlStream);

        var xPathEvaluation = (IEnumerable) document.XPathEvaluate(xpath);
        return xPathEvaluation.Cast<XNode>().ToList();
    }

    protected string ExpectedFolder(string filepath)
    {
        return Path.Combine(filepath, "Expected");
    }

    protected string InputFolder(string filepath)
    {
        return Path.Combine(filepath, "Input");
    }
}
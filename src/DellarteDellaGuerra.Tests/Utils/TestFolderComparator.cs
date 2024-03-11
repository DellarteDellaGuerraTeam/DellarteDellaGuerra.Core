using System.Collections;
using System.Xml.Linq;
using System.Xml.XPath;
using DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters;
using NUnit.Framework;

namespace DellarteDellaGuerra.Tests.Utils;

public class TestFolderComparator
{
    protected void AssertCharacterEquipmentPools(
        string expectedXmlFolderPath,
        IDictionary<string, IEquipmentPoolSorter> allTroopEquipmentPools,
        string troopId,
        int poolId)
    {
        var expectedEquipmentPool = EvaluateFileXPath(
            $"{expectedXmlFolderPath}/{troopId}_pool{poolId}.xml", "Equipments/*");

        Assert.IsTrue(allTroopEquipmentPools[troopId]
            .GetEquipmentPools()
            .Any(pool => pool.SequenceEqual(expectedEquipmentPool, new XNodeEqualityComparer())));
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
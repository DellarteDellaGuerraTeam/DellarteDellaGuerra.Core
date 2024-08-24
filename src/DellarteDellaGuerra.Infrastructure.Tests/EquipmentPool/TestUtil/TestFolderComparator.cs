using System.Collections;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.TestUtil;

public static class TestFolderComparator
{
    public static void AssertCharacterEquipmentPools(
        string expectedXmlFolderPath,
        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> allTroopEquipmentPools)
    {
        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> expectedEquipmentPoolsByCharacter =
            ReadEquipmentPoolFromDataFolder(expectedXmlFolderPath);

        Assert.That(allTroopEquipmentPools,
            Is.EquivalentTo(expectedEquipmentPoolsByCharacter));
    }

    public static IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> ReadEquipmentPoolFromDataFolder(
        string folderPath)
    {
        return Directory.EnumerateFiles(folderPath).ToImmutableSortedSet().Select(filePath =>
            {
                var match = Regex.Match(Path.GetFileName(filePath), "(.*)-pool([0-9]{1})\\.xml");
                if (!match.Success)
                    Assert.Fail("Invalid file name format. Expected: <characterId>-pool<poolId>.xml");

                var characterId = match.Groups[1].Value;
                var poolId = int.Parse(match.Groups[2].Value);

                var equipmentPoolNodes = EvaluateFileXPath(filePath, "Equipments/*")
                    .Select(node => new Equipment(node))
                    .ToList();

                return (equipmentPool: new Domain.EquipmentPool.Model.EquipmentPool(equipmentPoolNodes, poolId),
                    characterId);
            })
            .Where(tuple => !tuple.equipmentPool.IsEmpty())
            .GroupBy(tuple => tuple.characterId)
            .ToDictionary(tuple => tuple.Key,
                tuple => (IList<Domain.EquipmentPool.Model.EquipmentPool>)tuple.Select(tuple => tuple.equipmentPool)
                    .ToList());
    }

    public static string ExpectedFolder(string filepath)
    {
        return Path.Combine(filepath, "Expected");
    }

    public static string InputFolder(string filepath)
    {
        return Path.Combine(filepath, "Input");
    }

    private static IList<XNode> EvaluateFileXPath(string xmlFilePath, string xpath)
    {
        using var xmlStream = new FileStream(xmlFilePath, FileMode.Open);
        var document = XDocument.Load(xmlStream);

        var xPathEvaluation = (IEnumerable)document.XPathEvaluate(xpath);
        return xPathEvaluation.Cast<XNode>().ToList();
    }
}
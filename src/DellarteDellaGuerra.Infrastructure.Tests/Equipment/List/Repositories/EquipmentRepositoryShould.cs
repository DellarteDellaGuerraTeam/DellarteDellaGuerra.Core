using System.Xml.Linq;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Utils;
using DellarteDellaGuerra.Infrastructure.Tests.Util;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.Equipment.List.Repositories;

public class EquipmentRepositoryShould : TestFolderComparator
{
    private const string ValidBattleEquipmentDataFolderPath = "Data\\EquipmentRepository\\ValidSymbols";
    private const string ValidBattleEquipmentInputXmlFile = $"{ValidBattleEquipmentDataFolderPath}\\Input\\valid_pool_symbols.xml";
    private const string InvalidBattleEquipmentDataFolderPath = "Data\\EquipmentRepository\\InvalidSymbols";
    private const string InvalidBattleEquipmentPoolInputFile = $"{InvalidBattleEquipmentDataFolderPath}\\Input\\invalid_pool_symbols.xml";

    [Test]
    public void ReadingBattleEquipmentFromXml_WithMultipleTroopsInXml_GroupsBattleEquipmentIntoPools()
    {
        var recruitId = "vlandian_recruit";
        var footmanId = "vlandian_footman";
        var troopEquipmentReader = new EquipmentRepository(CreateXmlProcessor(ValidBattleEquipmentInputXmlFile));

        var allTroopEquipmentPools = troopEquipmentReader.GetEquipmentPoolsByCharacter();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(2));
        Assert.That(allTroopEquipmentPools[recruitId].Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[footmanId].Count, Is.EqualTo(4));

        AssertCharacterEquipmentPools(ExpectedFolder(ValidBattleEquipmentDataFolderPath), allTroopEquipmentPools, recruitId, 0);
        AssertCharacterEquipmentPools(ExpectedFolder(ValidBattleEquipmentDataFolderPath), allTroopEquipmentPools, footmanId, 0);
        AssertCharacterEquipmentPools(ExpectedFolder(ValidBattleEquipmentDataFolderPath), allTroopEquipmentPools, footmanId, 1);
        AssertCharacterEquipmentPools(ExpectedFolder(ValidBattleEquipmentDataFolderPath), allTroopEquipmentPools, footmanId, 2);
        AssertCharacterEquipmentPools(ExpectedFolder(ValidBattleEquipmentDataFolderPath), allTroopEquipmentPools, footmanId, 3);
    }

    [Test]
    public void ReadingBattleEquipmentFromXml_WithInvalidPoolValues_GroupsBattleEquipmentInPoolZero()
    {
        var recruitId = "vlandian_recruit";
        var troopEquipmentReader =
            new EquipmentRepository(CreateXmlProcessor(InvalidBattleEquipmentPoolInputFile));

        var allTroopEquipmentPools = troopEquipmentReader.GetEquipmentPoolsByCharacter();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].Count, Is.EqualTo(2));
        AssertCharacterEquipmentPools(ExpectedFolder(InvalidBattleEquipmentDataFolderPath), allTroopEquipmentPools, recruitId, 0);
        AssertCharacterEquipmentPools(ExpectedFolder(InvalidBattleEquipmentDataFolderPath), allTroopEquipmentPools, recruitId, 1);
    }

    private INpcCharacterXmlProcessor CreateXmlProcessor(string inputFilePath)
    {
        var xmlProcessor = new Mock<INpcCharacterXmlProcessor>();
        xmlProcessor.Setup(processor => processor.GetMergedXmlCharacterNodes()).Returns(XDocument.Load(inputFilePath));
        return xmlProcessor.Object;
    }
}

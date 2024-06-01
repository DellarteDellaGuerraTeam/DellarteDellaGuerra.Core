using System.Xml.Linq;
using DellarteDellaGuerra.Infrastructure.SpawnEquipmentPools.ListEquipment.Repositories;
using DellarteDellaGuerra.Infrastructure.SpawnEquipmentPools.ListEquipment.Utils;
using DellarteDellaGuerra.Infrastructure.Tests.Utils;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.SpawnEquipmentPools.ListEquipment.Repositories;

public class CharacterEquipmentRepositoryShould : TestFolderComparator
{
    private const string ValidBattleEquipmentDataFolderPath = "Data\\CharacterEquipmentRepository\\ValidSymbols";
    private const string ValidBattleEquipmentInputXmlFile = $"{ValidBattleEquipmentDataFolderPath}\\Input\\valid_pool_symbols.xml";
    private const string InvalidBattleEquipmentDataFolderPath = "Data\\CharacterEquipmentRepository\\InvalidSymbols";
    private const string InvalidBattleEquipmentPoolInputFile = $"{InvalidBattleEquipmentDataFolderPath}\\Input\\invalid_pool_symbols.xml";

    [Test]
    public void ReadingBattleEquipmentFromXml_WithMultipleTroopsInXml_GroupsBattleEquipmentIntoPools()
    {
        var recruitId = "vlandian_recruit";
        var footmanId = "vlandian_footman";
        var troopEquipmentReader = new CharacterEquipmentRepository(CreateXmlProcessor(ValidBattleEquipmentInputXmlFile));

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
        var troopEquipmentReader = new CharacterEquipmentRepository(CreateXmlProcessor(InvalidBattleEquipmentPoolInputFile));

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

using System.Xml.Linq;
using DellarteDellaGuerra.Tests.Utils;
using DellarteDellaGuerra.Xml;
using DellarteDellaGuerra.Xml.Characters.Repositories;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Tests.Xml;

public class CharacterEquipmentRepositoryTests : TestFolderComparator
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

        var allTroopEquipmentPools = troopEquipmentReader.FindEquipmentPoolsByCharacter();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(2));
        Assert.That(allTroopEquipmentPools[recruitId].GetEquipmentPools().Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[footmanId].GetEquipmentPools().Count, Is.EqualTo(4));

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

        var allTroopEquipmentPools = troopEquipmentReader.FindEquipmentPoolsByCharacter();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].GetEquipmentPools().Count, Is.EqualTo(2));
        AssertCharacterEquipmentPools(ExpectedFolder(InvalidBattleEquipmentDataFolderPath), allTroopEquipmentPools, recruitId, 0);
        AssertCharacterEquipmentPools(ExpectedFolder(InvalidBattleEquipmentDataFolderPath), allTroopEquipmentPools, recruitId, 1);
    }

    private IXmlProcessor CreateXmlProcessor(string inputFilePath)
    {
        var xmlProcessor = new Mock<IXmlProcessor>();
        xmlProcessor.Setup(processor => processor.GetMergedXmlCharacterNodes()).Returns(XDocument.Load(inputFilePath));
        return xmlProcessor.Object;
    }
}

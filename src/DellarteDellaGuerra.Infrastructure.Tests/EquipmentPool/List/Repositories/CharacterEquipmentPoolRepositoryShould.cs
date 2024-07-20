using System.Xml.Linq;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;
using Moq;
using NUnit.Framework;
using static DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.TestUtil.TestFolderComparator;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Repositories;

public class CharacterEquipmentPoolRepositoryShould
{
    private const string ValidBattleEquipmentDataFolderPath = "Data\\CharacterEquipmentPoolRepository\\ValidSymbols";

    private const string ValidBattleEquipmentInputXmlFile =
        $"{ValidBattleEquipmentDataFolderPath}\\Input\\valid_pool_symbols.xml";

    private const string InvalidBattleEquipmentDataFolderPath =
        "Data\\CharacterEquipmentPoolRepository\\InvalidSymbols";

    private const string InvalidBattleEquipmentPoolInputFile =
        $"{InvalidBattleEquipmentDataFolderPath}\\Input\\invalid_pool_symbols.xml";

    [Test]
    public void ReadingBattleEquipmentFromXml_WithMultipleTroopsInXml_GroupsBattleEquipmentIntoPools()
    {
        var troopEquipmentReader =
            new CharacterEquipmentPoolRepository(CreateXmlProcessor(ValidBattleEquipmentInputXmlFile));

        var allTroopEquipmentPools = troopEquipmentReader.GetEquipmentPoolsById();

        AssertCharacterEquipmentPools(ExpectedFolder(ValidBattleEquipmentDataFolderPath), allTroopEquipmentPools);
    }

    [Test]
    public void ReadingBattleEquipmentFromXml_WithInvalidPoolValues_GroupsBattleEquipmentInPoolZero()
    {
        var troopEquipmentReader =
            new CharacterEquipmentPoolRepository(CreateXmlProcessor(InvalidBattleEquipmentPoolInputFile));

        var allTroopEquipmentPools = troopEquipmentReader.GetEquipmentPoolsById();

        AssertCharacterEquipmentPools(ExpectedFolder(InvalidBattleEquipmentDataFolderPath), allTroopEquipmentPools);
    }

    private IXmlProcessor CreateXmlProcessor(string inputFilePath)
    {
        var xmlProcessor = new Mock<IXmlProcessor>();
        xmlProcessor.Setup(processor => processor.GetXmlNodes(CharacterEquipmentPoolRepository.NpcCharacterRootTag))
            .Returns(XDocument.Load(inputFilePath));
        return xmlProcessor.Object;
    }
}
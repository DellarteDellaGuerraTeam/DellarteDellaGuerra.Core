using System.Xml.Linq;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;
using Moq;
using NUnit.Framework;
using static DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.TestUtil.TestFolderComparator;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Repositories;

public class EquipmentRosterEquipmentPoolRepositoryShould
{
    private const string ValidBattleEquipmentDataFolderPath =
        "Data\\EquipmentRosterEquipmentPoolRepository\\ValidSymbols";

    private const string ValidBattleEquipmentInputXmlFile =
        $"{ValidBattleEquipmentDataFolderPath}\\Input\\valid_pool_symbols.xml";

    private const string InvalidBattleEquipmentDataFolderPath =
        "Data\\EquipmentRosterEquipmentPoolRepository\\InvalidSymbols";

    private const string InvalidBattleEquipmentPoolInputFile =
        $"{InvalidBattleEquipmentDataFolderPath}\\Input\\invalid_pool_symbols.xml";

    [Test]
    public void ReadingBattleEquipmentFromXml_WithMultipleTroopsInXml_GroupsBattleEquipmentIntoPools()
    {
        var troopEquipmentReader =
            new EquipmentRosterEquipmentPoolRepository(CreateXmlProcessor(ValidBattleEquipmentInputXmlFile));

        var allTroopEquipmentPools = troopEquipmentReader.GetEquipmentPoolsById();

        AssertCharacterEquipmentPools(ExpectedFolder(ValidBattleEquipmentDataFolderPath), allTroopEquipmentPools);
    }

    [Test]
    public void ReadingBattleEquipmentFromXml_WithInvalidPoolValues_GroupsBattleEquipmentInPoolZero()
    {
        var troopEquipmentReader =
            new EquipmentRosterEquipmentPoolRepository(CreateXmlProcessor(InvalidBattleEquipmentPoolInputFile));

        var allTroopEquipmentPools = troopEquipmentReader.GetEquipmentPoolsById();

        AssertCharacterEquipmentPools(ExpectedFolder(InvalidBattleEquipmentDataFolderPath), allTroopEquipmentPools);
    }

    private IXmlProcessor CreateXmlProcessor(string inputFilePath)
    {
        var xmlProcessor = new Mock<IXmlProcessor>();
        xmlProcessor
            .Setup(processor => processor.GetXmlNodes(EquipmentRosterEquipmentPoolRepository.EquipmentRostersRootTag))
            .Returns(XDocument.Load(inputFilePath));
        return xmlProcessor.Object;
    }
}
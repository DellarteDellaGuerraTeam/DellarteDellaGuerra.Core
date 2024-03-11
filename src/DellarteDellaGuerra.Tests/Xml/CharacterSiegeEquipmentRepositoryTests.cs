using System.Xml.Linq;
using DellarteDellaGuerra.Tests.Utils;
using DellarteDellaGuerra.Xml.Characters.Repositories;
using DellarteDellaGuerra.Xml.Characters.Repositories.EquipmentPoolSorters;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Tests.Xml;

public class CharacterSiegeEquipmentRepositoryTests : TestFolderComparator
{
    private const string ValidSiegeEquipmentDataFolderPath = "Data\\CharacterSiegeEquipmentRepository\\ValidSymbols";
    private const string InvalidSiegeEquipmentDataFolderPath = "Data\\CharacterSiegeEquipmentRepository\\InvalidSymbols";

    [Test]
    public void GettingSiegeEquipmentPools_WithMultiplePools_GroupsSiegeEquipmentIntoPools()
    {
        var recruitId = "vlandian_recruit";
        var characterEquipmentRepository =
            CreateCharacterEquipmentRepository(InputFolder(ValidSiegeEquipmentDataFolderPath));
        var troopEquipmentReader = new CharacterSiegeEquipmentRepository(characterEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.FindSiegeEquipmentByCharacterAndPool();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].GetEquipmentPools().Count, Is.EqualTo(2));

        AssertCharacterEquipmentPools(ExpectedFolder(ValidSiegeEquipmentDataFolderPath), allTroopEquipmentPools, recruitId, 0);
        AssertCharacterEquipmentPools(ExpectedFolder(ValidSiegeEquipmentDataFolderPath), allTroopEquipmentPools, recruitId, 1);
    }

    [Test]
    public void GettingSiegeEquipmentPools_WithInvalidSiegeFlags_DoesNotAddEquipmentToPools()
    {
        var recruitId = "vlandian_recruit";
        var characterEquipmentRepository =
            CreateCharacterEquipmentRepository(InputFolder(InvalidSiegeEquipmentDataFolderPath));
        var troopEquipmentReader = new CharacterSiegeEquipmentRepository(characterEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.FindSiegeEquipmentByCharacterAndPool();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].GetEquipmentPools().Count, Is.EqualTo(0));
    }

    private ICharacterEquipmentRepository CreateCharacterEquipmentRepository(string inputFolderPath)
    {
        List<List<XNode>> equipmentPoolsNodes = new List<List<XNode>>();
        foreach (var inputFilePath in Directory.EnumerateFiles(inputFolderPath))
        {
            var equipmentPoolNodes = EvaluateFileXPath(inputFilePath, "Equipments/*");
            equipmentPoolsNodes.Add(equipmentPoolNodes.ToList());
        }

        var equipmentPools = new Mock<IEquipmentPoolSorter>();
        equipmentPools.Setup(sorter => sorter.GetEquipmentPools())
            .Returns(equipmentPoolsNodes.ToList());

        var characterEquipmentRepository = new Mock<ICharacterEquipmentRepository>();
        characterEquipmentRepository
            .Setup(repository => repository.FindEquipmentPoolsByCharacter())
            .Returns(new Dictionary<string, IEquipmentPoolSorter>() {{ "vlandian_recruit", equipmentPools.Object }});
        return characterEquipmentRepository.Object;
    }
}

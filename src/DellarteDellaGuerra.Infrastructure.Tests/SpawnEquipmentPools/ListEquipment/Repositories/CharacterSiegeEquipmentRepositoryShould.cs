using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Model;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Port;
using DellarteDellaGuerra.Infrastructure.SpawnEquipmentPools.ListEquipment.Repositories;
using DellarteDellaGuerra.Infrastructure.Tests.Utils;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.SpawnEquipmentPools.ListEquipment.Repositories;

public class CharacterSiegeEquipmentRepositoryShould : TestFolderComparator
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

        var allTroopEquipmentPools = troopEquipmentReader.GetSiegeEquipmentByCharacterAndPool();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].Count, Is.EqualTo(2));

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

        var allTroopEquipmentPools = troopEquipmentReader.GetSiegeEquipmentByCharacterAndPool();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].Count, Is.EqualTo(0));
    }

    private ICharacterEquipmentRepository CreateCharacterEquipmentRepository(string inputFolderPath)
    {
        IList<EquipmentPool> equipmentPools = Directory.EnumerateFiles(inputFolderPath).Select((filePath, poolId) =>
        {
            var equipmentPoolNodes = EvaluateFileXPath(filePath, "Equipments/*").Select(node => new Domain.SpawnEquipmentPools.ListEquipment.Model.Equipment(node))
                .ToList();
            return new EquipmentPool(equipmentPoolNodes, poolId);
        }).ToList();

        var characterEquipmentRepository = new Mock<ICharacterEquipmentRepository>();
        characterEquipmentRepository
            .Setup(repository => repository.GetEquipmentPoolsByCharacter())
            .Returns(new Dictionary<string, IList<EquipmentPool>>() {{ "vlandian_recruit", equipmentPools }});
        return characterEquipmentRepository.Object;
    }
}

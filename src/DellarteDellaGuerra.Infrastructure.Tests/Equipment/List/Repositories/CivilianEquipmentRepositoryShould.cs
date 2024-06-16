using DellarteDellaGuerra.Domain.Equipment.Get.Model;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories;
using DellarteDellaGuerra.Infrastructure.Tests.Util;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.Equipment.List.Repositories;

public class CivilianEquipmentRepositoryShould : TestFolderComparator
{
    private const string ValidSiegeEquipmentDataFolderPath = "Data\\CivilianEquipmentRepository\\ValidSymbols";
    private const string InvalidSiegeEquipmentDataFolderPath = "Data\\CivilianEquipmentRepository\\InvalidSymbols";

    [Test]
    public void GettingCivilianEquipmentPools_WithMultiplePools_GroupsCivilianEquipmentIntoPools()
    {
        var recruitId = "vlandian_recruit";
        var characterEquipmentRepository =
            CreateCharacterEquipmentRepository(InputFolder(ValidSiegeEquipmentDataFolderPath));
        var troopEquipmentReader = new CivilianEquipmentRepository(characterEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.GetCivilianEquipmentByCharacterAndPool();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].Count, Is.EqualTo(2));

        AssertCharacterEquipmentPools(ExpectedFolder(ValidSiegeEquipmentDataFolderPath), allTroopEquipmentPools, recruitId, 0);
        AssertCharacterEquipmentPools(ExpectedFolder(ValidSiegeEquipmentDataFolderPath), allTroopEquipmentPools, recruitId, 1);
    }

    [Test]
    public void GettingCivilianEquipmentPools_WithInvalidCivilianFlags_DoesNotAddEquipmentToPools()
    {
        var recruitId = "vlandian_recruit";
        var characterEquipmentRepository =
            CreateCharacterEquipmentRepository(InputFolder(InvalidSiegeEquipmentDataFolderPath));
        var troopEquipmentReader = new SiegeEquipmentRepository(characterEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.GetSiegeEquipmentByCharacterAndPool();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].Count, Is.EqualTo(0));
    }

    private IEquipmentRepository CreateCharacterEquipmentRepository(string inputFolderPath)
    {
        IList<EquipmentPool> equipmentPools = Directory.EnumerateFiles(inputFolderPath).Select((filePath, poolId) =>
        {
            var equipmentPoolNodes = EvaluateFileXPath(filePath, "Equipments/*")
                .Select(node => new Domain.Equipment.Get.Model.Equipment(node))
                .ToList();
            return new EquipmentPool(equipmentPoolNodes, poolId);
        }).ToList();

        var characterEquipmentRepository = new Mock<IEquipmentRepository>();
        characterEquipmentRepository
            .Setup(repository => repository.GetEquipmentPoolsByCharacter())
            .Returns(new Dictionary<string, IList<EquipmentPool>>() {{ "vlandian_recruit", equipmentPools }});
        return characterEquipmentRepository.Object;
    }
}

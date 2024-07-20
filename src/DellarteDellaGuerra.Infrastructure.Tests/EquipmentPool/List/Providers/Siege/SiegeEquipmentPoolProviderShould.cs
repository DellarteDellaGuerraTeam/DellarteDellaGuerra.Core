using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Siege;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using Moq;
using NUnit.Framework;
using static DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.TestUtil.TestFolderComparator;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Providers.Siege;

public class SiegeEquipmentPoolProviderShould
{
    private const string ValidSiegeEquipmentDataFolderPath = "Data\\SiegeEquipmentPoolProvider\\ValidSymbols";
    private const string InvalidSiegeEquipmentDataFolderPath = "Data\\SiegeEquipmentPoolProvider\\InvalidSymbols";

    private const string MultipleEquipmentRepositoriesDataFolderPath =
        "Data\\SiegeEquipmentPoolProvider\\MultipleRepos";

    private const string FirstEquipmentRepositoryDataFolderPath =
        "Data\\SiegeEquipmentPoolProvider\\MultipleRepos\\FirstRepo";

    private const string SecondEquipmentRepositoryDataFolderPath =
        "Data\\SiegeEquipmentPoolProvider\\MultipleRepos\\SecondRepo";

    private Mock<ILogger> _logger;
    private ILoggerFactory _loggerFactory;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(factory => factory.CreateLogger<SiegeEquipmentPoolProvider>())
            .Returns(_logger.Object);
        _loggerFactory = loggerFactory.Object;
    }

    [Test]
    public void GetEquipmentPoolsFromSingleRepository()
    {
        var characterEquipmentRepository =
            CreateEquipmentRepository(InputFolder(ValidSiegeEquipmentDataFolderPath));
        var troopEquipmentReader = new SiegeEquipmentPoolProvider(_loggerFactory, characterEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.GetSiegeEquipmentByCharacterAndPool();

        AssertCharacterEquipmentPools(ExpectedFolder(ValidSiegeEquipmentDataFolderPath),
            allTroopEquipmentPools);
    }

    [Test]
    public void GetEquipmentPoolsFromMultipleRepositories()
    {
        var firstEquipmentRepository =
            CreateEquipmentRepository(InputFolder(FirstEquipmentRepositoryDataFolderPath));
        var secondEquipmentRepository =
            CreateEquipmentRepository(InputFolder(SecondEquipmentRepositoryDataFolderPath));
        var troopEquipmentReader =
            new SiegeEquipmentPoolProvider(_loggerFactory, firstEquipmentRepository, secondEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.GetSiegeEquipmentByCharacterAndPool();

        AssertCharacterEquipmentPools(ExpectedFolder(MultipleEquipmentRepositoriesDataFolderPath),
            allTroopEquipmentPools);
        _logger.Verify(
            logger => logger.Warn(
                "'vlandian_recruit' is defined in multiple xml files. Only the first equipment list will be used.",
                null),
            Times.Once);
    }

    [Test]
    public void GettingSiegeEquipmentPools_WithInvalidSiegeFlags_DoesNotAddEquipmentToPools()
    {
        var recruitId = "vlandian_recruit";
        var characterEquipmentRepository =
            CreateEquipmentRepository(InputFolder(InvalidSiegeEquipmentDataFolderPath));
        var troopEquipmentReader = new SiegeEquipmentPoolProvider(_loggerFactory, characterEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.GetSiegeEquipmentByCharacterAndPool();

        Assert.NotNull(allTroopEquipmentPools);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].Count, Is.EqualTo(0));
    }

    private IEquipmentPoolRepository CreateEquipmentRepository(string inputFolderPath)
    {
        var characterEquipmentRepository = new Mock<IEquipmentPoolRepository>();
        characterEquipmentRepository
            .Setup(repository => repository.GetEquipmentPoolsById())
            .Returns(ReadEquipmentPoolFromDataFolder(inputFolderPath));
        return characterEquipmentRepository.Object;
    }
}
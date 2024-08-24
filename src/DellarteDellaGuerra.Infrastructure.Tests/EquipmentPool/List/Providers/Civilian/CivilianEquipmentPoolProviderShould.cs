using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Civilian;
using Moq;
using NUnit.Framework;
using static DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.TestUtil.TestFolderComparator;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Providers.Civilian;

public class CivilianEquipmentPoolProviderShould
{
    private const string ValidSiegeEquipmentDataFolderPath = "Data\\CivilianEquipmentPoolProvider\\ValidSymbols";
    private const string InvalidSiegeEquipmentDataFolderPath = "Data\\CivilianEquipmentPoolProvider\\InvalidSymbols";

    private const string MultipleEquipmentRepositoriesDataFolderPath =
        "Data\\CivilianEquipmentPoolProvider\\MultipleRepos";

    private const string FirstEquipmentRepositoryDataFolderPath =
        "Data\\CivilianEquipmentPoolProvider\\MultipleRepos\\FirstRepo";

    private const string SecondEquipmentRepositoryDataFolderPath =
        "Data\\CivilianEquipmentPoolProvider\\MultipleRepos\\SecondRepo";

    private Mock<ILogger> _logger;
    private ILoggerFactory _loggerFactory;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(factory => factory.CreateLogger<CivilianEquipmentPoolProvider>())
            .Returns(_logger.Object);
        _loggerFactory = loggerFactory.Object;
    }

    [Test]
    public void GetEquipmentPoolsFromSingleRepository()
    {
        var firstEquipmentRepository =
            CreateEquipmentRepository(InputFolder(ValidSiegeEquipmentDataFolderPath));
        var troopEquipmentReader = new CivilianEquipmentPoolProvider(_loggerFactory, firstEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.GetCivilianEquipmentByCharacterAndPool();

        AssertCharacterEquipmentPools(ExpectedFolder(ValidSiegeEquipmentDataFolderPath), allTroopEquipmentPools);
    }

    [Test]
    public void GetEquipmentPoolsFromMultipleRepositories()
    {
        var firstEquipmentRepository =
            CreateEquipmentRepository(InputFolder(FirstEquipmentRepositoryDataFolderPath));
        var secondEquipmentRepository =
            CreateEquipmentRepository(InputFolder(SecondEquipmentRepositoryDataFolderPath));
        var troopEquipmentReader =
            new CivilianEquipmentPoolProvider(_loggerFactory, firstEquipmentRepository,
                secondEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.GetCivilianEquipmentByCharacterAndPool();

        AssertCharacterEquipmentPools(ExpectedFolder(MultipleEquipmentRepositoriesDataFolderPath),
            allTroopEquipmentPools);
        _logger.Verify(
            logger => logger.Warn(
                "'vlandian_recruit' is defined in multiple xml files. Only the first equipment list will be used.",
                null),
            Times.Once);
    }

    [Test]
    public void NotReturnEquipmentPools_WhenInvalidSymbolsAreUsedInCondition()
    {
        var recruitId = "vlandian_recruit";
        var characterEquipmentRepository =
            CreateEquipmentRepository(InputFolder(InvalidSiegeEquipmentDataFolderPath));
        var troopEquipmentReader =
            new CivilianEquipmentPoolProvider(_loggerFactory, characterEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.GetCivilianEquipmentByCharacterAndPool();

        Assert.That(allTroopEquipmentPools, Is.Not.Null);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].Count, Is.EqualTo(0));
    }

    private IEquipmentPoolsRepository CreateEquipmentRepository(string inputFolderPath)
    {
        var characterEquipmentRepository = new Mock<IEquipmentPoolsRepository>();
        characterEquipmentRepository
            .Setup(repository => repository.GetEquipmentPoolsById())
            .Returns(ReadEquipmentPoolFromDataFolder(inputFolderPath));
        return characterEquipmentRepository.Object;
    }
}
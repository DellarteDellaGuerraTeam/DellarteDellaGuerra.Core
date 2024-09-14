using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Caching;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Civilian;
using Moq;
using NUnit.Framework;
using static DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.TestUtil.TestFolderComparator;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Providers.Civilian;

public class CivilianEquipmentPoolProviderShould
{
    private const string CachedObjectId = "irrelevant_cached_object_id";
    
    private const string ValidSiegeEquipmentDataFolderPath = "Data\\CivilianEquipmentPoolProvider\\ValidSymbols";
    private const string InvalidSiegeEquipmentDataFolderPath = "Data\\CivilianEquipmentPoolProvider\\InvalidSymbols";

    private const string MultipleEquipmentRepositoriesDataFolderPath =
        "Data\\CivilianEquipmentPoolProvider\\MultipleRepos";

    private const string FirstEquipmentRepositoryDataFolderPath =
        "Data\\CivilianEquipmentPoolProvider\\MultipleRepos\\FirstRepo";

    private const string SecondEquipmentRepositoryDataFolderPath =
        "Data\\CivilianEquipmentPoolProvider\\MultipleRepos\\SecondRepo";

    private Mock<ICacheProvider> _cacheProvider;
    private Mock<ILogger> _logger;
    private Mock<ILoggerFactory> _loggerFactory;

    [SetUp]
    public void SetUp()
    {
        _cacheProvider = new Mock<ICacheProvider>();
        _logger = new Mock<ILogger>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(factory => factory.CreateLogger<CivilianEquipmentPoolProvider>())
            .Returns(_logger.Object);
    }

    [Test]
    public void GetEquipmentPoolsFromSingleRepository()
    {
        var firstEquipmentRepository =
            CreateEquipmentRepository(InputFolder(ValidSiegeEquipmentDataFolderPath));
        var troopEquipmentReader =
            new CivilianEquipmentPoolProvider(_loggerFactory.Object, _cacheProvider.Object, firstEquipmentRepository);

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
            new CivilianEquipmentPoolProvider(_loggerFactory.Object, _cacheProvider.Object, firstEquipmentRepository,
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
            new CivilianEquipmentPoolProvider(_loggerFactory.Object, _cacheProvider.Object,
                characterEquipmentRepository);

        var allTroopEquipmentPools = troopEquipmentReader.GetCivilianEquipmentByCharacterAndPool();

        Assert.That(allTroopEquipmentPools, Is.Not.Null);
        Assert.That(allTroopEquipmentPools.Count, Is.EqualTo(1));
        Assert.That(allTroopEquipmentPools[recruitId].Count, Is.EqualTo(0));
    }

    [Test]
    public void GetCachedEquipmentPools()
    {
        var characterEquipmentRepository =
            CreateEquipmentRepository(InputFolder(InvalidSiegeEquipmentDataFolderPath));
        var troopEquipmentReader =
            new CivilianEquipmentPoolProvider(_loggerFactory.Object, _cacheProvider.Object,
                characterEquipmentRepository);

        _cacheProvider.Setup(provider => provider.CacheObject(It.IsAny<object>()))
            .Returns(CachedObjectId);
        _cacheProvider.Setup(provider =>
            provider.InvalidateCache(CachedObjectId, CampaignEvent.OnAfterSessionLaunched));

        var allTroopEquipmentPools = troopEquipmentReader.GetCivilianEquipmentByCharacterAndPool();

        _cacheProvider.Setup(cacheProvider =>
                cacheProvider.GetCachedObject<IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>>(
                    CachedObjectId))
            .Returns(allTroopEquipmentPools);

        var cachedAllTroopEquipmentPools = troopEquipmentReader.GetCivilianEquipmentByCharacterAndPool();

        _cacheProvider.VerifyAll();
        Assert.That(cachedAllTroopEquipmentPools, Is.EqualTo(allTroopEquipmentPools));
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
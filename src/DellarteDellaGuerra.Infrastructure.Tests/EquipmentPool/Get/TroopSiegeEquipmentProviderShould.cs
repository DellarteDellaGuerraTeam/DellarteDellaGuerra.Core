using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Port;
using DellarteDellaGuerra.Infrastructure.Caching;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.Get;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Siege;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.Get;

public class TroopSiegeEquipmentProviderShould
{
    private const string firstTroopId = "first unrelevant troop id";
    private const string secondTroopId = "second unrelevant troop id";
    private const string cachedOnLoadEquipmentPoolsKey = "cachedOnLoadEquipmentPoolsKey";

    private Mock<IList<Domain.EquipmentPool.Model.EquipmentPool>> _troopEquipmentPools;

    private Mock<ILoggerFactory> _loggerFactory;
    private Mock<ISiegeEquipmentPoolProvider> _siegeEquipmentRepository;
    private Mock<ICacheProvider> _cacheProvider;
    private ITroopSiegeEquipmentProvider _troopSiegeEquipmentProvider;

    [SetUp]
    public void Setup()
    {
        _troopEquipmentPools = new Mock<IList<Domain.EquipmentPool.Model.EquipmentPool>>();
        _siegeEquipmentRepository = new Mock<ISiegeEquipmentPoolProvider>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(factory => factory.CreateLogger<TroopSiegeEquipmentProvider>())
            .Returns(new Mock<ILogger>().Object);
        _cacheProvider = new Mock<ICacheProvider>();
        _cacheProvider
            .Setup(
                cache => cache.CacheObject(It.IsAny<Func<object>>(), CampaignEvent.OnSessionLaunched))
            .Returns(cachedOnLoadEquipmentPoolsKey);

        _troopSiegeEquipmentProvider =
            new TroopSiegeEquipmentProvider(_loggerFactory.Object, _siegeEquipmentRepository.Object,
                _cacheProvider.Object);
    }


    [Test]
    public void ReturnNoEquipmentPoolsIfTroopIdIsEmpty()
    {
        var actualEquipmentPools = _troopSiegeEquipmentProvider.GetSiegeTroopEquipmentPools("");

        Assert.That(actualEquipmentPools, Is.Empty);
    }

    [Test]
    public void ReturnNoEquipmentPoolsIfTroopIdIsNull()
    {
        var actualEquipmentPools = _troopSiegeEquipmentProvider.GetSiegeTroopEquipmentPools(null!);

        Assert.That(actualEquipmentPools, Is.Empty);
    }

    [Test]
    public void ReturnSiegeEquipmentPools()
    {
        _cacheProvider
            .Setup(cache =>
                cache.GetCachedObject<IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>>(cachedOnLoadEquipmentPoolsKey))
            .Returns(new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    firstTroopId, _troopEquipmentPools.Object
                },
                {
                    secondTroopId, null!
                }
            });

        var actualEquipmentPools = _troopSiegeEquipmentProvider.GetSiegeTroopEquipmentPools(firstTroopId);

        Assert.That(actualEquipmentPools, Is.EqualTo(_troopEquipmentPools.Object));
    }

    [Test]
    public void ReturnNoEquipmentPoolsIfTroopIdDoesNotExist()
    {
        _cacheProvider
            .Setup(cache =>
                cache.GetCachedObject<IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>>(cachedOnLoadEquipmentPoolsKey))
            .Returns(new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>());

        var actualEquipmentPools = _troopSiegeEquipmentProvider.GetSiegeTroopEquipmentPools(firstTroopId);

        Assert.That(actualEquipmentPools, Is.Empty);
    }
}
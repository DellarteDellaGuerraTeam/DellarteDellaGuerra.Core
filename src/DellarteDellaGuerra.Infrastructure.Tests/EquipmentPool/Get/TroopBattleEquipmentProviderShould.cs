using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Cache;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.Get;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.Get;

public class TroopBattleEquipmentProviderShould
{
    private const string firstTroopId = "first unrelevant troop id";
    private const string secondTroopId = "second unrelevant troop id";
    private const string cachedOnNewEquipmentPoolsKey = "cachedOnNewEquipmentPoolsKey";
    private const string cachedOnLoadEquipmentPoolsKey = "cachedOnLoadEquipmentPoolsKey";

    private Mock<IList<Domain.EquipmentPool.Model.EquipmentPool>> _troopEquipmentPools;

    private Mock<ILoggerFactory> _loggerFactory;
    private Mock<IBattleEquipmentRepository> _battleEquipmentRepository;
    private Mock<ICacheProvider> _cacheProvider;
    private TroopBattleEquipmentProvider _troopBattleEquipmentProvider;

    [SetUp]
    public void Setup()
    {
        _troopEquipmentPools = new Mock<IList<Domain.EquipmentPool.Model.EquipmentPool>>();
        _battleEquipmentRepository = new Mock<IBattleEquipmentRepository>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(factory => factory.CreateLogger<TroopBattleEquipmentProvider>())
            .Returns(new Mock<ILogger>().Object);
        _cacheProvider = new Mock<ICacheProvider>();
        _cacheProvider
            .Setup(
                cache => cache.CacheObject(It.IsAny<Func<object>>(), CachedEvent.OnSessionLaunched))
            .Returns(cachedOnLoadEquipmentPoolsKey);

        _troopBattleEquipmentProvider =
            new TroopBattleEquipmentProvider(_loggerFactory.Object, _battleEquipmentRepository.Object,
                _cacheProvider.Object);
    }


    [Test]
    public void ReturnNoEquipmentPoolsIfTroopIdIsEmpty()
    {
        var actualEquipmentPools = _troopBattleEquipmentProvider.GetBattleTroopEquipmentPools("");

        Assert.That(actualEquipmentPools, Is.Empty);
    }

    [Test]
    public void ReturnNoEquipmentPoolsIfTroopIdIsNull()
    {
        var actualEquipmentPools = _troopBattleEquipmentProvider.GetBattleTroopEquipmentPools(null!);

        Assert.That(actualEquipmentPools, Is.Empty);
    }

    [Test]
    public void ReturnBattleEquipmentPools()
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

        var actualEquipmentPools = _troopBattleEquipmentProvider.GetBattleTroopEquipmentPools(firstTroopId);

        Assert.That(actualEquipmentPools, Is.EqualTo(_troopEquipmentPools.Object));
    }

    [Test]
    public void ReturnNoEquipmentPoolsIfTroopIdDoesNotExist()
    {
        _cacheProvider
            .Setup(cache =>
                cache.GetCachedObject<IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>>(cachedOnLoadEquipmentPoolsKey))
            .Returns(new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>());

        var actualEquipmentPools = _troopBattleEquipmentProvider.GetBattleTroopEquipmentPools(firstTroopId);

        Assert.That(actualEquipmentPools, Is.Empty);
    }
}
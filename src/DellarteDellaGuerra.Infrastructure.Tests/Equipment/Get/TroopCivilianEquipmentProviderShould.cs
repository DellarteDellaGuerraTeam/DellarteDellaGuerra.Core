using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Equipment.Get.Model;
using DellarteDellaGuerra.Domain.Equipment.Get.Port;
using DellarteDellaGuerra.Infrastructure.Cache;
using DellarteDellaGuerra.Infrastructure.Equipment.Get;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.Equipment.Get;

public class TroopCivilianEquipmentProviderShould
{
    private const string firstTroopId = "first unrelevant troop id";
    private const string secondTroopId = "second unrelevant troop id";
    private const string cachedOnNewEquipmentPoolsKey = "cachedOnNewEquipmentPoolsKey";
    private const string cachedOnLoadEquipmentPoolsKey = "cachedOnLoadEquipmentPoolsKey";

    private Mock<IList<EquipmentPool>> _troopEquipmentPools;

    private Mock<ILoggerFactory> _loggerFactory;
    private Mock<ICivilianEquipmentRepository> _civilianEquipmentRepository;
    private Mock<ICacheProvider> _cacheProvider;
    private ITroopCivilianEquipmentProvider _troopCivilianEquipmentProvider;

    [SetUp]
    public void Setup()
    {
        _troopEquipmentPools = new Mock<IList<EquipmentPool>>();
        _civilianEquipmentRepository = new Mock<ICivilianEquipmentRepository>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(factory => factory.CreateLogger<TroopCivilianEquipmentProvider>())
            .Returns(new Mock<ILogger>().Object);
        _cacheProvider = new Mock<ICacheProvider>();
        _cacheProvider
            .Setup(
                cache => cache.CacheObjectOnGameLoadFinished(It.IsAny<Func<object>>()))
            .Returns(cachedOnLoadEquipmentPoolsKey);

        _cacheProvider
            .Setup(
                cache => cache.CacheObjectOnNewGameCreated(It.IsAny<Func<object>>()))
            .Returns(cachedOnNewEquipmentPoolsKey);

        _troopCivilianEquipmentProvider =
            new TroopCivilianEquipmentProvider(_loggerFactory.Object, _civilianEquipmentRepository.Object,
                _cacheProvider.Object);
    }


    [Test]
    public void ReturnNoEquipmentPoolsIfTroopIdIsEmpty()
    {
        var actualEquipmentPools = _troopCivilianEquipmentProvider.GetCivilianTroopEquipmentPools("");

        Assert.That(actualEquipmentPools, Is.Empty);
    }

    [Test]
    public void ReturnNoEquipmentPoolsIfTroopIdIsNull()
    {
        var actualEquipmentPools = _troopCivilianEquipmentProvider.GetCivilianTroopEquipmentPools(null!);

        Assert.That(actualEquipmentPools, Is.Empty);
    }

    [Test]
    public void ReturnCivilianEquipmentPools()
    {
        _cacheProvider
            .Setup(cache =>
                cache.GetCachedObject<IDictionary<string, IList<EquipmentPool>>>(cachedOnLoadEquipmentPoolsKey))
            .Returns(new Dictionary<string, IList<EquipmentPool>>
            {
                {
                    firstTroopId, _troopEquipmentPools.Object
                },
                {
                    secondTroopId, null!
                }
            });

        var actualEquipmentPools = _troopCivilianEquipmentProvider.GetCivilianTroopEquipmentPools(firstTroopId);

        Assert.That(actualEquipmentPools, Is.EqualTo(_troopEquipmentPools.Object));
    }

    [Test]
    public void ReturnNoEquipmentPoolsIfTroopIdDoesNotExist()
    {
        _cacheProvider
            .Setup(cache =>
                cache.GetCachedObject<IDictionary<string, IList<EquipmentPool>>>(cachedOnLoadEquipmentPoolsKey))
            .Returns(new Dictionary<string, IList<EquipmentPool>>());

        var actualEquipmentPools = _troopCivilianEquipmentProvider.GetCivilianTroopEquipmentPools(firstTroopId);

        Assert.That(actualEquipmentPools, Is.Empty);
    }
}
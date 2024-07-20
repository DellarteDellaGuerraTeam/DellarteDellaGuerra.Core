using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Battle;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Civilian;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Siege;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Providers.Battle;

public class BattleEquipmentPoolProviderShould
{
    private Mock<ILogger> _logger;
    private Mock<ILoggerFactory> _loggerFactory;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(factory => factory.CreateLogger<BattleEquipmentPoolProvider>())
            .Returns(_logger.Object);
    }

    [Test]
    public void GetEquipmentPoolsFromSingleRepository()
    {
        var equipmentRepository = EquipmentRepositoryReturns(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment1", "Equipment2" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment5", "Equipment6" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment7" }, 1)
                    }
                }
            });
        var siegeEquipmentRepository =
            SiegeEquipmentRepositoryReturns(new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>());
        var civilianEquipmentRepository =
            CivilianEquipmentRepositoryReturns(
                new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>());
        var battleEquipmentRepository = new BattleEquipmentPoolProvider(_loggerFactory.Object, siegeEquipmentRepository,
            civilianEquipmentRepository, equipmentRepository);

        var characterBattleEquipment = battleEquipmentRepository.GetBattleEquipmentByCharacterAndPool();

        Assert.That(characterBattleEquipment, Is.EqualTo(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment1", "Equipment2" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment5", "Equipment6" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment7" }, 1)
                    }
                }
            }));
    }

    [Test]
    public void GetEquipmentPoolsFromMultipleRepositories()
    {
        var leftEquipmentRepository = EquipmentRepositoryReturns(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment1", "Equipment2" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment4" }, 1)
                    }
                }
            });
        var rightEquipmentRepository = EquipmentRepositoryReturns(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character2", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment5", "Equipment6" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment7" }, 1)
                    }
                }
            });
        var siegeEquipmentRepository =
            SiegeEquipmentRepositoryReturns(new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>());
        var civilianEquipmentRepository =
            CivilianEquipmentRepositoryReturns(
                new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>());
        var battleEquipmentRepository = new BattleEquipmentPoolProvider(_loggerFactory.Object, siegeEquipmentRepository,
            civilianEquipmentRepository, leftEquipmentRepository, rightEquipmentRepository);

        var characterBattleEquipment = battleEquipmentRepository.GetBattleEquipmentByCharacterAndPool();

        Assert.That(characterBattleEquipment, Is.EqualTo(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment1", "Equipment2" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment5", "Equipment6" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment7" }, 1)
                    }
                }
            }));
    }

    [Test]
    public void GetEquipmentPoolsFromFirstRepositoryIfConflict()
    {
        var leftEquipmentRepository = EquipmentRepositoryReturns(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment1", "Equipment2" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment4" }, 1)
                    }
                }
            });
        var rightEquipmentRepository = EquipmentRepositoryReturns(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment5", "Equipment6" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment7" }, 1)
                    }
                }
            });
        var siegeEquipmentRepository =
            SiegeEquipmentRepositoryReturns(new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>());
        var civilianEquipmentRepository =
            CivilianEquipmentRepositoryReturns(
                new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>());
        var battleEquipmentRepository = new BattleEquipmentPoolProvider(_loggerFactory.Object, siegeEquipmentRepository,
            civilianEquipmentRepository, leftEquipmentRepository, rightEquipmentRepository);

        var characterBattleEquipment = battleEquipmentRepository.GetBattleEquipmentByCharacterAndPool();

        Assert.That(characterBattleEquipment, Is.EqualTo(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment1", "Equipment2" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment4" }, 1)
                    }
                }
            }));
        _logger.Verify(
            logger => logger.Warn(
                "'Character1' is defined in multiple xml files. Only the first equipment list will be used.", null),
            Times.Once);
    }

    [Test]
    public void NotGetCivilianCharacterEquipment()
    {
        var equipmentRepository = EquipmentRepositoryReturns(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "CivilianEquipment1", "CivilianEquipment2" }, 0),
                        CreateEquipmentPool(new[] { "CivilianEquipment3", "CivilianEquipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "CivilianEquipment5", "CivilianEquipment6" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "CivilianEquipment7" }, 1),
                        CreateEquipmentPool(new[] { "Equipment3", "CivilianEquipment7" }, 2)
                    }
                }
            });
        var siegeEquipmentRepository =
            SiegeEquipmentRepositoryReturns(new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>());
        var civilianEquipmentRepository =
            CivilianEquipmentRepositoryReturns(new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "CivilianEquipment1", "CivilianEquipment2" }, 0),
                        CreateEquipmentPool(new[] { "CivilianEquipment3", "CivilianEquipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "CivilianEquipment5", "CivilianEquipment6" }, 0),
                        CreateEquipmentPool(new[] { "CivilianEquipment7" }, 1)
                    }
                }
            });
        var battleEquipmentRepository = new BattleEquipmentPoolProvider(_loggerFactory.Object, siegeEquipmentRepository,
            civilianEquipmentRepository, equipmentRepository);

        var characterBattleEquipment = battleEquipmentRepository.GetBattleEquipmentByCharacterAndPool();

        Assert.That(characterBattleEquipment, Is.EqualTo(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>()
                },
                {
                    "Character2", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment3" }, 1),
                        CreateEquipmentPool(new[] { "Equipment3", "CivilianEquipment7" }, 2)
                    }
                }
            }));
    }

    [Test]
    public void NotGetSiegeCharacterEquipment()
    {
        var equipmentRepository = EquipmentRepositoryReturns(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "SiegeEquipment1", "SiegeEquipment2" }, 0),
                        CreateEquipmentPool(new[] { "SiegeEquipment3", "SiegeEquipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "SiegeEquipment5", "SiegeEquipment6" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "SiegeEquipment7" }, 1),
                        CreateEquipmentPool(new[] { "Equipment3", "SiegeEquipment7" }, 2)
                    }
                }
            });
        var siegeEquipmentRepository =
            SiegeEquipmentRepositoryReturns(new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "SiegeEquipment1", "SiegeEquipment2" }, 0),
                        CreateEquipmentPool(new[] { "SiegeEquipment3", "SiegeEquipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "SiegeEquipment5", "SiegeEquipment6" }, 0),
                        CreateEquipmentPool(new[] { "SiegeEquipment7" }, 1)
                    }
                }
            });
        var civilianEquipmentRepository =
            CivilianEquipmentRepositoryReturns(
                new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>());
        var battleEquipmentRepository = new BattleEquipmentPoolProvider(_loggerFactory.Object, siegeEquipmentRepository,
            civilianEquipmentRepository, equipmentRepository);

        var characterBattleEquipment = battleEquipmentRepository.GetBattleEquipmentByCharacterAndPool();

        Assert.That(characterBattleEquipment, Is.EqualTo(
            new Dictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "Character1", new List<Domain.EquipmentPool.Model.EquipmentPool>()
                },
                {
                    "Character2", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        CreateEquipmentPool(new[] { "Equipment3" }, 1),
                        CreateEquipmentPool(new[] { "Equipment3", "SiegeEquipment7" }, 2)
                    }
                }
            })
        );
    }

    private IEquipmentPoolRepository EquipmentRepositoryReturns(
        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> equipmentPoolsByCharacter)
    {
        var equipmentRepository = new Mock<IEquipmentPoolRepository>();
        equipmentRepository.Setup(repo => repo.GetEquipmentPoolsById()).Returns(equipmentPoolsByCharacter);
        return equipmentRepository.Object;
    }

    private ICivilianEquipmentPoolProvider CivilianEquipmentRepositoryReturns(
        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> equipmentPoolsByCharacter)
    {
        var equipmentRepository = new Mock<ICivilianEquipmentPoolProvider>();
        equipmentRepository.Setup(repo => repo.GetCivilianEquipmentByCharacterAndPool())
            .Returns(equipmentPoolsByCharacter);
        return equipmentRepository.Object;
    }

    private ISiegeEquipmentPoolProvider SiegeEquipmentRepositoryReturns(
        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> equipmentPoolsByCharacter)
    {
        var equipmentRepository = new Mock<ISiegeEquipmentPoolProvider>();
        equipmentRepository.Setup(repo => repo.GetSiegeEquipmentByCharacterAndPool())
            .Returns(equipmentPoolsByCharacter);
        return equipmentRepository.Object;
    }

    private Domain.EquipmentPool.Model.EquipmentPool CreateEquipmentPool(string[] equipmentIds, int poolId)
    {
        var equipment = equipmentIds.Select(equipmentId => CreateEquipment(equipmentId)).ToList();
        return new Domain.EquipmentPool.Model.EquipmentPool(equipment, poolId);
    }

    private Equipment CreateEquipment(string id)
    {
        var xml = $"<EquipmentRoster id=\"{id}\">\n" +
                  "<equipment slot=\"Item0\" id=\"Item.ddg_polearm_longspear2\"/>\n" +
                  "<equipment slot=\"Body\" id=\"Item.jack_sleeveless_with_splints2\"/>\n" +
                  "<equipment slot=\"Leg\" id=\"Item.hosen_with_boots_c2\"/>\n" +
                  "<equipment slot=\"Head\" id=\"Item.war_hat2\"/>\n" +
                  "</EquipmentRoster>";

        return new Equipment(XDocument.Parse(xml).Root!);
    }
}
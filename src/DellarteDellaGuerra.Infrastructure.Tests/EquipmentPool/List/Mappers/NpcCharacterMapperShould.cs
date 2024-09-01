using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Mappers;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using Moq;
using NUnit.Framework;
using Equipment = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters.Equipment;
using EquipmentRoster = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters.EquipmentRoster;
using EquipmentSet = DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters.EquipmentSet;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Mappers;

public class NpcCharacterMapperShould
{
    private Mock<IEquipmentRosterRepository> _equipmentRosterRepository;
    private Mock<IEquipmentSetMapper> _equipmentRosterMapper;
    private Mock<ILoggerFactory> _loggerFactory;

    private INpcCharacterMapper _npcCharacterMapper;

    [SetUp]
    public void SetUp()
    {
        _equipmentRosterRepository = new Mock<IEquipmentRosterRepository>();
        _equipmentRosterMapper = new Mock<IEquipmentSetMapper>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(factory => factory.CreateLogger<NpcCharacterMapper>())
            .Returns(new Mock<ILogger>().Object);
        _npcCharacterMapper =
            new NpcCharacterMapper(_equipmentRosterRepository.Object, _equipmentRosterMapper.Object,
                _loggerFactory.Object);
    }

    [Test]
    public void MapsEquipmentRoster()
    {
        NpcCharacter npcCharacter = new NpcCharacter
        {
            Id = "npc1",
            Equipments = new Equipments
            {
                EquipmentRoster = new List<EquipmentRoster>
                {
                    new()
                    {
                        Pool = "0",
                        Equipment = new List<Equipment>
                        {
                            new() { Slot = "Arm", Id = "item1" }
                        }
                    },
                    new()
                    {
                        Pool = "1",
                        Equipment = new List<Equipment>
                        {
                            new() { Slot = "Body", Id = "item2" }
                        }
                    },
                    new()
                    {
                        IsCivilian = "false",
                        IsSiege = "false",
                        Pool = "1",
                        Equipment = new List<Equipment>
                        {
                            new() { Slot = "Body", Id = "item2" }
                        }
                    }
                }
            }
        };

        IList<EquipmentRoster> equipmentPools = _npcCharacterMapper.MapToEquipmentRosters(npcCharacter);

        Assert.That(equipmentPools, Is.EqualTo(npcCharacter.Equipments.EquipmentRoster));
    }

    [Test]
    public void MapsEquipmentSets()
    {
        // Arrange
        NpcCharacter npcCharacter = new NpcCharacter
        {
            Id = "npc1",
            Equipments = new Equipments
            {
                EquipmentSet = new List<EquipmentSet>
                {
                    new()
                    {
                        Id = "equipmentSet1"
                    },
                    new()
                    {
                        Id = "equipmentSet2"
                    },
                    new()
                    {
                        Id = "equipmentSet3"
                    }
                }
            }
        };

        List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet> equipmentSets =
            new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet>
                { new() { IsCivilian = "a" }, new() { IsCivilian = "b" }, new() { IsCivilian = "c" } };

        _equipmentRosterRepository.Setup(repo => repo.GetEquipmentRosters()).Returns(new EquipmentRosters
        {
            EquipmentRoster = new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentRoster>
            {
                new()
                {
                    Id = "equipmentSet1",
                    EquipmentSet = new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet>
                    {
                        equipmentSets[0]
                    }
                },
                new()
                {
                    Id = "equipmentSet2",
                    EquipmentSet = new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet>
                    {
                        equipmentSets[1]
                    }
                },
                new()
                {
                    Id = "equipmentSet3",
                    EquipmentSet = new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet>
                    {
                        equipmentSets[2]
                    }
                }
            }
        });

        List<EquipmentRoster> mappedEquipmentRosters = new List<EquipmentRoster>
            { new() { Pool = "0" }, new() { Pool = "1" }, new() { Pool = "1" } };
        _equipmentRosterMapper.Setup(mapper => mapper.MapToEquipmentRoster(equipmentSets[0]))
            .Returns(mappedEquipmentRosters[0]);
        _equipmentRosterMapper.Setup(mapper => mapper.MapToEquipmentRoster(equipmentSets[1]))
            .Returns(mappedEquipmentRosters[1]);
        _equipmentRosterMapper.Setup(mapper => mapper.MapToEquipmentRoster(equipmentSets[2]))
            .Returns(mappedEquipmentRosters[2]);

        // Act
        var equipmentPools = _npcCharacterMapper.MapToEquipmentRosters(npcCharacter);

        // Assert
        Assert.That(equipmentPools, Is.EqualTo(mappedEquipmentRosters));
    }

    [Test]
    public void MapsCivilianEquipmentSetsToCivilianEquipmentOnly()
    {
        // Arrange
        NpcCharacter npcCharacter = new NpcCharacter
        {
            Id = "npc1",
            Equipments = new Equipments
            {
                EquipmentSet = new List<EquipmentSet>
                {
                    new()
                    {
                        IsCivilian = "true",
                        Id = "equipmentSet1"
                    },
                    new()
                    {
                        IsCivilian = "true",
                        Id = "equipmentSet2"
                    }
                }
            }
        };

        List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet> equipmentSets =
            new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet>
            {
                new()
                {
                    IsCivilian = "true",
                    Equipment =
                        new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.Equipment>
                        {
                            new() { Id = "item1" }
                        }
                },
                new() { IsCivilian = "false" },
                new()
                {
                    IsCivilian = "true",
                    Equipment =
                        new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.Equipment>
                        {
                            new() { Id = "item2" }
                        }
                }
            };

        _equipmentRosterRepository.Setup(repo => repo.GetEquipmentRosters()).Returns(new EquipmentRosters
        {
            EquipmentRoster = new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentRoster>
            {
                new()
                {
                    Id = "equipmentSet1",
                    EquipmentSet = equipmentSets
                },
                new()
                {
                    Id = "equipmentSet2",
                    EquipmentSet = equipmentSets
                }
            }
        });

        List<EquipmentRoster> mappedEquipmentRosters = new List<EquipmentRoster>
            { new() { Pool = "0" }, new() { Pool = "1" } };
        _equipmentRosterMapper.Setup(mapper => mapper.MapToEquipmentRoster(equipmentSets[0]))
            .Returns(mappedEquipmentRosters[0]);
        _equipmentRosterMapper.Setup(mapper => mapper.MapToEquipmentRoster(equipmentSets[2]))
            .Returns(mappedEquipmentRosters[1]);

        // Act
        IList<EquipmentRoster> equipmentRosters = _npcCharacterMapper.MapToEquipmentRosters(npcCharacter);

        // Assert
        Assert.That(equipmentRosters, Is.EqualTo(new List<EquipmentRoster>
        {
            mappedEquipmentRosters[0],
            mappedEquipmentRosters[1],
            mappedEquipmentRosters[0],
            mappedEquipmentRosters[1]
        }));
    }

    [Test]
    public void MapsSiegeEquipmentSetsToSiegeEquipmentOnly()
    {
        // Arrange
        NpcCharacter npcCharacter = new NpcCharacter
        {
            Id = "npc1",
            Equipments = new Equipments
            {
                EquipmentSet = new List<EquipmentSet>
                {
                    new()
                    {
                        IsSiege = "true",
                        Id = "equipmentSet1"
                    },
                    new()
                    {
                        IsSiege = "true",
                        Id = "equipmentSet2"
                    }
                }
            }
        };

        List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet> equipmentSets =
            new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet>
            {
                new()
                {
                    IsSiege = "true",
                    Equipment =
                        new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.Equipment>
                        {
                            new() { Id = "item1" }
                        }
                },
                new() { IsSiege = "false" },
                new()
                {
                    IsSiege = "true",
                    Equipment =
                        new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.Equipment>
                        {
                            new() { Id = "item2" }
                        }
                }
            };

        _equipmentRosterRepository.Setup(repo => repo.GetEquipmentRosters()).Returns(new EquipmentRosters
        {
            EquipmentRoster = new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentRoster>
            {
                new()
                {
                    Id = "equipmentSet1",
                    EquipmentSet = equipmentSets
                },
                new()
                {
                    Id = "equipmentSet2",
                    EquipmentSet = equipmentSets
                }
            }
        });

        List<EquipmentRoster> mappedEquipmentRosters = new List<EquipmentRoster>
            { new() { Pool = "0" }, new() { Pool = "1" } };
        _equipmentRosterMapper.Setup(mapper => mapper.MapToEquipmentRoster(equipmentSets[0]))
            .Returns(mappedEquipmentRosters[0]);
        _equipmentRosterMapper.Setup(mapper => mapper.MapToEquipmentRoster(equipmentSets[2]))
            .Returns(mappedEquipmentRosters[1]);

        // Act
        IList<EquipmentRoster> equipmentRosters = _npcCharacterMapper.MapToEquipmentRosters(npcCharacter);

        // Assert
        // Assert
        Assert.That(equipmentRosters, Is.EqualTo(new List<EquipmentRoster>
        {
            mappedEquipmentRosters[0],
            mappedEquipmentRosters[1],
            mappedEquipmentRosters[0],
            mappedEquipmentRosters[1]
        }));
    }
    
    [Test]
    public void OverridesEquipmentRosterSlotsWithRootEquipmentSlots()
    {
        NpcCharacter npcCharacter = new NpcCharacter
        {
            Id = "npc1",
            Equipments = new Equipments
            {
                EquipmentRoster = new List<EquipmentRoster>
                {
                    new()
                    {
                        Pool = "0",
                        Equipment = new List<Equipment>
                        {
                            new() { Slot = "Arm", Id = "item0" },
                            new() { Slot = "Body", Id = "item1" }
                        }
                    },
                    new()
                    {
                        Pool = "1",
                        Equipment = new List<Equipment>
                        {
                            new() { Slot = "Body", Id = "item2" }
                        }
                    },
                    new()
                    {
                        Pool = "2",
                        Equipment = new List<Equipment>
                        {
                            new() { Slot = "Arm", Id = "item3" }
                        }
                    }
                },
                EquipmentSet = new List<EquipmentSet>
                {
                    new()
                    {
                        Id = "equipmentSet1"
                    }
                },
                Equipment = new List<Equipment>
                {
                    new() { Slot = "Arm", Id = "item4" },
                    new() { Slot = "Item0", Id = "item0" }
                }
            }
        };

        EquipmentRosters equipmentRosters = new EquipmentRosters
        {
            EquipmentRoster = new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentRoster>
            {
                new()
                {
                    Id = "equipmentSet1",
                    EquipmentSet = new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.EquipmentSet>
                    {
                        new()
                        {
                            Equipment = new List<Infrastructure.EquipmentPool.List.Models.EquipmentRosters.Equipment>
                            {
                                new() { Slot = "Arm", Id = "item1" }
                            }
                        }
                    }
                }
            }
        };
        _equipmentRosterRepository.Setup(repo => repo.GetEquipmentRosters()).Returns(equipmentRosters);
        _equipmentRosterMapper.Setup(mapper =>
                mapper.MapToEquipmentRoster(equipmentRosters.EquipmentRoster[0].EquipmentSet[0]))
            .Returns(new EquipmentRoster
            {
                Pool = "0",
                Equipment = new List<Equipment>
                {
                    new() { Slot = "Arm", Id = "item1" }
                }
            });

        var equipmentPools = _npcCharacterMapper.MapToEquipmentRosters(npcCharacter);

        // Assert
        Assert.That(equipmentPools, Is.EquivalentTo(new List<EquipmentRoster>
        {
            new()
            {
                Pool = "0",
                Equipment = new List<Equipment>
                {
                    new() { Slot = "Arm", Id = "item4" },
                    new() { Slot = "Body", Id = "item1" },
                    new() { Slot = "Item0", Id = "item0" }
                }
            },
            new()
            {
                Pool = "1",
                Equipment = new List<Equipment>
                {
                    new() { Slot = "Body", Id = "item2" },
                    new() { Slot = "Arm", Id = "item4" },
                    new() { Slot = "Item0", Id = "item0" }
                }
            },
            new()
            {
                Pool = "2",
                Equipment = new List<Equipment>
                {
                    new() { Slot = "Arm", Id = "item4" },
                    new() { Slot = "Item0", Id = "item0" }
                }
            },
            new()
            {
                Pool = "0",
                Equipment = new List<Equipment>
                {
                    new() { Slot = "Arm", Id = "item4" },
                    new() { Slot = "Item0", Id = "item0" }
                }
            }
        }));
    }
}
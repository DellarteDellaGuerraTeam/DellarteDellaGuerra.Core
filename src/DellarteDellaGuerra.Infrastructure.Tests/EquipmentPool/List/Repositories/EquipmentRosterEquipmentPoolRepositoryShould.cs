using System.Xml.Linq;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Repositories;

public class EquipmentRosterEquipmentPoolRepositoryShould
{
    private Mock<IEquipmentRosterRepository> _equipmentRosterRepository;
    private EquipmentRosterEquipmentPoolsProvider _equipmentRosterEquipmentPoolsProvider;

    [SetUp]
    public void Setup()
    {
        _equipmentRosterRepository = new Mock<IEquipmentRosterRepository>();
        _equipmentRosterEquipmentPoolsProvider =
            new EquipmentRosterEquipmentPoolsProvider(_equipmentRosterRepository.Object);
    }
    
    private const string InvalidBattleEquipmentDataFolderPath =
        "Data\\EquipmentRosterEquipmentPoolsProvider\\InvalidSymbols";

    private const string InvalidBattleEquipmentPoolInputFile =
        $"{InvalidBattleEquipmentDataFolderPath}\\Input\\invalid_pool_symbols.xml";

    [Test]
    public void ReadingBattleEquipmentFromXml_WithMultipleTroopsInXml_GroupsBattleEquipmentIntoPools()
    {
        EquipmentRosters equipmentRosters = new EquipmentRosters
        {
            EquipmentRoster = new List<EquipmentRoster>
            {
                new()
                {
                    Id = "vlandian_recruit",
                    Culture = "Culture.empire",
                    EquipmentSet = new List<EquipmentSet>
                    {
                        new()
                        {
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_simple_hat_a" },
                                new() { Slot = "Body", Id = "Item.pleatedjacketfurtrim_red" },
                                new() { Slot = "Leg", Id = "Item.hosen_with_shoes_a" }
                            }
                        },
                        new()
                        {
                            Pool = "2",
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_simple_hat_b" },
                                new() { Slot = "Body", Id = "Item.pleatedjacketfurtrim_green" },
                                new() { Slot = "Leg", Id = "Item.hosen_with_shoes_b" }
                            }
                        },
                        new()
                        {
                            Pool = "0",
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_sheperds_hat" },
                                new() { Slot = "Body", Id = "Item.pleatedjacketfurtrim_black" },
                                new() { Slot = "Leg", Id = "Item.hosen_with_boots_a" }
                            }
                        },
                        new()
                        {
                            Pool = "1",
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_sheperds_hat" },
                                new() { Slot = "Body", Id = "Item.pleatedjacketfurtrim_blue" },
                                new() { Slot = "Leg", Id = "Item.hosen_with_boots_a" }
                            }
                        },
                        new()
                        {
                            Pool = "2",
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_simple_hat_b" },
                                new() { Slot = "Body", Id = "Item.pleatedgownfur_red" },
                                new() { Slot = "Leg", Id = "Item.hosen_with_boots_b" }
                            }
                        }
                    }
                },
                new()
                {
                    Id = "vlandian_footman",
                    Culture = "Culture.empire",
                    EquipmentSet = new List<EquipmentSet>
                    {
                        new()
                        {
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_simple_hat_a" }
                            }
                        }
                    }
                }
            }
        };
        _equipmentRosterRepository.Setup(repo => repo.GetEquipmentRosters()).Returns(equipmentRosters);

        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> allTroopEquipmentPools =
            _equipmentRosterEquipmentPoolsProvider.GetEquipmentPoolsById();

        Assert.That(allTroopEquipmentPools, Is.EquivalentTo(
            new Dictionary<string, List<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "vlandian_footman", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        new(new List<Domain.EquipmentPool.Model.Equipment>
                        {
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true">
                                    <Equipment slot="Head" id="Item.dadg_simple_hat_a" />
                                </EquipmentSet>
                                """))
                        }, 0)
                    }
                },
                {
                    "vlandian_recruit", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        new(new List<Domain.EquipmentPool.Model.Equipment>
                        {
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true">
                                    <Equipment slot="Head" id="Item.dadg_simple_hat_a" />
                                    <Equipment slot="Body" id="Item.pleatedjacketfurtrim_red" />
                                    <Equipment slot="Leg" id="Item.hosen_with_shoes_a" />
                                </EquipmentSet>
                                """)),
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true" pool="0">
                                    <Equipment slot="Head" id="Item.dadg_sheperds_hat" />
                                    <Equipment slot="Body" id="Item.pleatedjacketfurtrim_black" />
                                    <Equipment slot="Leg" id="Item.hosen_with_boots_a" />
                                </EquipmentSet>
                                """))
                        }, 0),
                        new(new List<Domain.EquipmentPool.Model.Equipment>
                        {
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true" pool="1">
                                    <Equipment slot="Head" id="Item.dadg_sheperds_hat" />
                                    <Equipment slot="Body" id="Item.pleatedjacketfurtrim_blue" />
                                    <Equipment slot="Leg" id="Item.hosen_with_boots_a" />
                                </EquipmentSet>
                                """))
                        }, 1),
                        new(new List<Domain.EquipmentPool.Model.Equipment>
                        {
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true" pool="2">
                                    <Equipment slot="Head" id="Item.dadg_simple_hat_b" />
                                    <Equipment slot="Body" id="Item.pleatedjacketfurtrim_green" />
                                    <Equipment slot="Leg" id="Item.hosen_with_shoes_b" />
                                </EquipmentSet>
                                """)),
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true" pool="2">
                                    <Equipment slot="Head" id="Item.dadg_simple_hat_b" />
                                    <Equipment slot="Body" id="Item.pleatedgownfur_red" />
                                    <Equipment slot="Leg"  id="Item.hosen_with_boots_b" />
                                </EquipmentSet>
                                """))
                        }, 2)
                    }
                }
            }));
    }

    [Test]
    public void ReadingBattleEquipmentFromXml_WithInvalidPoolValues_GroupsBattleEquipmentInPoolZero()
    {
        EquipmentRosters equipmentRosters = new EquipmentRosters
        {
            EquipmentRoster = new List<EquipmentRoster>
            {
                new()
                {
                    Id = "vlandian_recruit",
                    Culture = "Culture.empire",
                    EquipmentSet = new List<EquipmentSet>
                    {
                        new()
                        {
                            Pool = "",
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_simple_hat_a" },
                                new() { Slot = "Body", Id = "Item.pleatedjacketfurtrim_red" },
                                new() { Slot = "Leg", Id = "Item.hosen_with_shoes_a" }
                            }
                        },
                        new()
                        {
                            Pool = "invalid_pool",
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_simple_hat_b" },
                                new() { Slot = "Body", Id = "Item.pleatedjacketfurtrim_green" },
                                new() { Slot = "Leg", Id = "Item.hosen_with_shoes_b" }
                            }
                        },
                        new()
                        {
                            Pool = "&",
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_sheperds_hat" },
                                new() { Slot = "Body", Id = "Item.pleatedjacketfurtrim_black" },
                                new() { Slot = "Leg", Id = "Item.hosen_with_boots_a" }
                            }
                        },
                        new()
                        {
                            Pool = "         ",
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_sheperds_hat" },
                                new() { Slot = "Body", Id = "Item.pleatedjacketfurtrim_blue" },
                                new() { Slot = "Leg", Id = "Item.hosen_with_boots_a" }
                            }
                        },
                        new()
                        {
                            Pool = "2",
                            IsCivilian = "true",
                            Equipment = new List<Equipment>
                            {
                                new() { Slot = "Head", Id = "Item.dadg_simple_hat_b" },
                                new() { Slot = "Body", Id = "Item.pleatedgownfur_red" },
                                new() { Slot = "Leg", Id = "Item.hosen_with_boots_b" }
                            }
                        }
                    }
                }
            }
        };
        _equipmentRosterRepository.Setup(repo => repo.GetEquipmentRosters()).Returns(equipmentRosters);

        IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>> allTroopEquipmentPools =
            _equipmentRosterEquipmentPoolsProvider.GetEquipmentPoolsById();

        Assert.That(allTroopEquipmentPools, Is.EquivalentTo(
            new Dictionary<string, List<Domain.EquipmentPool.Model.EquipmentPool>>
            {
                {
                    "vlandian_recruit", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        new(new List<Domain.EquipmentPool.Model.Equipment>
                        {
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true" pool="">
                                    <Equipment slot="Head" id="Item.dadg_simple_hat_a" />
                                    <Equipment slot="Body" id="Item.pleatedjacketfurtrim_red" />
                                    <Equipment slot="Leg" id="Item.hosen_with_shoes_a" />
                                </EquipmentSet>
                                """)),
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true" pool="invalid_pool">
                                    <Equipment slot="Head" id="Item.dadg_simple_hat_b" />
                                    <Equipment slot="Body" id="Item.pleatedjacketfurtrim_green" />
                                    <Equipment slot="Leg" id="Item.hosen_with_shoes_b" />
                                </EquipmentSet>
                                """)),
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true" pool="&amp;">
                                    <Equipment slot="Head" id="Item.dadg_sheperds_hat" />
                                    <Equipment slot="Body" id="Item.pleatedjacketfurtrim_black" />
                                    <Equipment slot="Leg" id="Item.hosen_with_boots_a" />
                                </EquipmentSet>
                                """)),
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true" pool="         ">
                                    <Equipment slot="Head" id="Item.dadg_sheperds_hat" />
                                    <Equipment slot="Body" id="Item.pleatedjacketfurtrim_blue" />
                                    <Equipment slot="Leg" id="Item.hosen_with_boots_a" />
                                </EquipmentSet>
                                """))
                        }, 0),
                        new(new List<Domain.EquipmentPool.Model.Equipment>
                        {
                            new(XDocument.Parse(
                                """
                                <EquipmentSet civilian="true" pool="2">
                                    <Equipment slot="Head" id="Item.dadg_simple_hat_b" />
                                    <Equipment slot="Body" id="Item.pleatedgownfur_red" />
                                    <Equipment slot="Leg" id="Item.hosen_with_boots_b" />
                                </EquipmentSet>
                                """))
                        }, 2)
                    }
                }
            }));
    }
}
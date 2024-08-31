using System.Xml.Linq;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Mappers;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Repositories;

public class NpcCharacterEquipmentPoolRepositoryShould
{
    private Mock<INpcCharacterMapper> _equipmentPoolMapper;
    private Mock<INpcCharacterRepository> _npcCharacterRepository;

    private NpcCharacterEquipmentPoolsProvider _npcCharacterEquipmentPoolsProvider;

    [SetUp]
    public void Setup()
    {
        _equipmentPoolMapper = new Mock<INpcCharacterMapper>();
        _npcCharacterRepository = new Mock<INpcCharacterRepository>();
        _npcCharacterEquipmentPoolsProvider =
            new NpcCharacterEquipmentPoolsProvider(_npcCharacterRepository.Object, _equipmentPoolMapper.Object);
    }

    [Test]
    public void ReadingBattleEquipmentFromXml_WithMultipleTroopsInXml_GroupsBattleEquipmentIntoPools()
    {
        var npcCharacters = new NpcCharacters
        {
            NpcCharacter = new List<NpcCharacter>
            {
                new() { Id = "vlandian_recruit" },
                new() { Id = "vlandian_footman" }
            }
        };

        var firstEquipmentRosters = new List<EquipmentRoster>
        {
            new() { Equipment = new List<Equipment> { new() { Id = "Item.ddg_polearm_longspear", Slot = "Item0" } } },
            new() { Equipment = new List<Equipment> { new() { Id = "Item.livery_coat_over_doublet", Slot = "Body" } } }
        };
        var secondEquipmentRosters = new List<EquipmentRoster>
        {
            new() { Pool = "1", Equipment = new List<Equipment> { new() { Id = "Item.hosen_a", Slot = "Leg" } } },
            new()
            {
                Pool = "0", Equipment = new List<Equipment> { new() { Id = "Item.open_sallet_01e", Slot = "Head" } }
            },
            new()
            {
                Pool = "1", Equipment = new List<Equipment> { new() { Id = "Item.mitten_gauntlets", Slot = "Gloves" } }
            }
        };
        _npcCharacterRepository.Setup(repository => repository.GetNpcCharacters())
            .Returns(npcCharacters);
        _equipmentPoolMapper.Setup(mapper => mapper.MapToEquipmentRosters(npcCharacters.NpcCharacter[0]))
            .Returns(firstEquipmentRosters);
        _equipmentPoolMapper.Setup(mapper => mapper.MapToEquipmentRosters(npcCharacters.NpcCharacter[1]))
            .Returns(secondEquipmentRosters);

        var allTroopEquipmentPools = _npcCharacterEquipmentPoolsProvider.GetEquipmentPoolsById();

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
                                    <EquipmentRoster>
                                    <equipment slot="Item0" id="Item.ddg_polearm_longspear"/>
                                </EquipmentRoster>
                                """)),
                            new(XDocument.Parse(
                                """
                                <EquipmentRoster>
                                    <equipment slot="Body" id="Item.livery_coat_over_doublet"/>
                                </EquipmentRoster>
                                """))
                        }, 0)
                    }
                },
                {
                    "vlandian_footman", new List<Domain.EquipmentPool.Model.EquipmentPool>
                    {
                        new(new List<Domain.EquipmentPool.Model.Equipment>
                        {
                            new(XDocument.Parse(
                                """
                                <EquipmentRoster pool="0">
                                    <equipment slot="Head" id="Item.open_sallet_01e"/>
                                </EquipmentRoster>
                                """))
                        }, 0),
                        new(new List<Domain.EquipmentPool.Model.Equipment>
                        {
                            new(XDocument.Parse(
                                """
                                <EquipmentRoster pool="1">
                                    <equipment slot="Leg" id="Item.hosen_a"/>
                                </EquipmentRoster>
                                """)),
                            new(XDocument.Parse(
                                """
                                <EquipmentRoster pool="1">
                                    <equipment slot="Gloves" id="Item.mitten_gauntlets"/>
                                </EquipmentRoster>
                                """))
                        }, 1)
                    }
                }
            }));
    }

    [Test]
    public void ReadingBattleEquipmentFromXml_WithInvalidPoolValues_GroupsBattleEquipmentInPoolZero()
    {
        var npcCharacters = new NpcCharacters
            { NpcCharacter = new List<NpcCharacter> { new() { Id = "vlandian_recruit" } } };

        var equipmentRosters = new List<EquipmentRoster>
        {
            new() { Pool = "", Equipment = new List<Equipment> { new() { Id = "Item.hosen_a", Slot = "Leg" } } },
            new()
            {
                Pool = "invalid_pool", Equipment = new List<Equipment> { new() { Id = "Item.hosen_b", Slot = "Leg" } }
            },
            new() { Pool = "&", Equipment = new List<Equipment> { new() { Id = "Item.hosen_c", Slot = "Leg" } } },
            new()
            {
                Pool = "         ", Equipment = new List<Equipment> { new() { Id = "Item.hosen_d", Slot = "Leg" } }
            },
            new() { Pool = "1", Equipment = new List<Equipment> { new() { Id = "Item.hosen_e", Slot = "Leg" } } }
        };

        _npcCharacterRepository.Setup(repository => repository.GetNpcCharacters())
            .Returns(npcCharacters);
        _equipmentPoolMapper.Setup(mapper => mapper.MapToEquipmentRosters(npcCharacters.NpcCharacter[0]))
            .Returns(equipmentRosters);

        var allTroopEquipmentPools = _npcCharacterEquipmentPoolsProvider.GetEquipmentPoolsById();

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
                                <EquipmentRoster pool="">
                                    <equipment slot="Leg" id="Item.hosen_a"/>
                                </EquipmentRoster>
                                """)),
                            new(XDocument.Parse(
                                """
                                <EquipmentRoster pool="invalid_pool">
                                    <equipment slot="Leg" id="Item.hosen_b"/>
                                </EquipmentRoster>
                                """)),
                            new(XDocument.Parse(
                                """
                                <EquipmentRoster pool="&amp;">
                                    <equipment slot="Leg" id="Item.hosen_c"/>
                                </EquipmentRoster>
                                """)),
                            new(XDocument.Parse(
                                """
                                    <EquipmentRoster pool="         ">
                                    <equipment slot="Leg" id="Item.hosen_d"/>
                                </EquipmentRoster>
                                """))
                        }, 0),
                        new(new List<Domain.EquipmentPool.Model.Equipment>
                        {
                            new(XDocument.Parse(
                                """
                                <EquipmentRoster pool="1">
                                    <equipment slot="Leg" id="Item.hosen_e"/>
                                </EquipmentRoster>
                                """))
                        }, 1)
                    }
                }
            }));
    }
}
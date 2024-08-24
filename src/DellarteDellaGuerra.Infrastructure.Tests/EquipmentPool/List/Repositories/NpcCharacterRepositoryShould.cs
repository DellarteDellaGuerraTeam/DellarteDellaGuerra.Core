using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Common.Exception;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Repositories;

public class NpcRepositoryRepositoryShould
{
    private Mock<IXmlProcessor> _xmlProcessor;
    private INpcCharacterRepository _npcCharacterRepository;

    [SetUp]
    public void Setup()
    {
        _xmlProcessor = new Mock<IXmlProcessor>();
        _npcCharacterRepository = new NpcCharacterRepository(_xmlProcessor.Object);
    }

    [Test]
    public void GetNpcCharacters()
    {
        string xml =
            """
            <NpcCharacters>
                <NpcCharacter id="irrelevant_character_id_1" culture="irrelevant_culture_id">
                    <Equipments>
                         <EquipmentRoster/>
                        <EquipmentSet/>
                    </Equipments>
                </NpcCharacter>
                <NpcCharacter id="irrelevant_character_id_2" culture="irrelevant_culture_id">
                    <Equipments>
                        <EquipmentRoster pool="irrelevant_pool_id">
                            <equipment slot="irrelevant_slot_1" id="irrelevant_slot_id_1"/>
                            <equipment slot="irrelevant_slot_2" id="irrelevant_slot_id_2"/>
                        </EquipmentRoster>
                        <EquipmentSet pool="irrelevant_pool_id" id="irrelevant_equipment_set_id"
                            civilian="irrelevant_civilian_flag" siege="irrelevant_siege_flag" />
                    </Equipments>
                </NpcCharacter>
            </NpcCharacters>
            """;

        _xmlProcessor.Setup(processor => processor.GetXmlNodes(NpcCharacterRepository.NpcCharacterRootTag))
            .Returns(XDocument.Parse(xml));

        NpcCharacters equipmentRosters = _npcCharacterRepository.GetNpcCharacters();

        Assert.That(equipmentRosters, Is.EqualTo(new NpcCharacters
        {
            NpcCharacter = new List<NpcCharacter>
            {
                new()
                {
                    Id = "irrelevant_character_id_1",
                    Equipments = new Equipments
                    {
                        EquipmentRoster = new List<EquipmentRoster>
                        {
                            new()
                        },
                        EquipmentSet = new List<EquipmentSet>
                        {
                            new()
                        }
                    }
                },
                new()
                {
                    Id = "irrelevant_character_id_2",
                    Equipments = new Equipments
                    {
                        EquipmentRoster = new List<EquipmentRoster>
                        {
                            new()
                            {
                                Pool = "irrelevant_pool_id",
                                Equipment = new List<Equipment>
                                {
                                    new() { Slot = "irrelevant_slot_1", Id = "irrelevant_slot_id_1" },
                                    new() { Slot = "irrelevant_slot_2", Id = "irrelevant_slot_id_2" }
                                }
                            }
                        },
                        EquipmentSet = new List<EquipmentSet>
                        {
                            new()
                            {
                                Pool = "irrelevant_pool_id",
                                Id = "irrelevant_equipment_set_id",
                                IsCivilian = "irrelevant_civilian_flag",
                                IsSiege = "irrelevant_siege_flag"
                            }
                        }
                    }
                }
            }
        }));
    }

    [Test]
    public void ThrowsTechnicalException_WhenIOErrorOccurs()
    {
        _xmlProcessor
            .Setup(processor => processor.GetXmlNodes(NpcCharacterRepository.NpcCharacterRootTag))
            .Throws(new IOException());

        Exception? ex = Assert.Throws<TechnicalException>(() => _npcCharacterRepository.GetNpcCharacters());
        Assert.That(ex?.Message, Is.EqualTo(NpcCharacterRepository.IoErrorMessage));
    }

    [Test]
    public void ThrowsTechnicalException_WhenNpcCharactersAreNotDefinedCorrectly()
    {
        _xmlProcessor
            .Setup(processor => processor.GetXmlNodes(NpcCharacterRepository.NpcCharacterRootTag))
            .Returns(XDocument.Parse("<InvalidRootTag/>"));

        Exception? ex = Assert.Throws<TechnicalException>(() => _npcCharacterRepository.GetNpcCharacters());
        Assert.That(ex?.Message, Is.EqualTo(NpcCharacterRepository.DeserialisationErrorMessage));
    }
}
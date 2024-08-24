using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Common.Exception;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Repositories;

public class EquipmentRosterRepositoryShould
{
    private Mock<IXmlProcessor> _xmlProcessor;
    private IEquipmentRosterRepository _equipmentRosterRepository;

    [SetUp]
    public void Setup()
    {
        _xmlProcessor = new Mock<IXmlProcessor>();
        _equipmentRosterRepository = new EquipmentRosterRepository(_xmlProcessor.Object);
    }

    [Test]
    public void GetEquipmentRosters()
    {
        string xml =
            """
            <EquipmentRosters>
                <EquipmentRoster id="irrelevant_equipment_roster_id" culture="irrelevant_culture_id">
                    <EquipmentSet civilian="irrelevant_civilian_flag" siege="irrelevant_siege_flag" pool="irrelevant_pool_id">
                        <Equipment slot="irrelevant_slot_1" id="irrelevant_slot_id_1"/>
                        <Equipment slot="irrelevant_slot_2" id="irrelevant_slot_id_2"/>
                    </EquipmentSet>
                    <EquipmentSet/>
                </EquipmentRoster>
                <EquipmentRoster id="irrelevant_empty_equipment_roster_id"/>
            </EquipmentRosters>
            """;

        _xmlProcessor.Setup(processor => processor.GetXmlNodes(EquipmentRosterRepository.EquipmentRostersRootTag))
            .Returns(XDocument.Parse(xml));

        EquipmentRosters equipmentRosters = _equipmentRosterRepository.GetEquipmentRosters();

        Assert.That(equipmentRosters, Is.EqualTo(new EquipmentRosters
        {
            EquipmentRoster = new List<EquipmentRoster>
            {
                new()
                {
                    Id = "irrelevant_equipment_roster_id",
                    Culture = "irrelevant_culture_id",
                    EquipmentSet = new List<EquipmentSet>
                    {
                        new()
                        {
                            Equipment = new List<Equipment>
                            {
                                new()
                                {
                                    Slot = "irrelevant_slot_1",
                                    Id = "irrelevant_slot_id_1"
                                },
                                new()
                                {
                                    Slot = "irrelevant_slot_2",
                                    Id = "irrelevant_slot_id_2"
                                }
                            },
                            Pool = "irrelevant_pool_id",
                            IsCivilian = "irrelevant_civilian_flag",
                            IsSiege = "irrelevant_siege_flag"
                        },
                        new()
                    }
                },
                new()
                {
                    Id = "irrelevant_empty_equipment_roster_id"
                }
            }
        }));
    }

    [Test]
    public void ThrowsTechnicalException_WhenIOErrorOccurs()
    {
        _xmlProcessor
            .Setup(processor => processor.GetXmlNodes(EquipmentRosterRepository.EquipmentRostersRootTag))
            .Throws(new IOException());

        Exception? ex = Assert.Throws<TechnicalException>(() => _equipmentRosterRepository.GetEquipmentRosters());
        Assert.That(ex?.Message, Is.EqualTo(EquipmentRosterRepository.IoErrorMessage));
    }

    [Test]
    public void ThrowsTechnicalException_WhenEquipmentRostersAreNotDefinedCorrectly()
    {
        _xmlProcessor
            .Setup(processor => processor.GetXmlNodes(EquipmentRosterRepository.EquipmentRostersRootTag))
            .Returns(XDocument.Parse("<InvalidRootTag/>"));

        Exception? ex = Assert.Throws<TechnicalException>(() => _equipmentRosterRepository.GetEquipmentRosters());
        Assert.That(ex?.Message, Is.EqualTo(EquipmentRosterRepository.DeserialisationErrorMessage));
    }
}
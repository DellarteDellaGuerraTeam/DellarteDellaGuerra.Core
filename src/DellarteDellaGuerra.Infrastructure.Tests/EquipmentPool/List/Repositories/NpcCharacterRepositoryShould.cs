using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Common.Exception;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Caching;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Infrastructure.Tests.EquipmentPool.List.Repositories;

public class NpcRepositoryRepositoryShould
{
    private const string CachedObjectId = "irrelevant_cached_object_id";
    
    private Mock<IXmlProcessor> _xmlProcessor;
    private Mock<ICacheProvider> _cacheProvider;
    private Mock<ILoggerFactory> _loggerFactory;
    private INpcCharacterRepository _npcCharacterRepository;

    [SetUp]
    public void Setup()
    {
        _xmlProcessor = new Mock<IXmlProcessor>(MockBehavior.Strict);
        _cacheProvider = new Mock<ICacheProvider>(MockBehavior.Strict);
        _loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
        _loggerFactory.Setup(factory => factory.CreateLogger<INpcCharacterRepository>())
            .Returns(new Mock<ILogger>(MockBehavior.Strict).Object);
        _npcCharacterRepository =
            new NpcCharacterRepository(_xmlProcessor.Object, _cacheProvider.Object, _loggerFactory.Object);
    }

    [Test]
    public void GetNpcCharacters()
    {
        string xml =
            """
            <NPCCharacters>
                <NPCCharacter id="irrelevant_character_id_1" culture="irrelevant_culture_id">
                    <Equipments>
                         <EquipmentRoster/>
                        <EquipmentSet/>
                    </Equipments>
                </NPCCharacter>
                <NPCCharacter id="irrelevant_character_id_2" culture="irrelevant_culture_id">
                    <Equipments>
                        <EquipmentRoster pool="irrelevant_pool_id">
                            <equipment slot="irrelevant_slot_1" id="irrelevant_slot_id_1"/>
                            <equipment slot="irrelevant_slot_2" id="irrelevant_slot_id_2"/>
                        </EquipmentRoster>
                        <EquipmentSet pool="irrelevant_pool_id" id="irrelevant_equipment_set_id"
                            civilian="irrelevant_civilian_flag" siege="irrelevant_siege_flag" />
                    </Equipments>
                </NPCCharacter>
            </NPCCharacters>
            """;

        _xmlProcessor.Setup(processor => processor.GetXmlNodes(NpcCharacterRepository.NpcCharacterRootTag))
            .Returns(XDocument.Parse(xml));
        _cacheProvider.Setup(cache => cache.CacheObject(It.IsAny<NpcCharacters>()))
            .Returns(CachedObjectId);
        _cacheProvider.Setup(cache => cache.InvalidateCache(CachedObjectId, CampaignEvent.OnAfterSessionLaunched));

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
    public void GetCachedNpcCharacters()
    {
        string xml = "<NPCCharacters/>";

        _xmlProcessor.Setup(processor => processor.GetXmlNodes(NpcCharacterRepository.NpcCharacterRootTag))
            .Returns(XDocument.Parse(xml));
        _cacheProvider.Setup(cache => cache.CacheObject(It.IsAny<NpcCharacters>()))
            .Returns(CachedObjectId);
        _cacheProvider.Setup(cache => cache.InvalidateCache(CachedObjectId, CampaignEvent.OnAfterSessionLaunched));

        // First call to populate cache
        var equipmentRosters = _npcCharacterRepository.GetNpcCharacters();

        _cacheProvider.Verify(provider => provider.CacheObject(equipmentRosters), Times.Once);
        _cacheProvider.Setup(cache => cache.GetCachedObject<NpcCharacters>(CachedObjectId))
            .Returns(new NpcCharacters());

        NpcCharacters cachedEquipmentRosters = _npcCharacterRepository.GetNpcCharacters();

        _cacheProvider.VerifyAll();
        Assert.That(cachedEquipmentRosters, Is.EqualTo(equipmentRosters));
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
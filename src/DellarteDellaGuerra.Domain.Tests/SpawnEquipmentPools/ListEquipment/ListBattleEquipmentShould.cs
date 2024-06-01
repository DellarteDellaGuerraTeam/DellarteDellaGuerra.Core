using System.Xml.Linq;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Model;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Port;
using Moq;
using NUnit.Framework;

namespace DellarteDellaGuerra.Domain.Tests.SpawnEquipmentPools.ListEquipment
{
    public class ListBattleEquipmentShould
    {
        [Test]
        public void GetCharacterEquipment()
        {
            var equipmentRepository = EquipmentRepositoryReturns(new Dictionary<string, IList<EquipmentPool>>()
            {
                {
                    "Character1", new List<EquipmentPool>()
                    {
                        CreateEquipmentPool(new[] { "Equipment1", "Equipment2" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<EquipmentPool>()
                    {
                        CreateEquipmentPool(new[] { "Equipment5", "Equipment6" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment7" }, 1),
                    }
                }
            });
            var siegeEquipmentRepository =
                SiegeEquipmentRepositoryReturns(new Dictionary<string, IList<EquipmentPool>>());
            var civilianEquipmentRepository =
                CivilianEquipmentRepositoryReturns(new Dictionary<string, IList<EquipmentPool>>());
            var listBattleEquipment = new ListBattleEquipment(equipmentRepository, siegeEquipmentRepository,
                civilianEquipmentRepository);

            var characterBattleEquipment = listBattleEquipment.ListEquipment();

            Assert.That(characterBattleEquipment, Is.EqualTo(new Dictionary<string, IList<EquipmentPool>>()
            {
                {
                    "Character1", new List<EquipmentPool>()
                    {
                        CreateEquipmentPool(new[] { "Equipment1", "Equipment2" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<EquipmentPool>()
                    {
                        CreateEquipmentPool(new[] { "Equipment5", "Equipment6" }, 0),
                        CreateEquipmentPool(new[] { "Equipment3", "Equipment7" }, 1),
                    }
                }
            }));
        }

        [Test]
        public void NotGetCivilianCharacterEquipment()
        {
            var equipmentRepository = EquipmentRepositoryReturns(new Dictionary<string, IList<EquipmentPool>>()
            {
                {
                    "Character1", new List<EquipmentPool>()
                    {
                        CreateEquipmentPool(new [] { "CivilianEquipment1", "CivilianEquipment2" }, 0),
                        CreateEquipmentPool(new [] { "CivilianEquipment3", "CivilianEquipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<EquipmentPool>()
                    {
                        CreateEquipmentPool(new [] { "CivilianEquipment5", "CivilianEquipment6" }, 0),
                        CreateEquipmentPool(new [] { "Equipment3", "CivilianEquipment7" }, 1),
                        CreateEquipmentPool(new [] { "Equipment3", "CivilianEquipment7" }, 2),
                    }
                }
            });
            var siegeEquipmentRepository =
                SiegeEquipmentRepositoryReturns(new Dictionary<string, IList<EquipmentPool>>());
            var civilianEquipmentRepository =
                CivilianEquipmentRepositoryReturns(new Dictionary<string, IList<EquipmentPool>>()
                {
                    {
                        "Character1", new List<EquipmentPool>()
                        {
                            CreateEquipmentPool(new [] { "CivilianEquipment1", "CivilianEquipment2" }, 0),
                            CreateEquipmentPool(new [] { "CivilianEquipment3", "CivilianEquipment4" }, 1)
                        }
                    },
                    {
                        "Character2", new List<EquipmentPool>()
                        {
                            CreateEquipmentPool(new [] { "CivilianEquipment5", "CivilianEquipment6" }, 0),
                            CreateEquipmentPool(new [] { "CivilianEquipment7" }, 1)
                        }
                    }
                });
            var listBattleEquipment = new ListBattleEquipment(equipmentRepository, siegeEquipmentRepository,
                civilianEquipmentRepository);

            var characterBattleEquipment = listBattleEquipment.ListEquipment();

            Assert.That(characterBattleEquipment, Is.EqualTo(new Dictionary<string, IList<EquipmentPool>>()
                {
                    {
                        "Character1", new List<EquipmentPool>()
                    },
                    {
                        "Character2", new List<EquipmentPool>()
                        {
                            CreateEquipmentPool(new [] { "Equipment3" }, 1),
                            CreateEquipmentPool(new [] { "Equipment3", "CivilianEquipment7" }, 2),
                        }
                    }
                }));
        }

        [Test]
        public void NotGetSiegeCharacterEquipment()
        {
            var equipmentRepository = EquipmentRepositoryReturns(new Dictionary<string, IList<EquipmentPool>>()
            {
                {
                    "Character1", new List<EquipmentPool>()
                    {
                        CreateEquipmentPool(new [] { "SiegeEquipment1", "SiegeEquipment2" }, 0),
                        CreateEquipmentPool(new [] { "SiegeEquipment3", "SiegeEquipment4" }, 1)
                    }
                },
                {
                    "Character2", new List<EquipmentPool>()
                    {
                        CreateEquipmentPool(new [] { "SiegeEquipment5", "SiegeEquipment6" }, 0),
                        CreateEquipmentPool(new [] { "Equipment3", "SiegeEquipment7" }, 1),
                        CreateEquipmentPool(new [] { "Equipment3", "SiegeEquipment7" }, 2),
                    }
                }
            });
            var siegeEquipmentRepository =
                SiegeEquipmentRepositoryReturns(new Dictionary<string, IList<EquipmentPool>>()
                {
                    {
                        "Character1", new List<EquipmentPool>()
                        {
                            CreateEquipmentPool(new [] { "SiegeEquipment1", "SiegeEquipment2" }, 0),
                            CreateEquipmentPool(new [] { "SiegeEquipment3", "SiegeEquipment4" }, 1)
                        }
                    },
                    {
                        "Character2", new List<EquipmentPool>()
                        {
                            CreateEquipmentPool(new [] { "SiegeEquipment5", "SiegeEquipment6" }, 0),
                            CreateEquipmentPool(new [] { "SiegeEquipment7" }, 1),
                        }
                    }
                });
            var civilianEquipmentRepository =
                CivilianEquipmentRepositoryReturns(new Dictionary<string, IList<EquipmentPool>>());            
            var listBattleEquipment = new ListBattleEquipment(equipmentRepository, siegeEquipmentRepository,
                civilianEquipmentRepository);

            var characterBattleEquipment = listBattleEquipment.ListEquipment();

            Assert.That(characterBattleEquipment, Is.EqualTo(new Dictionary<string, IList<EquipmentPool>>()
                {
                    {
                        "Character1", new List<EquipmentPool>()
                    },
                    {
                        "Character2", new List<EquipmentPool>()
                        {
                            CreateEquipmentPool(new [] { "Equipment3" }, 1),
                            CreateEquipmentPool(new [] { "Equipment3", "SiegeEquipment7" }, 2),
                        }
                    }
                })
            );
        }

        private ICharacterEquipmentRepository EquipmentRepositoryReturns(
            IDictionary<string, IList<EquipmentPool>> equipmentPoolsByCharacter)
        {
            var equipmentRepository = new Mock<ICharacterEquipmentRepository>();
            equipmentRepository.Setup(repo => repo.GetEquipmentPoolsByCharacter()).Returns(equipmentPoolsByCharacter);
            return equipmentRepository.Object;
        }

        private ICharacterCivilianEquipmentRepository CivilianEquipmentRepositoryReturns(
            IDictionary<string, IList<EquipmentPool>> equipmentPoolsByCharacter)
        {
            var equipmentRepository = new Mock<ICharacterCivilianEquipmentRepository>();
            equipmentRepository.Setup(repo => repo.GetCivilianEquipmentByCharacterAndPool()).Returns(equipmentPoolsByCharacter);
            return equipmentRepository.Object;
        }

        private ICharacterSiegeEquipmentRepository SiegeEquipmentRepositoryReturns(
            IDictionary<string, IList<EquipmentPool>> equipmentPoolsByCharacter)
        {
            var equipmentRepository = new Mock<ICharacterSiegeEquipmentRepository>();
            equipmentRepository.Setup(repo => repo.GetSiegeEquipmentByCharacterAndPool()).Returns(equipmentPoolsByCharacter);
            return equipmentRepository.Object;
        }

        private EquipmentPool CreateEquipmentPool(string[] equipmentIds, int poolId)
        {
            var equipment = equipmentIds.Select(equipmentId => CreateEquipment(equipmentId)).ToList();
            return new EquipmentPool(equipment, poolId);
        }

        private Equipment CreateEquipment(string id)
        {
            string xml = $"<EquipmentRoster id=\"{id}\">\n" +
                         "<equipment slot=\"Item0\" id=\"Item.ddg_polearm_longspear2\"/>\n" +
                         "<equipment slot=\"Body\" id=\"Item.jack_sleeveless_with_splints2\"/>\n" +
                         "<equipment slot=\"Leg\" id=\"Item.hosen_with_boots_c2\"/>\n" +
                         "<equipment slot=\"Head\" id=\"Item.war_hat2\"/>\n" +
                         "</EquipmentRoster>";

            return new Equipment(XDocument.Parse(xml).Root!);
        }
    }
}

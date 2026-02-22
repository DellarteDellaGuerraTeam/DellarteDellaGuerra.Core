using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Model;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Port;
using Xunit;
using DomainEquipment = DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Model.Equipment;

namespace DellarteDellaGuerra.Domain.Tests.Tournament.Jousting.Equipment
{
    public class GetJoustEquipmentUtilTests
    {
        private sealed class FakeTemplatesRepository : IJoustingEquipmentTemplatesRepository
        {
            private readonly IReadOnlyList<IReadOnlyList<EquipmentSlot>> _templates;

            public FakeTemplatesRepository(IReadOnlyList<IReadOnlyList<EquipmentSlot>> templates)
            {
                _templates = templates;
            }

            public IReadOnlyList<IReadOnlyList<EquipmentSlot>> GetJoustingEquipmentTemplates() => _templates;
        }

        private sealed class FakeRandom : Random
        {
            private readonly int _value;

            public FakeRandom(int value)
            {
                _value = value;
            }

            public override int Next(int maxValue) => _value % maxValue;
        }

		[Fact]
		public void GetJoustEquipment_WhenNoTemplates_ReturnsOriginalInstance()
		{
			var originalSlots = new List<EquipmentSlot>
			{
				new(0, "orig-0"),
				new(1, "orig-1"),
				new(2, "orig-2"),
			};

			var original = new DomainEquipment(originalSlots);

            var repo = new FakeTemplatesRepository(Array.Empty<IReadOnlyList<EquipmentSlot>>());
            var random = new FakeRandom(0);
            var util = new GetJoustEquipmentUtil(repo, random);

            var result = util.GetJoustEquipment(original);

            // When there are no templates, the domain must not create a new instance
            // or alter the equipment. It simply uses the original.
            Assert.Same(original, result);
        }

        [Fact]
        public void GetJoustEquipment_OverridesNonArmorSlotsAndPreservesArmor()
        {
            // Domain convention for this test:
            // 0-3: weapons, 4: shield, 5-9: armor, 10: horse, 11: harness
            var originalSlots = new List<EquipmentSlot>();
            for (var slotId = 0; slotId <= 11; slotId++)
            {
                originalSlots.Add(new EquipmentSlot(slotId, $"orig-{slotId}"));
            }

			var original = new DomainEquipment(originalSlots);

            var template = new List<EquipmentSlot>
            {
                new(0, "templ-weapon0"),
                new(1, "templ-weapon1"),
                new(10, "templ-horse"),
                new(11, "templ-harness"),
            };

            var repo = new FakeTemplatesRepository(new[]
            {
                (IReadOnlyList<EquipmentSlot>)template,
            });

            var random = new FakeRandom(0);
            var util = new GetJoustEquipmentUtil(repo, random);

            var result = util.GetJoustEquipment(original);

            // Same number of slots
            Assert.Equal(original.EquipmentSlots.Count, result.EquipmentSlots.Count);

            // Non-armor slots overridden by template.
            Assert.Equal("templ-weapon0", result.EquipmentSlots[0].EquipmentId);
            Assert.Equal("templ-weapon1", result.EquipmentSlots[1].EquipmentId);
            Assert.Equal("templ-horse", result.EquipmentSlots[10].EquipmentId);
            Assert.Equal("templ-harness", result.EquipmentSlots[11].EquipmentId);

            // Armor slots (5-9) preserved from original.
            for (var slotId = 5; slotId <= 9; slotId++)
            {
                Assert.Equal($"orig-{slotId}", result.EquipmentSlots[slotId].EquipmentId);
            }
        }
    }
}

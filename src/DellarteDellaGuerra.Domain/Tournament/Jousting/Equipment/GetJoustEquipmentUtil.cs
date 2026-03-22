using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Model;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Port;

namespace DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment
{
	public class GetJoustEquipmentUtil : IGetJoustEquipmentUtil
	{
		private readonly IJoustingEquipmentTemplatesRepository _templatesRepository;
		private readonly Random _random;

		public GetJoustEquipmentUtil(IJoustingEquipmentTemplatesRepository templatesRepository, Random? random = null)
		{
			_templatesRepository = templatesRepository;
			_random = random ?? new Random();
		}

		public Model.Equipment GetJoustEquipment(Model.Equipment originalEquipment)
		{
			var templates = _templatesRepository.GetJoustingEquipmentTemplates();
			if (templates == null || templates.Count == 0)
				return originalEquipment;

			// Pick a template at random.
			var templateIndex = _random.Next(templates.Count);
			var selectedTemplate = templates[templateIndex];

			// Clone original slots into a mutable list so we can overlay the template.
			var resultSlots = new List<EquipmentSlot>(originalEquipment.EquipmentSlots.Count);
			foreach (var slot in originalEquipment.EquipmentSlots)
			{
				resultSlots.Add(new EquipmentSlot(slot.SlotId, slot.EquipmentId));
			}

			// Overlay non-armor slots from the template.
			foreach (var templateSlot in selectedTemplate)
			{
				if (IsArmorSlot(templateSlot.SlotId))
					continue;

				var index = templateSlot.SlotId;
				if (index < 0 || index >= resultSlots.Count)
					continue;

				resultSlots[index] = templateSlot;
			}

			return new Model.Equipment(resultSlots);
		}

		private static bool IsArmorSlot(int slotId)
		{
			return slotId >= JoustEquipmentSlots.FirstArmorSlot && slotId <= JoustEquipmentSlots.LastArmorSlot;
		}
	}
}

using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Model;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Tournament.Jousting.Equipment.Spi
{
	using DomainEquipment = DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Model.Equipment;
	using NativeEquipment = TaleWorlds.Core.Equipment;

	/// <summary>
	/// Mapper that converts between the domain <see cref="DomainEquipment"/> and the native
	/// <see cref="TaleWorlds.Core.Equipment"/> used by Bannerlord.
	/// </summary>
	public class EquipmentMapper : IEquipmentMapper
	{
		private readonly EquipmentSlotMapper _slotMapper;

		public EquipmentMapper(EquipmentSlotMapper slotMapper)
		{
			_slotMapper = slotMapper;
		}

		public NativeEquipment ToNative(DomainEquipment domainEquipment)
		{
			// Re‑use the existing slot‑level mapper which already knows how to map a list of slots.
			return _slotMapper.Map(domainEquipment.EquipmentSlots);
		}

		public DomainEquipment ToDomain(NativeEquipment nativeEquipment)
		{
			// Build a dense list of domain slots from 0..maxDomainSlotId so that
			// slot index == domain slot id for all entries.
			var maxDomainSlotId = GetMaxDomainSlotId();
			var slots = new List<EquipmentSlot>(maxDomainSlotId + 1);
			for (var i = 0; i <= maxDomainSlotId; i++)
				slots.Add(new EquipmentSlot(i, null));

			foreach (EquipmentIndex nativeIndex in Enum.GetValues(typeof(EquipmentIndex)))
			{
				var domainSlotId = MapNativeIndexToDomainSlot(nativeIndex);
				if (domainSlotId is null)
					continue;

				var element = nativeEquipment[nativeIndex];
				var equipmentId = element.Item?.StringId;
				slots[domainSlotId.Value] = new EquipmentSlot(domainSlotId.Value, equipmentId);
			}

			return new DomainEquipment(slots);
		}

		private static int GetMaxDomainSlotId()
		{
			var max = -1;
			foreach (EquipmentIndex nativeIndex in Enum.GetValues(typeof(EquipmentIndex)))
			{
				var slotId = MapNativeIndexToDomainSlot(nativeIndex);
				if (slotId.HasValue && slotId.Value > max)
					max = slotId.Value;
			}

			return max;
		}

		private static int? MapNativeIndexToDomainSlot(EquipmentIndex index)
		{
			return index switch
			{
				EquipmentIndex.Weapon0 => 0,
				EquipmentIndex.Weapon1 => 1,
				EquipmentIndex.Weapon2 => 2,
				EquipmentIndex.Weapon3 => 3,
				EquipmentIndex.Head => 5,
				EquipmentIndex.Body => 6,
				EquipmentIndex.Leg => 7,
				EquipmentIndex.Gloves => 8,
				EquipmentIndex.Cape => 9,
				EquipmentIndex.Horse => 10,
				EquipmentIndex.HorseHarness => 11,
				_ => null
			};
		}
	}
}

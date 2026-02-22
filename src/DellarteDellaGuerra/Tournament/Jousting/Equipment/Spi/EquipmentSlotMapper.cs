using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Model;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Tournament.Jousting.Equipment.Spi
{
	public class EquipmentSlotMapper
	{
		private readonly MBObjectManager _mbObjectManager;

        public EquipmentSlotMapper(MBObjectManager mbObjectManager)
        {
            _mbObjectManager = mbObjectManager;
        }

		public TaleWorlds.Core.Equipment Map(List<EquipmentSlot> equipmentSlots)
		{
			var equipment = new TaleWorlds.Core.Equipment();

			foreach (var equipmentSlot in equipmentSlots)
			{
				var nativeIndex = MapDomainSlotToNativeIndex(equipmentSlot.SlotId);
				if (nativeIndex is null)
					continue;

				if (equipmentSlot.EquipmentId is null)
				{
					equipment[nativeIndex.Value] = new EquipmentElement();
					continue;
				}

				var item = _mbObjectManager.GetObject<ItemObject>(equipmentSlot.EquipmentId);
				if (item is null)
					// TODO: log error when an equipment id cannot be resolved
					continue;
				equipment[nativeIndex.Value] = new EquipmentElement(item);
			}

			return equipment;
		}

		private static EquipmentIndex? MapDomainSlotToNativeIndex(int slotId)
		{
			return slotId switch
			{
				0 => EquipmentIndex.Weapon0,
				1 => EquipmentIndex.Weapon1,
				2 => EquipmentIndex.Weapon2,
				3 => EquipmentIndex.Weapon3,
				5 => EquipmentIndex.Head,
				6 => EquipmentIndex.Body,
				7 => EquipmentIndex.Leg,
				8 => EquipmentIndex.Gloves,
				9 => EquipmentIndex.Cape,
				10 => EquipmentIndex.Horse,
				11 => EquipmentIndex.HorseHarness,
				_ => null
			};
		}
	}
}

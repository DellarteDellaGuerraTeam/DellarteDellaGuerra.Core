using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Model;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Tournament.Jousting.Equipment.Spi
{
    public class JoustingEquipmentTemplatesRepository : IJoustingEquipmentTemplatesRepository
    {
        private const string NpcCharacterId = "jousting-template";

        private readonly MBObjectManager _objectManager;

		public JoustingEquipmentTemplatesRepository(MBObjectManager objectManager)
		{
			_objectManager = objectManager;
		}
		
		public IReadOnlyList<IReadOnlyList<EquipmentSlot>> GetJoustingEquipmentTemplates()
		{
			var characterObject = _objectManager.GetObject<CharacterObject>(NpcCharacterId);
			
			if (characterObject is null) return new List<IReadOnlyList<EquipmentSlot>>();
			
			var result = new List<IReadOnlyList<EquipmentSlot>>();
			foreach (var equipment in characterObject.AllEquipments)
			{
				var equipmentSlots = new List<EquipmentSlot>();
				
				// Weapon slots
				for (var index = EquipmentIndex.WeaponItemBeginSlot;
				     index < EquipmentIndex.NumAllWeaponSlots;
				     index++)
				{
					var domainSlotId = MapNativeIndexToDomainSlot(index);
					if (domainSlotId is null)
						continue;
					
					var item = equipment[index].Item;
					var equipmentId = item?.StringId;
					equipmentSlots.Add(new EquipmentSlot(domainSlotId.Value, equipmentId));
				}
				
				// Horse & harness
				var horseSlotId = MapNativeIndexToDomainSlot(EquipmentIndex.Horse);
				if (horseSlotId is not null)
				{
					var horseItem = equipment[EquipmentIndex.Horse].Item;
					var horseId = horseItem?.StringId;
					equipmentSlots.Add(new EquipmentSlot(horseSlotId.Value, horseId));
				}
				
				var harnessSlotId = MapNativeIndexToDomainSlot(EquipmentIndex.HorseHarness);
				if (harnessSlotId is not null)
				{
					var harnessItem = equipment[EquipmentIndex.HorseHarness].Item;
					var harnessId = harnessItem?.StringId;
					equipmentSlots.Add(new EquipmentSlot(harnessSlotId.Value, harnessId));
				}
				
				result.Add(equipmentSlots);
			}
			
			return result;
		}
		
		private static int? MapNativeIndexToDomainSlot(EquipmentIndex index)
		{
			return index switch
			{
				EquipmentIndex.Weapon0 => 0,
				EquipmentIndex.Weapon1 => 1,
				EquipmentIndex.Weapon2 => 2,
				EquipmentIndex.Weapon3 => 3,
				EquipmentIndex.Horse => 10,
				EquipmentIndex.HorseHarness => 11,
				_ => null
			};
		}
	}
}

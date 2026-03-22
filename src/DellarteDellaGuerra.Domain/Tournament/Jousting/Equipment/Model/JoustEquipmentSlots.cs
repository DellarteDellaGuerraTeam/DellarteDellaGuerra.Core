namespace DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Model
{
	/// <summary>
	/// Domain-level convention for jousting equipment slot ids.
	/// These are intentionally simple integers so the domain stays
	/// independent of TaleWorlds.Core.EquipmentIndex.
	/// </summary>
	public static class JoustEquipmentSlots
	{
		// Armor slots are treated specially: they are preserved from the
		// original equipment when building jousting loadouts.
		public const int FirstArmorSlot = 5;
		public const int LastArmorSlot = 9;
	}
}


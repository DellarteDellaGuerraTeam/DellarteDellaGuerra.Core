namespace DellarteDellaGuerra.Tournament.Jousting.Equipment.Spi
{
	using DomainEquipment = DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Model.Equipment;
	using NativeEquipment = TaleWorlds.Core.Equipment;

	public interface IEquipmentMapper
	{
		NativeEquipment ToNative(DomainEquipment domainEquipment);
		DomainEquipment ToDomain(NativeEquipment nativeEquipment);
	}
}


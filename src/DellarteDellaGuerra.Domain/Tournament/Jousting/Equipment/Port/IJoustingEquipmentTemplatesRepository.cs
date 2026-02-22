using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Model;

namespace DellarteDellaGuerra.Domain.Tournament.Jousting.Equipment.Port
{
	public interface IJoustingEquipmentTemplatesRepository
	{
		IReadOnlyList<IReadOnlyList<EquipmentSlot>> GetJoustingEquipmentTemplates();
	}
}

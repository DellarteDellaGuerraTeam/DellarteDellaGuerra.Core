using DellarteDellaGuerra.Domain.Equipment.Get.Model;

namespace DellarteDellaGuerra.Domain.Equipment.Get.Port
{
    public interface IEncounterTypeProvider
    {
        EncounterType GetEncounterType();
    }
}
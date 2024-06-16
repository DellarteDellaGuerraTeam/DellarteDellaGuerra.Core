using DellarteDellaGuerra.Domain.Equipment.Get.Model;
using DellarteDellaGuerra.Domain.Equipment.Get.Port;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Equipment.Get.Providers
{
    public class EncounterTypeProvider : IEncounterTypeProvider
    {
        public EncounterType GetEncounterType()
        {
            var currentMission = Mission.Current;
            if (currentMission == null) return EncounterType.None;

            if (currentMission.IsSiegeBattle || currentMission.IsSallyOutBattle) return EncounterType.Siege;
            if (currentMission.IsFriendlyMission) return EncounterType.Civilian;
            return EncounterType.Battle;
        }
    }
}
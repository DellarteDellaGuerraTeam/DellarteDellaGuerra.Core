using DellarteDellaGuerra.Domain.Equipment.List.Model;
using DellarteDellaGuerra.Domain.Equipment.List.Port;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Equipment.List.Providers
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
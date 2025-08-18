using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Cannon;

public class CannonMissionBehaviour : MissionLogic
{
    public override void OnAddTeam(TaleWorlds.MountAndBlade.Team team)
    {
        base.OnAddTeam(team);
        Mission.Current.ActiveMissionObjects
            .Select(missionObject => missionObject.GameEntity.GetFirstScriptOfTypeInFamily<Falconet>())
            .Where(script => script is not null && script.Side.Equals(BattleSideEnum.Attacker) && team.IsAttacker)
            .ToList()
            .ForEach(script => script.Team = team);

        Mission.Current.ActiveMissionObjects
            .Select(missionObject => missionObject.GameEntity.GetFirstScriptOfTypeInFamily<Falconet>())
            .Where(script => script is not null && script.Side.Equals(BattleSideEnum.Defender) && team.IsDefender)
            .ToList()
            .ForEach(script => script.Team = team);
    }
}
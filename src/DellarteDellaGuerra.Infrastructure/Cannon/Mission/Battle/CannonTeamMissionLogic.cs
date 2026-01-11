using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Cannon;

public class CannonTeamMissionLogic : MissionLogic
{
    public override void OnAddTeam(TaleWorlds.MountAndBlade.Team team)
    {
        base.OnAddTeam(team);

        Mission.Current.ActiveMissionObjects
            .OfType<Falconet>()
            .Where(script => script.Side.Equals(BattleSideEnum.Attacker) && team.IsAttacker)
            .ToList()
            .ForEach(script =>
            {
                script.Team = team;
                script.SetForcedUse(true);
            });
        
        Mission.Current.ActiveMissionObjects
            .OfType<Falconet>()
            .Where(script => script.Side.Equals(BattleSideEnum.Defender) && team.IsDefender)
            .ToList()
            .ForEach(script =>
            {
                script.Team = team;
                script.SetForcedUse(true);
            });
    }
}
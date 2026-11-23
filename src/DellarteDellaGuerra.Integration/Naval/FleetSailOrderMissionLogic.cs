using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.Naval;

internal sealed class FleetSailOrderMissionLogic : MissionLogic
{
    private bool _showedHint;

    public override void OnBehaviorInitialize() => OarlessShipAI.Install();

    public override void OnMissionTick(float dt)
    {
        if (!Mission.IsOrderMenuOpen || !Mission.IsDeploymentFinished || Mission.IsMissionEnding ||
            Mission.MainAgent?.IsActive() != true) return;
        var team = Mission.PlayerTeam;
        if (team == null || (!team.IsPlayerGeneral && !team.IsPlayerSergeant)) return;
        var pressed = Input.IsKeyDown(InputKey.LeftControl) && Input.IsKeyPressed(InputKey.M);
        if (_showedHint && !pressed) return;

        var orders = team.PlayerOrderController;
        var selected = new List<MissionShip>();
        foreach (var ship in Mission.GetMissionBehavior<NavalShipsLogic>().AllShips)
        {
            if (ship.Team == team && ship.Formation != null && OarlessShipAI.IsOarless(ship) &&
                orders.SelectedFormations.Contains(ship.Formation) && orders.IsFormationSelectable(ship.Formation))
                selected.Add(ship);
        }
        if (selected.Count == 0) return;
        if (!_showedHint)
        {
            _showedHint = true;
            Show("{=dadg_fleet_sail_hint}Press Left Ctrl+M to toggle make sail for the selected sailing ships.");
        }
        if (!pressed) return;
        var makeSail = selected.Exists(ship => !OarlessShipAI.IsMakingSail(ship));
        foreach (var ship in selected) OarlessShipAI.SetMakeSail(ship, makeSail);
        Show(makeSail ? "{=dadg_fleet_make_sail}Selected ships ordered to make sail." :
            "{=dadg_fleet_auto_sail}Selected ships returned to automatic sail control.");
    }

    private static void Show(string message) =>
        InformationManager.DisplayMessage(new InformationMessage(new TextObject(message).ToString()));
}

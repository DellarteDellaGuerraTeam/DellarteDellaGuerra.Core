using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.Naval;

public class ShipSwivelGunCrewMissionLogic : MissionLogic
{
    // DADG ships carry several swivel falconets under one upgrade slot, but MissionShip keeps only the
    // last RangedSiegeWeapon it finds as ShipSiegeWeapon, and ShipOrder crews that one alone. The other
    // guns are fully working machines that nobody is ever sent to. Crew them the way ShipOrder crews
    // ShipSiegeWeapon: join the ship formation to the gun, then seat a detachable agent on the pilot point.
    // MissionShip lives in the optional NavalDLC assembly, so it is only reached through reflection.
    private const string GunTag = "dadg_ship_falconet";
    private const float UpdateInterval = 1f;
    private const float MaxCrewDistanceSquared = 15f * 15f;

    private readonly Dictionary<RangedSiegeWeapon, ShipView?> _guns = new();
    private readonly Dictionary<RangedSiegeWeapon, Formation> _joinedFormations = new();
    private float _timer;

    public override void OnMissionTick(float dt)
    {
        _timer += dt;
        if (_timer < UpdateInterval)
            return;
        _timer = 0f;

        foreach (var gun in Mission.ActiveMissionObjects.OfType<RangedSiegeWeapon>())
        {
            if (!_guns.TryGetValue(gun, out var ship))
            {
                ship = gun.GameEntity.HasTag(GunTag) ? ShipView.Find(gun.GameEntity) : null;
                _guns[gun] = ship;
            }

            if (ship != null)
                Crew(gun, ship.Value);
        }
    }

    private void Crew(RangedSiegeWeapon gun, ShipView ship)
    {
        var formation = ship.Formation;
        if (_joinedFormations.TryGetValue(gun, out var joined) && (joined != formation || !ship.IsCrewable))
        {
            if (joined.Detachments.IndexOf(gun) >= 0)
                joined.StopUsingMachine(gun, isPlayerOrder: true);
            _joinedFormations.Remove(gun);
        }

        // ShipOrder already crews this one
        if (ship.SiegeWeapon == gun || formation?.Team == null || !ship.IsCrewable)
            return;
        if (gun.IsDisabled || gun.IsDestroyed || gun.AmmoCount <= 0 || gun.PilotAgent != null
            || gun.PilotStandingPoint == null || gun.PilotStandingPoint.HasAIMovingTo
            || gun.IsDisabledForBattleSideAI(formation.Team.Side))
            return;

        // Once the ship is set up, ShipOrder parks every idle sailor in its ShipPlacementDetachment, so no
        // agent is free in the formation. ShipOrder itself reclaims one with PickLastAgent; take the nearest.
        var placement = ship.PlacementDetachment;
        var gunPosition = gun.GameEntity.GlobalPosition;
        Agent? crew = null;
        var bestDistance = MaxCrewDistanceSquared;
        formation.ApplyActionOnEachUnit(agent =>
        {
            if (!agent.IsActive() || !agent.IsAIControlled || agent.InteractingWithAnyGameObject())
                return;
            var placed = placement != null && agent.Detachment == placement;
            if (!placed && (!agent.IsDetachableFromFormation || !agent.CanBeAssignedForScriptedMovement()))
                return;
            var distance = agent.Position.DistanceSquared(gunPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                crew = agent;
            }
        });
        if (crew == null)
            return;
        if (placement != null && crew.Detachment == placement)
        {
            placement.RemoveAgent(crew);
            formation.AttachUnit(crew);
        }

        if (formation.Detachments.IndexOf(gun) < 0)
            formation.StartUsingMachine(gun, isPlayerOrder: true);
        _joinedFormations[gun] = formation;
        // Vanilla only fills the target list of ShipSiegeWeapon; an AI pilot on any other gun NREs in GetAllThreats
        (gun.Ai as RangedSiegeWeaponAi)?.InitializeThreatSeeker();
        gun.AddAgentAtSlotIndex(crew, gun.PilotStandingPointSlotIndex);
    }

    private readonly struct ShipView
    {
        private static readonly Dictionary<System.Type, PropertyInfo[]> Properties = new();
        private static readonly string[] Names =
            { "Formation", "ShipSiegeWeapon", "IsDeployed", "BeingAbandoned", "IsSinking", "ShipPlacementDetachment" };

        private readonly object _ship;
        private readonly PropertyInfo[] _properties;

        private ShipView(object ship, PropertyInfo[] properties)
        {
            _ship = ship;
            _properties = properties;
        }

        public Formation? Formation => (Formation?)_properties[0].GetValue(_ship);
        public RangedSiegeWeapon? SiegeWeapon => (RangedSiegeWeapon?)_properties[1].GetValue(_ship);
        public IDetachment? PlacementDetachment => (IDetachment?)_properties[5].GetValue(_ship);

        public bool IsCrewable => (bool)_properties[2].GetValue(_ship)
                                  && !(bool)_properties[3].GetValue(_ship)
                                  && !(bool)_properties[4].GetValue(_ship);

        public static ShipView? Find(WeakGameEntity entity)
        {
            for (var e = entity; e.IsValid; e = e.Parent)
            {
                var ship = e.GetScriptComponents().FirstOrDefault(s => s?.GetType().Name == "MissionShip");
                if (ship == null)
                    continue;
                var type = ship.GetType();
                if (!Properties.TryGetValue(type, out var properties))
                {
                    properties = Names.Select(n => type.GetProperty(n, BindingFlags.Public | BindingFlags.Instance)).ToArray()!;
                    Properties[type] = properties;
                }
                return properties.All(p => p != null) ? new ShipView(ship, properties) : null;
            }
            return null;
        }
    }
}

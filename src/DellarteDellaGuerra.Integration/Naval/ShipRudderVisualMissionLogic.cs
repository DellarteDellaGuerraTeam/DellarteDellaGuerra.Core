using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.Naval;

public sealed class ShipRudderVisualMissionLogic : MissionLogic
{
    private const string RudderTag = "dadg_rudder_visual";
    private readonly HashSet<MissionObject> _registeredShips = new();
    private readonly List<RudderVisual> _rudders = new();
    private float _discoveryTimer;

    public override void OnPreDisplayMissionTick(float dt)
    {
        _discoveryTimer -= dt;
        if (_discoveryTimer <= 0f)
        {
            _discoveryTimer = 1f;
            DiscoverRudders();
        }

        foreach (var rudder in _rudders)
        {
            if (!rudder.Entity.IsValid)
                continue;
            // Read the native absolute angle from the resting frame; never accumulate it each tick.
            var frame = rudder.RestFrame;
            frame.rotation.RotateAboutUp(rudder.ReadRotation());
            rudder.Entity.SetLocalFrame(ref frame, false);
        }
    }

    private void DiscoverRudders()
    {
        foreach (var ship in Mission.ActiveMissionObjects)
        {
            if (_registeredShips.Contains(ship) || ship.GetType().Name != "MissionShip")
                continue;
            var entities = ship.GameEntity.CollectChildrenEntitiesWithTag(RudderTag);
            if (entities.Count == 0)
                continue;

            // NavalDLC is optional; use its visual angle without adding an assembly dependency.
            var property = ship.GetType().GetProperty("VisualRudderRotation", BindingFlags.Public | BindingFlags.Instance);
            var getter = property?.GetGetMethod();
            if (property?.PropertyType != typeof(float) || getter == null)
                continue;
            var readRotation = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), ship, getter);
            foreach (var entity in entities)
                _rudders.Add(new RudderVisual(entity, entity.GetFrame(), readRotation));
            _registeredShips.Add(ship);
        }
    }

    private readonly struct RudderVisual
    {
        public WeakGameEntity Entity { get; }
        public MatrixFrame RestFrame { get; }
        public Func<float> ReadRotation { get; }

        public RudderVisual(WeakGameEntity entity, MatrixFrame restFrame, Func<float> readRotation)
        {
            Entity = entity;
            RestFrame = restFrame;
            ReadRotation = readRotation;
        }
    }
}

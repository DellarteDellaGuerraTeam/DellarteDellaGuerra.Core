using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Integration.Naval;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

// Exercise the production mission logic against a minimal engine double, without loading native DLLs.
var logic = new ShipRudderVisualMissionLogic();
logic.OnPreDisplayMissionTick(0.1f); // Non-naval missions must be harmless.
var blade = new EntityState { Frame = new MatrixFrame { rotation = new Mat3 { Angle = 0.3f }, Origin = 123 } };
var root = new EntityState();
root.TaggedChildren.Add(new WeakGameEntity(blade));
var ship = new MissionShip { GameEntity = new WeakGameEntity(root), VisualRudderRotation = 0.2f };
logic.Mission.ActiveMissionObjects.Add(ship); // Late spawning must be discovered.
logic.OnPreDisplayMissionTick(1f);
Check(blade.Frame.rotation.Angle, 0.5f, "first angle");
logic.OnPreDisplayMissionTick(0.1f);
Check(blade.Frame.rotation.Angle, 0.5f, "absolute angle must not accumulate");
ship.VisualRudderRotation = -0.4f;
logic.OnPreDisplayMissionTick(1f);
Check(blade.Frame.rotation.Angle, -0.1f, "reverse steering and rediscovery must preserve resting angle");
if (blade.Frame.Origin != 123) throw new Exception("Rudder pivot moved");
blade.Valid = false;
logic.OnPreDisplayMissionTick(0.1f); // A removed blade must not be written.
Console.WriteLine("PASS: optional DLC, late spawn, absolute/reversed rotation, stable pivot, removed blade.");

static void Check(float actual, float expected, string label)
{
    if (Math.Abs(actual - expected) > 0.00001f) throw new Exception(label);
}

public sealed class MissionShip : MissionObject
{
    public float VisualRudderRotation { get; set; }
}

namespace TaleWorlds.Library
{
    public struct Mat3
    {
        public float Angle;
        public void RotateAboutUp(float angle) => Angle += angle;
    }
    public struct MatrixFrame
    {
        public Mat3 rotation;
        public int Origin;
    }
}

namespace TaleWorlds.Engine
{
    public sealed class EntityState
    {
        public bool Valid = true;
        public MatrixFrame Frame;
        public List<WeakGameEntity> TaggedChildren = new();
    }
    public readonly struct WeakGameEntity
    {
        private readonly EntityState _state;
        public WeakGameEntity(EntityState state) => _state = state;
        public bool IsValid => _state.Valid;
        public MatrixFrame GetFrame() => _state.Frame;
        public List<WeakGameEntity> CollectChildrenEntitiesWithTag(string tag)
        {
            if (tag != "dadg_rudder_visual") throw new Exception("Unexpected tag");
            return _state.TaggedChildren;
        }
        public void SetLocalFrame(ref MatrixFrame frame, bool teleport)
        {
            if (!_state.Valid) throw new Exception("Wrote removed entity");
            _state.Frame = frame;
        }
    }
}

namespace TaleWorlds.MountAndBlade
{
    public class MissionObject { public WeakGameEntity GameEntity { get; set; } }
    public sealed class Mission { public List<MissionObject> ActiveMissionObjects { get; } = new(); }
    public abstract class MissionLogic
    {
        public Mission Mission { get; } = new();
        public virtual void OnPreDisplayMissionTick(float dt) { }
    }
}

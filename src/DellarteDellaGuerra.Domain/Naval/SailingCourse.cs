using System;
using System.Numerics;

namespace DellarteDellaGuerra.Domain.Naval
{
    public sealed class SailingCourse
    {
        // Upwind legs are 70 degrees off the wind source; turn onto a direct course outside 60.
        private const float TackCos = 0.34202015f;
        private const float TackSin = 0.93969262f;
        private const float CloseHauledCos = 0.5f;
        private Vector2 _waypoint;
        private Vector2 _destination;
        private Vector2 _upwind;
        private bool _onLeg;

        public void Reset() => _onLeg = false;

        public bool TryTack(Vector2 position, Vector2 destination, Vector2 forward, Vector2 wind,
            float hullLength, Func<Vector2, bool> isClear, out Vector2 waypoint)
        {
            waypoint = destination;
            var delta = destination - position;
            var distance = delta.Length();
            var arrival = Math.Max(5f, hullLength * 0.35f);
            if (wind.LengthSquared() < 1f || distance <= arrival)
            {
                Reset();
                return false;
            }

            var upwind = -Vector2.Normalize(wind);
            var along = Vector2.Dot(delta, upwind);
            // Keep a committed leg until its endpoint, a changed order/wind, or an obstruction.
            if (_onLeg && Vector2.DistanceSquared(destination, _destination) < arrival * arrival
                && Vector2.Dot(upwind, _upwind) > 0.96f
                && Vector2.Distance(position, _waypoint) > arrival && isClear(_waypoint))
            {
                waypoint = _waypoint;
                return true;
            }

            Reset();
            if (along <= CloseHauledCos * distance) return false;
            var across = new Vector2(-upwind.Y, upwind.X);
            var lateral = Vector2.Dot(delta, across);
            // Prefer the side that closes the lateral error. On the wind line turn toward the current bow.
            var side = Math.Abs(lateral) > arrival ? Math.Sign(lateral) :
                (Vector2.Dot(forward, across) >= 0f ? 1 : -1);

            for (var attempt = 0; attempt < 2; attempt++, side = -side)
            {
                var direction = upwind * TackCos + across * (side * TackSin);
                // Intersection with the opposite tack through the destination.
                var leg = Math.Min(4f * hullLength, 0.5f * (along / TackCos + side * lateral / TackSin));
                var candidate = position + direction * leg;
                if (leg <= arrival || !isClear(candidate)) continue;
                _destination = destination;
                _upwind = upwind;
                _waypoint = waypoint = candidate;
                _onLeg = true;
                return true;
            }
            return false;
        }

        public static Vector2 SkirmishWaypoint(Vector2 target, Vector2 outward, int turn, float radius)
        {
            var tangent = new Vector2(-outward.Y, outward.X) * turn;
            // Lead around the enemy by 45 degrees so the ship sails a chord across its flank.
            return target + (outward + tangent) * (0.70710678f * radius);
        }
    }
}

using System.Numerics;
using DellarteDellaGuerra.Domain.Naval;

namespace DellarteDellaGuerra.Domain.Tests.Naval;

public class SailingCourseShould
{
    private static readonly Vector2 Wind = -Vector2.UnitY * 10f;
    private static readonly Vector2 UpwindMark = Vector2.UnitY * 400f;
    private readonly SailingCourse _course = new();

    [Fact]
    public void ReachAnUpwindMarkOnBothTacks()
    {
        var position = Vector2.Zero;
        var bow = Vector2.UnitY;
        var sides = new HashSet<int>();
        for (int leg = 0; leg < 20; leg++)
        {
            if (!_course.TryTack(position, UpwindMark, bow, Wind, 30, _ => true, out var next)) break;
            bow = Vector2.Normalize(next - position);
            Assert.InRange(bow.Y, 0.33f, 0.35f);
            Assert.True(next.Y > position.Y);
            sides.Add(Math.Sign(bow.X));
            position = next;
        }
        Assert.Equal(2, sides.Count);
        var remaining = UpwindMark - position;
        Assert.True(remaining.Length() <= 10.5f || Vector2.Normalize(remaining).Y <= 0.5f);
    }

    [Fact]
    public void HoldTheLegWhileTheBowSwingsButFollowANewOrder()
    {
        Assert.True(_course.TryTack(Vector2.Zero, UpwindMark, Vector2.UnitX, Wind, 30, _ => true, out var leg));
        Assert.True(_course.TryTack(leg * 0.3f, UpwindMark, -Vector2.UnitX, Wind, 30, _ => true, out var same));
        Assert.Equal(leg, same);
        Assert.False(_course.TryTack(leg * 0.3f, -UpwindMark, -Vector2.UnitX, Wind, 30, _ => true, out _));
    }

    [Fact]
    public void UseTheClearSideAndYieldWhenBothLegsAreBlocked()
    {
        Assert.True(_course.TryTack(Vector2.Zero, UpwindMark, Vector2.UnitX, Wind, 30, p => p.X < 0f, out var leg));
        Assert.True(leg.X < 0f);
        Assert.False(_course.TryTack(Vector2.Zero, UpwindMark, Vector2.UnitX, Wind, 30, _ => false, out _));
    }

    [Theory]
    [InlineData(100, 0, 10)]
    [InlineData(0, -100, 10)]
    [InlineData(0, 100, 0)]
    [InlineData(0, 3, 10)]
    public void LeaveSailableCalmOrArrivingCoursesToNativeNavigation(float x, float y, float speed)
    {
        var mark = new Vector2(x, y);
        Assert.False(_course.TryTack(Vector2.Zero, mark, Vector2.UnitY, -Vector2.UnitY * speed, 30, _ => true, out var next));
        Assert.Equal(mark, next);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void PassAroundTheMovingTargetAtStandOffRange(int turn)
    {
        var center = new Vector2(71, 27);
        var outward = Vector2.UnitX;
        for (int i = 0; i < 8; i++)
        {
            var waypoint = SailingCourse.SkirmishWaypoint(center, outward, turn, 65);
            var newRadius = waypoint - center;
            Assert.InRange(newRadius.Length(), 64.99f, 65.01f);
            Assert.Equal(turn, Math.Sign(outward.X * newRadius.Y - outward.Y * newRadius.X));
            var chordMidpoint = (outward * 65 + newRadius) / 2;
            Assert.True(chordMidpoint.Length() > 60f);
            outward = Vector2.Normalize(newRadius);
            center += new Vector2(2, 3);
        }
        Assert.InRange(Vector2.Distance(outward, Vector2.UnitX), 0f, 0.001f);
    }
}

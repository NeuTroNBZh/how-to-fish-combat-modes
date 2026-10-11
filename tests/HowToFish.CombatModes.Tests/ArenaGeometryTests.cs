using System.Numerics;
using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class ArenaGeometryTests
{
    [Fact]
    public void AutoSpawns_AreOppositeAndFaceEachOther()
    {
        var (a, b) = ArenaGeometry.AutoSpawns(new Vector3(10, 5, 20), 60);

        Assert.Equal(new Vector3(-20, 5, 20), a.Position);
        Assert.Equal(new Vector3(40, 5, 20), b.Position);
        Assert.Equal(90f, a.Yaw, 3);
        Assert.Equal(270f, b.Yaw, 3);
    }

    [Fact]
    public void For_SwapsSpawnsWhenHostIsNotAtA()
    {
        var spawns = ArenaGeometry.Facing(Vector3.Zero, new Vector3(0, 0, 10));

        Assert.Equal(spawns.A, ArenaGeometry.For(Side.Host, true, spawns));
        Assert.Equal(spawns.B, ArenaGeometry.For(Side.Guest, true, spawns));
        Assert.Equal(spawns.B, ArenaGeometry.For(Side.Host, false, spawns));
        Assert.Equal(0f, spawns.A.Yaw, 3);
        Assert.Equal(180f, spawns.B.Yaw, 3);
    }

    [Theory]
    [InlineData(59, 0, 0, false)]
    [InlineData(61, 0, 0, true)]
    [InlineData(0, 0, -11, true)]
    [InlineData(0, 0, -9, false)]
    public void IsOutside_ChecksRadiusAndFall(float x, float z, float y, bool expected)
    {
        Assert.Equal(expected, ArenaGeometry.IsOutside(new Vector3(x, y, z), Vector3.Zero, 60));
    }
}

using System;
using System.Numerics;
using HowToFish.CombatModes.Core.Arena;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class SpawnPickerTests
{
    [Fact]
    public void Ring_PlacesPointsAtRadiusAroundCenter()
    {
        var center = new Vector3(10, 5, -20);

        var ring = SpawnPicker.Ring(center, 30, 12);

        Assert.Equal(12, ring.Count);
        Assert.All(ring, p =>
        {
            Assert.Equal(5, p.Y);
            Assert.Equal(30, Vector2.Distance(new Vector2(p.X, p.Z), new Vector2(10, -20)), 3);
        });
    }

    [Fact]
    public void Farthest_NoOccupied_ReturnsFirst()
    {
        Assert.Equal(0, SpawnPicker.Farthest(new[] { Vector3.Zero, Vector3.One }, Array.Empty<Vector3>()));
    }

    [Fact]
    public void Farthest_PicksCandidateFarthestFromNearestPlayer()
    {
        var candidates = new[] { new Vector3(0, 0, 0), new Vector3(50, 0, 0), new Vector3(100, 0, 0) };
        var occupied = new[] { new Vector3(10, 0, 0), new Vector3(60, 0, 0) };

        Assert.Equal(2, SpawnPicker.Farthest(candidates, occupied));
    }

    [Fact]
    public void Farthest_NoCandidates_Throws()
    {
        Assert.Throws<ArgumentException>(() => SpawnPicker.Farthest(Array.Empty<Vector3>(), Array.Empty<Vector3>()));
    }
}

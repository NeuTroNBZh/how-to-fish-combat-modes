using System;
using HowToFish.Duel1v1.Core;
using Xunit;

namespace HowToFish.Duel1v1.Tests;

public class IslandPickerTests
{
    [Fact]
    public void Pick_SingleIsland_AlwaysReturnsIt()
    {
        Assert.Equal(4, IslandPicker.Pick(new[] { 4 }, 4, new FakeRandom(3)));
    }

    [Fact]
    public void Pick_NoPrevious_UsesRandomIndex()
    {
        Assert.Equal(2, IslandPicker.Pick(new[] { 0, 1, 2 }, null, new FakeRandom(2)));
    }

    [Fact]
    public void Pick_ExcludesPreviousIsland()
    {
        Assert.Equal(2, IslandPicker.Pick(new[] { 0, 1, 2 }, 1, new FakeRandom(1)));
    }

    [Fact]
    public void Pick_NoIslands_Throws()
    {
        Assert.Throws<ArgumentException>(() => IslandPicker.Pick(Array.Empty<int>(), null, new FakeRandom()));
    }
}

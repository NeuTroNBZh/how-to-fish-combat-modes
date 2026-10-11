using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class SideTests
{
    [Theory]
    [InlineData(Side.Host, Side.Guest)]
    [InlineData(Side.Guest, Side.Host)]
    public void Other_ReturnsOppositeSide(Side side, Side expected)
    {
        Assert.Equal(expected, side.Other());
    }
}

using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class GameModesTests
{
    [Fact]
    public void Tag_RoundTripsForEveryMode()
    {
        foreach (var mode in GameModes.All)
        {
            Assert.True(GameModes.TryParseTag(GameModes.Tag(mode), out var parsed));
            Assert.Equal(mode, parsed);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tdm")]
    public void TryParseTag_Unknown_Fails(string? tag)
    {
        Assert.False(GameModes.TryParseTag(tag, out _));
    }

    [Fact]
    public void MaxMembers_DuelIsTwo_ArenaIsEight()
    {
        Assert.Equal(2, GameModes.MaxMembers(GameMode.Duel));
        Assert.Equal(8, GameModes.MaxMembers(GameMode.Deathmatch));
        Assert.Equal(8, GameModes.MaxMembers(GameMode.GunGame));
    }
}

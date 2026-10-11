using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class ArenaCodecTests
{
    private static ArenaWire Sample() => new(
        GameMode.GunGame, ArenaPhase.Playing, 1500, "pistol+knife", 76561198000000001UL,
        new[]
        {
            new ArenaWirePlayer(76561198000000001UL, 3, 1, 2, true),
            new ArenaWirePlayer(76561198000000002UL, -1, 4, 0, false),
        },
        new[] { "pistol", "knife" });

    [Fact]
    public void Encode_ThenDecode_RoundTrips()
    {
        var text = ArenaCodec.Encode(Sample());

        Assert.True(ArenaCodec.IsArena(text));
        Assert.True(ArenaCodec.TryDecode(text, out var wire, out var error), error);
        Assert.Equal(GameMode.GunGame, wire!.Mode);
        Assert.Equal(ArenaPhase.Playing, wire.Phase);
        Assert.Equal(1500, wire.RemainingMs);
        Assert.Equal("pistol+knife", wire.KitName);
        Assert.Equal(76561198000000001UL, wire.Winner);
        Assert.Equal(Sample().Players, wire.Players);
        Assert.Equal(new[] { "pistol", "knife" }, wire.Ladder);
    }

    [Fact]
    public void Encode_NoPlayersNoWinner_RoundTrips()
    {
        var empty = new ArenaWire(GameMode.Deathmatch, ArenaPhase.Lobby, 0, "", null,
            System.Array.Empty<ArenaWirePlayer>(), System.Array.Empty<string>());

        Assert.True(ArenaCodec.TryDecode(ArenaCodec.Encode(empty), out var wire, out _));
        Assert.Null(wire!.Winner);
        Assert.Empty(wire.Players);
        Assert.Empty(wire.Ladder);
    }

    [Theory]
    [InlineData("HTFCM|2|ARENA|1|0|0||0|")]
    [InlineData("HTFCM|2|ARENA|0|0|0||0||")]
    [InlineData("HTFCM|2|ARENA|1|9|0||0||")]
    [InlineData("HTFCM|2|ARENA|1|0|-5||0||")]
    [InlineData("HTFCM|2|ARENA|1|0|0||x||")]
    [InlineData("HTFCM|2|ARENA|1|0|0||0|12:1:1:0|")]
    [InlineData("HTFCM|2|ARENA|1|0|0||0|12:1:-1:0:1|")]
    [InlineData("HTFCM|2|ARENA|1|0|0||0|1:0:0:0:2|")]
    [InlineData("HTFCM|2|ARENA|1|0|0||0|1:0:0:0:1,2:0:0:0:1,3:0:0:0:1,4:0:0:0:1,5:0:0:0:1,6:0:0:0:1,7:0:0:0:1,8:0:0:0:1,9:0:0:0:1|")]
    public void TryDecode_Invalid_Fails(string text)
    {
        Assert.False(ArenaCodec.TryDecode(text, out _, out var error));
        Assert.NotEmpty(error);
    }

    [Fact]
    public void IsArena_RejectsDuelMessages()
    {
        Assert.False(ArenaCodec.IsArena("HTFCM|1|HELLO"));
    }

    [Fact]
    public void From_Snapshot_ComputesRemainingTimePerMode()
    {
        var playing = ArenaSnapshot.Lobby(GameMode.Deathmatch) with { Phase = ArenaPhase.Playing, EndsAt = 70 };
        var countdown = playing with { Phase = ArenaPhase.Countdown, PhaseEndsAt = 12 };

        Assert.Equal(60_000, ArenaWire.From(playing, 10, System.Array.Empty<string>()).RemainingMs);
        Assert.Equal(2_000, ArenaWire.From(countdown, 10, System.Array.Empty<string>()).RemainingMs);
        Assert.Equal(0, ArenaWire.From(playing with { Mode = GameMode.GunGame }, 10, System.Array.Empty<string>()).RemainingMs);
    }
}

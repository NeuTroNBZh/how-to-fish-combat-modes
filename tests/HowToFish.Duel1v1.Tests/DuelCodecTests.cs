using HowToFish.Duel1v1.Core;
using Xunit;

namespace HowToFish.Duel1v1.Tests;

public class DuelCodecTests
{
    private static readonly WireState Sample = new(
        DuelPhase.RoundEnd, 3, 2, 1, 2500, "pistol+knife", Side.Guest, false, null, false, true);

    [Fact]
    public void Hello_EncodesAndDecodes()
    {
        var text = DuelCodec.Encode(DuelMessage.Hello());

        Assert.Equal("HTFDUEL|1|HELLO", text);
        Assert.True(DuelCodec.TryDecode(text, out var m, out _));
        Assert.Equal(MessageKind.Hello, m!.Kind);
    }

    [Fact]
    public void Rematch_EncodesAndDecodes()
    {
        var text = DuelCodec.Encode(DuelMessage.Rematch());

        Assert.Equal("HTFDUEL|1|REQ|REMATCH", text);
        Assert.True(DuelCodec.TryDecode(text, out var m, out _));
        Assert.Equal(MessageKind.Rematch, m!.Kind);
    }

    [Fact]
    public void State_RoundTrips()
    {
        var text = DuelCodec.Encode(DuelMessage.Of(Sample));

        Assert.Equal("HTFDUEL|1|STATE|4|3|2|1|2500|pistol+knife|1|0|-1|0|1", text);
        Assert.True(DuelCodec.TryDecode(text, out var m, out _));
        Assert.Equal(Sample, m!.State);
        Assert.True(text.Length < 200);
    }

    [Theory]
    [InlineData("", "préfixe")]
    [InlineData("HELLO", "préfixe")]
    [InlineData("HTFDUEL|2|HELLO", "version")]
    [InlineData("HTFDUEL|x|HELLO", "version")]
    [InlineData("HTFDUEL|1|PING", "type")]
    [InlineData("HTFDUEL|1|REQ|NOPE", "type")]
    [InlineData("HTFDUEL|1|STATE|4|3|2|1|2500|kit|1|0|-1|0", "champs")]
    [InlineData("HTFDUEL|1|STATE|4|3|x|1|2500|kit|1|0|-1|0|1", "nombre")]
    [InlineData("HTFDUEL|1|STATE|9|3|2|1|2500|kit|1|0|-1|0|1", "phase")]
    [InlineData("HTFDUEL|1|STATE|4|3|2|1|2500|kit|5|0|-1|0|1", "côté")]
    [InlineData("HTFDUEL|1|STATE|4|3|-2|1|2500|kit|1|0|-1|0|1", "négative")]
    [InlineData("HTFDUEL|1|STATE|4|3|2|1|2500|kit|1|7|-1|0|1", "booléen")]
    public void TryDecode_RejectsInvalidMessages(string text, string expectedErrorPart)
    {
        Assert.False(DuelCodec.TryDecode(text, out var m, out var error));
        Assert.Null(m);
        Assert.Contains(expectedErrorPart, error);
    }

    [Fact]
    public void WireState_From_ComputesRemainingTimeAndKitName()
    {
        var snapshot = DuelSnapshot.Initial with
        {
            Phase = DuelPhase.Fighting, PhaseEndsAt = 50, Kit = new Kit("bow", new[] { "bow" }),
        };

        var wire = WireState.From(snapshot, 47.5);

        Assert.Equal(2500, wire.RemainingMs);
        Assert.Equal("bow", wire.KitName);
        Assert.Equal(0, WireState.From(snapshot, 60).RemainingMs);
        Assert.Equal("", WireState.From(DuelSnapshot.Initial, 0).KitName);
    }
}

using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class HudFormatterTests
{
    private static WireState State(DuelPhase phase, int host = 0, int guest = 0, Side? round = null,
        bool draw = false, Side? match = null, bool forfeit = false) =>
        new(phase, 6, host, guest, 47200, "bow", round, draw, match, forfeit, true);

    [Theory]
    [InlineData(47.2, "0:48")]
    [InlineData(90, "1:30")]
    [InlineData(0, "0:00")]
    [InlineData(-3, "0:00")]
    public void Clock_RoundsUpToSeconds(double seconds, string expected)
    {
        Assert.Equal(expected, HudFormatter.Clock(seconds));
    }

    [Fact]
    public void CountdownNumber_RoundsUp()
    {
        Assert.Equal(3, HudFormatter.CountdownNumber(2.1));
        Assert.Equal(1, HudFormatter.CountdownNumber(0.2));
    }

    [Fact]
    public void ScoreLine_PutsLocalPlayerFirst()
    {
        var s = State(DuelPhase.Fighting, host: 3, guest: 2);

        Assert.Equal("Moi 2 - 3 Lui", HudFormatter.ScoreLine(s, Side.Guest, "Moi", "Lui"));
        Assert.Equal("Moi 3 - 2 Lui", HudFormatter.ScoreLine(s, Side.Host, "Moi", "Lui"));
    }

    [Fact]
    public void RoundBanner_DependsOnLocalSide()
    {
        Assert.Equal("TU GAGNES LA MANCHE", HudFormatter.RoundBanner(State(DuelPhase.RoundEnd, round: Side.Host), Side.Host));
        Assert.Equal("L'ADVERSAIRE GAGNE LA MANCHE", HudFormatter.RoundBanner(State(DuelPhase.RoundEnd, round: Side.Host), Side.Guest));
        Assert.Equal("MANCHE NULLE", HudFormatter.RoundBanner(State(DuelPhase.RoundEnd, draw: true), Side.Guest));
    }

    [Fact]
    public void MatchBanner_ShowsResultWithLocalScoreFirst()
    {
        var s = State(DuelPhase.MatchEnd, host: 3, guest: 5, match: Side.Guest);

        Assert.Equal("VICTOIRE 5 - 3", HudFormatter.MatchBanner(s, Side.Guest));
        Assert.Equal("DÉFAITE 3 - 5", HudFormatter.MatchBanner(s, Side.Host));
    }

    [Fact]
    public void WaitingText_MentionsForfeit()
    {
        Assert.Equal("En attente d'un adversaire…", HudFormatter.WaitingText(null, Side.Host));
        Assert.Equal("L'adversaire a quitté — victoire par forfait. En attente d'un adversaire…",
            HudFormatter.WaitingText(State(DuelPhase.Waiting, match: Side.Host, forfeit: true), Side.Host));
    }
}

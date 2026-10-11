using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class DuelRulesTests
{
    private static DuelSettings Settings(int roundsToWin = 2, int countdown = 3) =>
        DuelSettings.FromRaw(DuelSettings.Defaults with
        {
            RoundsToWin = roundsToWin, CountdownSeconds = countdown, Kits = "a;b",
        }, out _);

    private static DuelRules Rules(int roundsToWin = 2, int countdown = 3) =>
        new(Settings(roundsToWin, countdown), new FakeRandom());

    private static DuelSnapshot Fighting(DuelRules rules, double at = 10)
    {
        var s = rules.Apply(DuelSnapshot.Initial, new PlayersReady(), at).Snapshot;
        s = rules.Apply(s, new Tick(100, 100), at + 1).Snapshot;
        return rules.Apply(s, new Tick(100, 100), at + 4).Snapshot;
    }

    [Fact]
    public void PlayersReady_InWaiting_StartsFirstRound()
    {
        var step = Rules().Apply(DuelSnapshot.Initial, new PlayersReady(), 10);

        Assert.Equal(DuelEffect.PrepareRound, step.Effect);
        Assert.Equal(DuelPhase.Preparing, step.Snapshot.Phase);
        Assert.Equal(1, step.Snapshot.Round);
        Assert.Equal(0, step.Snapshot.HostScore);
        Assert.Equal("a", step.Snapshot.Kit!.Name);
        Assert.True(step.Snapshot.HostAtSpawnA);
        Assert.Equal(11, step.Snapshot.PhaseEndsAt);
    }

    private static DuelRules RulesOnIslands(params int[] islands) =>
        new(Settings() with { Islands = islands }, new FakeRandom());

    private static DuelSnapshot NextRound(DuelRules rules, DuelSnapshot fighting)
    {
        var ended = rules.Apply(fighting, new PlayerDied(Side.Guest), 20).Snapshot;
        return rules.Apply(ended, new Tick(100, 0), 20 + DuelSettings.RoundEndSeconds).Snapshot;
    }

    [Fact]
    public void PlayersReady_PicksIslandFromSettings()
    {
        var step = RulesOnIslands(3, 4).Apply(DuelSnapshot.Initial, new PlayersReady(), 10);

        Assert.Equal(3, step.Snapshot.Island);
    }

    [Fact]
    public void NextRound_MovesToAnotherIsland()
    {
        var rules = RulesOnIslands(0, 1, 2);
        var fighting = Fighting(rules);

        var next = NextRound(rules, fighting);

        Assert.Equal(DuelPhase.Preparing, next.Phase);
        Assert.NotEqual(fighting.Island, next.Island);
    }

    [Fact]
    public void NextRound_SingleIsland_StaysThere()
    {
        var rules = RulesOnIslands(4);

        var next = NextRound(rules, Fighting(rules));

        Assert.Equal(4, next.Island);
    }

    [Fact]
    public void PlayersReady_OutsideWaiting_IsIgnored()
    {
        var rules = Rules();
        var fighting = Fighting(rules);

        var step = rules.Apply(fighting, new PlayersReady(), 20);

        Assert.Equal(fighting, step.Snapshot);
        Assert.Equal(DuelEffect.None, step.Effect);
    }

    [Fact]
    public void Preparing_ThenCountdown_ThenFighting()
    {
        var rules = Rules();
        var s = rules.Apply(DuelSnapshot.Initial, new PlayersReady(), 10).Snapshot;

        Assert.Equal(DuelPhase.Preparing, rules.Apply(s, new Tick(100, 100), 10.5).Snapshot.Phase);

        var countdown = rules.Apply(s, new Tick(100, 100), 11);
        Assert.Equal(DuelPhase.Countdown, countdown.Snapshot.Phase);
        Assert.Equal(14, countdown.Snapshot.PhaseEndsAt);

        var fight = rules.Apply(countdown.Snapshot, new Tick(100, 100), 14);
        Assert.Equal(DuelPhase.Fighting, fight.Snapshot.Phase);
        Assert.Equal(DuelEffect.StartFight, fight.Effect);
        Assert.Equal(104, fight.Snapshot.PhaseEndsAt);
    }

    [Fact]
    public void ZeroCountdown_GoesToFightingOnNextTick()
    {
        var rules = Rules(countdown: 0);
        var s = rules.Apply(DuelSnapshot.Initial, new PlayersReady(), 10).Snapshot;
        s = rules.Apply(s, new Tick(100, 100), 11).Snapshot;

        Assert.Equal(DuelPhase.Fighting, rules.Apply(s, new Tick(100, 100), 11).Snapshot.Phase);
    }

    [Fact]
    public void Death_DuringFight_GivesPointToOtherSide()
    {
        var rules = Rules();

        var step = rules.Apply(Fighting(rules), new PlayerDied(Side.Guest), 30);

        Assert.Equal(DuelEffect.AnnounceRoundResult, step.Effect);
        Assert.Equal(DuelPhase.RoundEnd, step.Snapshot.Phase);
        Assert.Equal(1, step.Snapshot.HostScore);
        Assert.Equal(Side.Host, step.Snapshot.LastRoundWinner);
        Assert.Equal(33, step.Snapshot.PhaseEndsAt);
    }

    [Fact]
    public void Death_OutsideFight_IsIgnored()
    {
        var rules = Rules();
        var countdown = rules.Apply(
            rules.Apply(DuelSnapshot.Initial, new PlayersReady(), 10).Snapshot, new Tick(100, 100), 11).Snapshot;

        Assert.Equal(countdown, rules.Apply(countdown, new PlayerDied(Side.Host), 12).Snapshot);
    }

    [Fact]
    public void SecondDeathWithinWindow_TurnsRoundIntoDraw()
    {
        var rules = Rules();
        var s = rules.Apply(Fighting(rules), new PlayerDied(Side.Guest), 30).Snapshot;

        var step = rules.Apply(s, new PlayerDied(Side.Host), 30.8);

        Assert.Equal(DuelEffect.AnnounceRoundResult, step.Effect);
        Assert.True(step.Snapshot.LastRoundDraw);
        Assert.Null(step.Snapshot.LastRoundWinner);
        Assert.Equal(0, step.Snapshot.HostScore);
    }

    [Fact]
    public void SecondDeathAfterWindow_IsIgnored()
    {
        var rules = Rules();
        var s = rules.Apply(Fighting(rules), new PlayerDied(Side.Guest), 30).Snapshot;

        Assert.Equal(s, rules.Apply(s, new PlayerDied(Side.Host), 31.5).Snapshot);
    }

    [Fact]
    public void RoundEnd_StartsNextRoundWithSwappedSpawnsAndNewKit()
    {
        var rules = Rules();
        var s = rules.Apply(Fighting(rules), new PlayerDied(Side.Guest), 30).Snapshot;

        var step = rules.Apply(s, new Tick(100, 0), 33);

        Assert.Equal(DuelEffect.PrepareRound, step.Effect);
        Assert.Equal(DuelPhase.Preparing, step.Snapshot.Phase);
        Assert.Equal(2, step.Snapshot.Round);
        Assert.False(step.Snapshot.HostAtSpawnA);
        Assert.Equal("b", step.Snapshot.Kit!.Name);
        Assert.Equal(1, step.Snapshot.HostScore);
        Assert.Null(step.Snapshot.LastRoundWinner);
    }

    [Fact]
    public void DrawRound_IsReplayedWithSameRoundNumber()
    {
        var rules = Rules();
        var s = rules.Apply(Fighting(rules), new PlayerDied(Side.Guest), 30).Snapshot;
        s = rules.Apply(s, new PlayerDied(Side.Host), 30.5).Snapshot;

        Assert.Equal(1, rules.Apply(s, new Tick(0, 0), 33).Snapshot.Round);
    }

    [Fact]
    public void ReachingRoundsToWin_EndsMatch()
    {
        var rules = Rules(roundsToWin: 1);
        var s = rules.Apply(Fighting(rules), new PlayerDied(Side.Host), 30).Snapshot;

        var step = rules.Apply(s, new Tick(0, 100), 33);

        Assert.Equal(DuelEffect.AnnounceMatchResult, step.Effect);
        Assert.Equal(DuelPhase.MatchEnd, step.Snapshot.Phase);
        Assert.Equal(Side.Guest, step.Snapshot.MatchWinner);
        Assert.False(step.Snapshot.Forfeit);
    }

    [Theory]
    [InlineData(80, 30, Side.Host)]
    [InlineData(20, 60, Side.Guest)]
    public void Timeout_HigherHealthWins(int hostHp, int guestHp, Side expected)
    {
        var rules = Rules();

        var step = rules.Apply(Fighting(rules), new Tick(hostHp, guestHp), 104);

        Assert.Equal(DuelPhase.RoundEnd, step.Snapshot.Phase);
        Assert.Equal(expected, step.Snapshot.LastRoundWinner);
    }

    [Fact]
    public void Timeout_EqualHealth_IsDraw()
    {
        var rules = Rules();

        var step = rules.Apply(Fighting(rules), new Tick(50, 50), 104);

        Assert.True(step.Snapshot.LastRoundDraw);
        Assert.Equal(0, step.Snapshot.HostScore + step.Snapshot.GuestScore);
    }

    [Fact]
    public void PlayerLeft_DuringFight_ForfeitsToWaiting()
    {
        var rules = Rules();

        var step = rules.Apply(Fighting(rules), new PlayerLeft(Side.Guest), 30);

        Assert.Equal(DuelEffect.ResetToWaiting, step.Effect);
        Assert.Equal(DuelPhase.Waiting, step.Snapshot.Phase);
        Assert.True(step.Snapshot.Forfeit);
        Assert.Equal(Side.Host, step.Snapshot.MatchWinner);
        Assert.Equal(0, step.Snapshot.HostScore);
    }

    [Fact]
    public void PlayerLeft_InWaiting_IsIgnored()
    {
        var step = Rules().Apply(DuelSnapshot.Initial, new PlayerLeft(Side.Guest), 5);

        Assert.Equal(DuelSnapshot.Initial, step.Snapshot);
        Assert.Equal(DuelEffect.None, step.Effect);
    }

    [Fact]
    public void Rematch_AfterMatchEnd_RestartsAtZero()
    {
        var rules = Rules(roundsToWin: 1);
        var s = rules.Apply(Fighting(rules), new PlayerDied(Side.Host), 30).Snapshot;
        s = rules.Apply(s, new Tick(0, 100), 33).Snapshot;

        var step = rules.Apply(s, new RematchRequested(), 40);

        Assert.Equal(DuelEffect.PrepareRound, step.Effect);
        Assert.Equal(1, step.Snapshot.Round);
        Assert.Equal(0, step.Snapshot.GuestScore);
        Assert.Null(step.Snapshot.MatchWinner);
    }

    [Fact]
    public void AllowsDamage_DuringFightAndDoubleKoWindowOnly()
    {
        var rules = Rules();
        var fighting = Fighting(rules);
        var roundEnd = rules.Apply(fighting, new PlayerDied(Side.Guest), 30).Snapshot;
        var draw = rules.Apply(roundEnd, new PlayerDied(Side.Host), 30.5).Snapshot;

        Assert.False(DuelRules.AllowsDamage(DuelSnapshot.Initial, 0));
        Assert.True(DuelRules.AllowsDamage(fighting, 20));
        Assert.True(DuelRules.AllowsDamage(roundEnd, 30.9));
        Assert.False(DuelRules.AllowsDamage(roundEnd, 31.5));
        Assert.False(DuelRules.AllowsDamage(draw, 30.6));
    }

    [Fact]
    public void Rematch_DuringFight_IsIgnored()
    {
        var rules = Rules();
        var fighting = Fighting(rules);

        Assert.Equal(fighting, rules.Apply(fighting, new RematchRequested(), 30).Snapshot);
    }
}

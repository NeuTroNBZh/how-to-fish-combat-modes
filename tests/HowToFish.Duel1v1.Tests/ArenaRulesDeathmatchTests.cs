using System.Linq;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;
using Xunit;
using static HowToFish.Duel1v1.Tests.ArenaTestKit;

namespace HowToFish.Duel1v1.Tests;

public class ArenaRulesDeathmatchTests
{
    private const GameMode Dm = GameMode.Deathmatch;

    [Fact]
    public void Start_WithOnePlayer_IsIgnored()
    {
        var rules = Rules();
        var lobby = Lobby(rules, Dm, A);

        var step = rules.Apply(lobby, new ArenaStart(), 10);

        Assert.Same(lobby, step.Snapshot);
        Assert.Empty(step.Actions);
    }

    [Fact]
    public void Start_WithTwoPlayers_PreparesIslandAndKit()
    {
        var rules = Rules(islands: new[] { 3, 4 });

        var step = rules.Apply(Lobby(rules, Dm, A, B), new ArenaStart(), 10);

        Assert.Equal(ArenaPhase.Preparing, step.Snapshot.Phase);
        Assert.Equal(3, step.Snapshot.Island);
        Assert.Equal("a", step.Snapshot.Kit!.Name);
        Assert.Equal(11, step.Snapshot.PhaseEndsAt);
        Assert.Equal(new PrepareIsland(3), Assert.Single(step.Actions));
    }

    [Fact]
    public void Preparing_AfterDelay_StartsCountdownAndRespawnsEveryone()
    {
        var rules = Rules();
        var preparing = rules.Apply(Lobby(rules, Dm, A, B), new ArenaStart(), 10).Snapshot;

        Assert.Equal(ArenaPhase.Preparing, rules.Apply(preparing, new ArenaTick(), 10.5).Snapshot.Phase);
        var step = rules.Apply(preparing, new ArenaTick(), 11);

        Assert.Equal(ArenaPhase.Countdown, step.Snapshot.Phase);
        Assert.Equal(14, step.Snapshot.PhaseEndsAt);
        Assert.All(step.Snapshot.Players, p => Assert.True(p.Alive));
        Assert.Equal(new[] { A, B }, step.Actions.OfType<Respawn>().Select(r => r.Id));
        Assert.All(step.Actions.OfType<Respawn>(), r => Assert.Equal("a", r.Kit.Name));
    }

    [Fact]
    public void CountdownEnd_StartsPlayingWithTimer()
    {
        var rules = Rules();
        var s = rules.Apply(Lobby(rules, Dm, A, B), new ArenaStart(), 10).Snapshot;
        s = rules.Apply(s, new ArenaTick(), 11).Snapshot;

        var step = rules.Apply(s, new ArenaTick(), 14);

        Assert.Equal(ArenaPhase.Playing, step.Snapshot.Phase);
        Assert.Equal(614, step.Snapshot.EndsAt);
        Assert.Equal(134, step.Snapshot.KitChangesAt);
        Assert.IsType<StartFight>(Assert.Single(step.Actions));
    }

    [Fact]
    public void Kill_ScoresAndSchedulesVictimRespawn()
    {
        var rules = Rules();
        var playing = Playing(rules, Dm, A, B);

        var step = rules.Apply(playing, new ArenaKill(A, B, "a"), 20);

        Assert.Equal(1, P(step.Snapshot, A).Kills);
        var victim = P(step.Snapshot, B);
        Assert.False(victim.Alive);
        Assert.Equal(1, victim.Deaths);
        Assert.Equal(23, victim.RespawnAt);
        Assert.Equal(new Strip(B), Assert.Single(step.Actions));
    }

    [Fact]
    public void Kill_ReachingTarget_EndsGame()
    {
        var rules = Rules(r => r with { KillTarget = 1 });
        var playing = Playing(rules, Dm, A, B);

        var step = rules.Apply(playing, new ArenaKill(A, B, "a"), 20);

        Assert.Equal(ArenaPhase.Ended, step.Snapshot.Phase);
        Assert.Equal(A, step.Snapshot.Winner);
        Assert.Contains(new EndGame(A), step.Actions);
    }

    [Fact]
    public void Kill_OutsidePlaying_IsIgnored()
    {
        var rules = Rules();
        var lobby = Lobby(rules, Dm, A, B);

        Assert.Same(lobby, rules.Apply(lobby, new ArenaKill(A, B, "a"), 5).Snapshot);
    }

    [Fact]
    public void Kill_OnSelf_CountsAsDeathWithPenalty()
    {
        var rules = Rules();
        var playing = Playing(rules, Dm, A, B);

        var step = rules.Apply(playing, new ArenaKill(A, A, "a"), 20);

        Assert.Equal(-1, P(step.Snapshot, A).Kills);
        Assert.Equal(1, P(step.Snapshot, A).Deaths);
    }

    [Fact]
    public void Kill_KillerNotInGame_CountsAsPlainDeath()
    {
        var rules = Rules();
        var playing = Playing(rules, Dm, A, B);

        var step = rules.Apply(playing, new ArenaKill(C, B, "a"), 20);

        Assert.False(P(step.Snapshot, B).Alive);
        Assert.Equal(1, P(step.Snapshot, B).Deaths);
        Assert.Equal(0, P(step.Snapshot, B).Kills);
    }

    [Fact]
    public void Kill_VictimNotInGame_IsIgnored()
    {
        var rules = Rules();
        var playing = Playing(rules, Dm, A, B);

        Assert.Same(playing, rules.Apply(playing, new ArenaKill(A, C, "a"), 20).Snapshot);
    }

    [Fact]
    public void Death_WithoutKiller_RemovesOneKill()
    {
        var rules = Rules();
        var playing = Playing(rules, Dm, A, B);

        var step = rules.Apply(playing, new ArenaDeath(B), 20);

        Assert.Equal(-1, P(step.Snapshot, B).Kills);
        Assert.False(P(step.Snapshot, B).Alive);
    }

    [Fact]
    public void Death_OfDeadPlayer_IsIgnored()
    {
        var rules = Rules();
        var dead = rules.Apply(Playing(rules, Dm, A, B), new ArenaDeath(B), 20).Snapshot;

        Assert.Same(dead, rules.Apply(dead, new ArenaDeath(B), 20.1).Snapshot);
    }

    [Fact]
    public void Respawn_AfterDelay_WithProtection()
    {
        var rules = Rules();
        var dead = rules.Apply(Playing(rules, Dm, A, B), new ArenaKill(A, B, "a"), 20).Snapshot;

        Assert.False(P(rules.Apply(dead, new ArenaTick(), 22.9).Snapshot, B).Alive);
        var step = rules.Apply(dead, new ArenaTick(), 23);

        Assert.True(P(step.Snapshot, B).Alive);
        Assert.Equal(25, P(step.Snapshot, B).ProtectedUntil);
        Assert.Equal(B, Assert.Single(step.Actions.OfType<Respawn>()).Id);
    }

    [Fact]
    public void AllowsDamage_OnlyForAliveUnprotectedPlayersWhilePlaying()
    {
        var rules = Rules();
        var playing = Playing(rules, Dm, A, B);
        var respawned = rules.Apply(rules.Apply(playing, new ArenaKill(A, B, "a"), 20).Snapshot, new ArenaTick(), 23).Snapshot;

        Assert.True(ArenaRules.AllowsDamage(playing, B, 15));
        Assert.False(ArenaRules.AllowsDamage(Lobby(rules, Dm, A, B), B, 15));
        Assert.False(ArenaRules.AllowsDamage(respawned, B, 24));
        Assert.True(ArenaRules.AllowsDamage(respawned, B, 25));
        Assert.False(ArenaRules.AllowsDamage(playing, C, 15));
    }

    [Fact]
    public void Hit_ByProtectedPlayer_EndsProtection()
    {
        var rules = Rules();
        var respawned = rules.Apply(rules.Apply(Playing(rules, Dm, A, B), new ArenaKill(A, B, "a"), 20).Snapshot, new ArenaTick(), 23).Snapshot;

        var step = rules.Apply(respawned, new ArenaHit(B), 23.5);

        Assert.True(ArenaRules.AllowsDamage(step.Snapshot, B, 23.5));
    }

    [Fact]
    public void KitRotation_ChangesKitForEveryone()
    {
        var rules = Rules();
        var playing = Playing(rules, Dm, A, B);

        var step = rules.Apply(playing, new ArenaTick(), 134);

        Assert.Equal("b", step.Snapshot.Kit!.Name);
        Assert.Equal(254, step.Snapshot.KitChangesAt);
        Assert.Equal("b", Assert.Single(step.Actions.OfType<RotateKit>()).Kit.Name);
    }

    [Fact]
    public void Respawn_AfterRotation_UsesNewKit()
    {
        var rules = Rules();
        var dead = rules.Apply(Playing(rules, Dm, A, B), new ArenaKill(A, B, "a"), 133).Snapshot;
        var rotated = rules.Apply(dead, new ArenaTick(), 134).Snapshot;

        var step = rules.Apply(rotated, new ArenaTick(), 136);

        Assert.Equal("b", Assert.Single(step.Actions.OfType<Respawn>()).Kit.Name);
    }

    [Fact]
    public void TimeLimit_BestKillsWins_ThenFewerDeaths_ThenLowestId()
    {
        var rules = Rules();
        var s = Playing(rules, Dm, A, B, C);
        s = rules.Apply(s, new ArenaKill(C, A, "a"), 20).Snapshot;
        s = rules.Apply(s, new ArenaTick(), 23).Snapshot;
        s = rules.Apply(s, new ArenaKill(B, A, "a"), 30).Snapshot;
        s = rules.Apply(s, new ArenaDeath(C), 31).Snapshot;
        s = rules.Apply(s, new ArenaTick(), 34).Snapshot;
        s = rules.Apply(s, new ArenaKill(C, A, "a"), 40).Snapshot;

        var step = rules.Apply(s, new ArenaTick(), 614);

        Assert.Equal(ArenaPhase.Ended, step.Snapshot.Phase);
        Assert.Equal(B, step.Snapshot.Winner);
    }

    [Fact]
    public void Join_DuringPlay_RespawnsOnNextTick()
    {
        var rules = Rules();
        var s = rules.Apply(Playing(rules, Dm, A, B), new ArenaJoin(C), 20).Snapshot;

        Assert.False(P(s, C).Alive);
        var step = rules.Apply(s, new ArenaTick(), 20);

        Assert.True(P(step.Snapshot, C).Alive);
        Assert.Equal(C, Assert.Single(step.Actions.OfType<Respawn>()).Id);
    }

    [Fact]
    public void Join_WhenFull_IsIgnored()
    {
        var rules = Rules();
        var full = Lobby(rules, Dm, 1, 2, 3, 4, 5, 6, 7, 8);

        Assert.Same(full, rules.Apply(full, new ArenaJoin(9), 0).Snapshot);
    }

    [Fact]
    public void Leave_LeavingOnePlayer_EndsGameWithHimAsWinner()
    {
        var rules = Rules();

        var step = rules.Apply(Playing(rules, Dm, A, B), new ArenaLeave(B), 20);

        Assert.Equal(ArenaPhase.Ended, step.Snapshot.Phase);
        Assert.Equal(A, step.Snapshot.Winner);
        Assert.Contains(new Strip(B), step.Actions);
    }

    [Fact]
    public void Leave_DuringPreparing_LeavingOne_EndsGame()
    {
        var rules = Rules();
        var preparing = rules.Apply(Lobby(rules, Dm, A, B), new ArenaStart(), 10).Snapshot;

        var step = rules.Apply(preparing, new ArenaLeave(A), 10.5);

        Assert.Equal(ArenaPhase.Ended, step.Snapshot.Phase);
        Assert.Equal(B, step.Snapshot.Winner);
    }

    [Fact]
    public void Leave_LastPlayer_ReturnsToLobby()
    {
        var rules = Rules();
        var lobby = Lobby(rules, Dm, A);

        var step = rules.Apply(lobby, new ArenaLeave(A), 1);

        Assert.Equal(ArenaPhase.Lobby, step.Snapshot.Phase);
        Assert.Empty(step.Snapshot.Players);
    }

    [Fact]
    public void Start_FromEnded_ResetsScoresAndChangesIsland()
    {
        var rules = Rules(r => r with { KillTarget = 1 }, islands: new[] { 0, 1 });
        var ended = rules.Apply(Playing(rules, Dm, A, B), new ArenaKill(A, B, "a"), 20).Snapshot;

        var step = rules.Apply(ended, new ArenaStart(), 30);

        Assert.Equal(ArenaPhase.Preparing, step.Snapshot.Phase);
        Assert.Equal(1, step.Snapshot.Island);
        Assert.Null(step.Snapshot.Winner);
        Assert.All(step.Snapshot.Players, p => Assert.Equal(0, p.Kills));
    }
}

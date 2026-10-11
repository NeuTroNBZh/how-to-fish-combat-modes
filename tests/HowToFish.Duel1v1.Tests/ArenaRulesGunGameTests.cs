using System.Linq;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;
using Xunit;
using static HowToFish.Duel1v1.Tests.ArenaTestKit;

namespace HowToFish.Duel1v1.Tests;

public class ArenaRulesGunGameTests
{
    private const GameMode Gg = GameMode.GunGame;

    private static ArenaSnapshot KillAndRespawn(ArenaRules rules, ArenaSnapshot s, ulong killer, ulong victim, string weapon, double at)
    {
        s = rules.Apply(s, new ArenaKill(killer, victim, weapon), at).Snapshot;
        return rules.Apply(s, new ArenaTick(), at + 3).Snapshot;
    }

    [Fact]
    public void Start_GivesFirstLadderLevelWithKnife()
    {
        var rules = Rules();
        var s = rules.Apply(Lobby(rules, Gg, A, B), new ArenaStart(), 10).Snapshot;

        var step = rules.Apply(s, new ArenaTick(), 11);

        Assert.Null(s.Kit);
        Assert.All(step.Actions.OfType<Respawn>(), r => Assert.Equal(new[] { "pistol", "knife" }, r.Kit.Items));
    }

    [Fact]
    public void Kill_PromotesKillerAndEquipsNextWeapon()
    {
        var rules = Rules();

        var step = rules.Apply(Playing(rules, Gg, A, B), new ArenaKill(A, B, "pistol"), 20);

        Assert.Equal(1, P(step.Snapshot, A).Level);
        var equip = Assert.Single(step.Actions.OfType<Equip>());
        Assert.Equal(A, equip.Id);
        Assert.Equal(new[] { "smg", "knife" }, equip.Kit.Items);
    }

    [Fact]
    public void KnifeKill_DemotesVictim_NotBelowZero()
    {
        var rules = Rules();
        var s = KillAndRespawn(rules, Playing(rules, Gg, A, B), B, A, "pistol", 20);

        s = rules.Apply(s, new ArenaKill(A, B, "knife"), 30).Snapshot;

        Assert.Equal(0, P(s, B).Level);
        Assert.Equal(1, P(s, A).Level);
        var again = KillAndRespawn(rules, rules.Apply(s, new ArenaTick(), 33).Snapshot, A, B, "knife", 40);
        Assert.Equal(0, P(again, B).Level);
    }

    [Fact]
    public void KnifeKillOnLastLevel_Wins()
    {
        var rules = Rules();
        var s = KillAndRespawn(rules, Playing(rules, Gg, A, B), A, B, "pistol", 20);
        s = KillAndRespawn(rules, s, A, B, "smg", 30);
        Assert.Equal(2, P(s, A).Level);

        var step = rules.Apply(s, new ArenaKill(A, B, "knife"), 40);

        Assert.Equal(ArenaPhase.Ended, step.Snapshot.Phase);
        Assert.Equal(A, step.Snapshot.Winner);
    }

    [Fact]
    public void NonKnifeKillOnLastLevel_DoesNotWin()
    {
        var rules = Rules();
        var s = KillAndRespawn(rules, Playing(rules, Gg, A, B), A, B, "pistol", 20);
        s = KillAndRespawn(rules, s, A, B, "smg", 30);

        var step = rules.Apply(s, new ArenaKill(A, B, "dynamite"), 40);

        Assert.Equal(ArenaPhase.Playing, step.Snapshot.Phase);
        Assert.Equal(2, P(step.Snapshot, A).Level);
    }

    [Fact]
    public void GunGame_HasNoTimeLimitAndNoRotation()
    {
        var rules = Rules();

        var step = rules.Apply(Playing(rules, Gg, A, B), new ArenaTick(), 100_000);

        Assert.Equal(ArenaPhase.Playing, step.Snapshot.Phase);
        Assert.Empty(step.Actions.OfType<RotateKit>());
    }

    [Fact]
    public void Death_WithoutKiller_KeepsKillsAndLevel()
    {
        var rules = Rules();
        var s = KillAndRespawn(rules, Playing(rules, Gg, A, B), A, B, "pistol", 20);

        s = rules.Apply(s, new ArenaDeath(A), 30).Snapshot;

        Assert.Equal(1, P(s, A).Kills);
        Assert.Equal(1, P(s, A).Level);
    }

    [Fact]
    public void Join_DuringPlay_StartsAtLowestLevel()
    {
        var rules = Rules();
        var s = KillAndRespawn(rules, Playing(rules, Gg, A, B), A, B, "pistol", 20);
        s = KillAndRespawn(rules, s, B, A, "pistol", 30);

        s = rules.Apply(s, new ArenaJoin(C), 40).Snapshot;

        Assert.Equal(1, P(s, C).Level);
    }

    [Fact]
    public void Respawn_UsesKitOfCurrentLevel()
    {
        var rules = Rules();
        var s = KillAndRespawn(rules, Playing(rules, Gg, A, B), A, B, "pistol", 20);
        s = rules.Apply(s, new ArenaDeath(A), 30).Snapshot;

        var step = rules.Apply(s, new ArenaTick(), 33);

        Assert.Equal(new[] { "smg", "knife" }, Assert.Single(step.Actions.OfType<Respawn>()).Kit.Items);
    }
}

using System;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class BannerTextTests
{
    private static WireState Duel(Side? roundWinner = null, bool draw = false, Side? matchWinner = null) =>
        new(DuelPhase.RoundEnd, 4, 3, 1, 0, "smg+knife", roundWinner, draw, matchWinner, false, true);

    [Fact]
    public void RoundTone_FollowsRoundWinner()
    {
        Assert.Equal(BannerTone.Good, HudFormatter.RoundTone(Duel(Side.Host), Side.Host));
        Assert.Equal(BannerTone.Bad, HudFormatter.RoundTone(Duel(Side.Guest), Side.Host));
        Assert.Equal(BannerTone.Neutral, HudFormatter.RoundTone(Duel(draw: true), Side.Host));
    }

    [Fact]
    public void MatchTone_FollowsMatchWinner()
    {
        Assert.Equal(BannerTone.Good, HudFormatter.MatchTone(Duel(matchWinner: Side.Guest), Side.Guest));
        Assert.Equal(BannerTone.Bad, HudFormatter.MatchTone(Duel(matchWinner: Side.Host), Side.Guest));
    }

    [Fact]
    public void ScoreboardRows_Duel_ShowsBothPlayersAndRound()
    {
        var rows = HudFormatter.ScoreboardRows(UiText.French, Duel(), Side.Guest, "Moi", "Bob");

        Assert.Equal(new[] { "Moi — 1 manche", "Bob — 3 manches", "Manche 4  ·  Kit : smg+knife" }, rows);
    }

    private static ArenaWire Arena(GameMode mode, ArenaPhase phase, params ArenaWirePlayer[] players) =>
        new(mode, phase, 0, "", null, players, new[] { "pistol", "smg", "knife" });

    [Fact]
    public void BannerFor_KillGivesGoodBanner()
    {
        var before = Arena(GameMode.Deathmatch, ArenaPhase.Playing, new ArenaWirePlayer(1, 2, 0, 0, true));
        var after = Arena(GameMode.Deathmatch, ArenaPhase.Playing, new ArenaWirePlayer(1, 3, 0, 0, true));

        Assert.Equal(("KILL", BannerTone.Good), ArenaHudFormatter.BannerFor(UiText.French, before, after, 1));
    }

    [Fact]
    public void BannerFor_DeathGivesBadBanner()
    {
        var before = Arena(GameMode.Deathmatch, ArenaPhase.Playing, new ArenaWirePlayer(1, 2, 0, 0, true));
        var after = Arena(GameMode.Deathmatch, ArenaPhase.Playing, new ArenaWirePlayer(1, 2, 1, 0, false));

        Assert.Equal(("ÉLIMINÉ", BannerTone.Bad), ArenaHudFormatter.BannerFor(UiText.French, before, after, 1));
    }

    [Fact]
    public void BannerFor_GunGameLevelUpNamesNextWeapon()
    {
        var before = Arena(GameMode.GunGame, ArenaPhase.Playing, new ArenaWirePlayer(1, 0, 0, 0, true));
        var after = Arena(GameMode.GunGame, ArenaPhase.Playing, new ArenaWirePlayer(1, 1, 0, 1, true));
        var last = Arena(GameMode.GunGame, ArenaPhase.Playing, new ArenaWirePlayer(1, 2, 0, 2, true));

        Assert.Equal(("NIVEAU 2 : smg", BannerTone.Good), ArenaHudFormatter.BannerFor(UiText.French, before, after, 1));
        Assert.Equal(("NIVEAU FINAL : couteau", BannerTone.Good), ArenaHudFormatter.BannerFor(UiText.French, after, last, 1));
    }

    [Fact]
    public void BannerFor_NoChangeOrOutsidePlay_GivesNothing()
    {
        var playing = Arena(GameMode.Deathmatch, ArenaPhase.Playing, new ArenaWirePlayer(1, 2, 0, 0, true));
        var ended = Arena(GameMode.Deathmatch, ArenaPhase.Ended, new ArenaWirePlayer(1, 3, 0, 0, true));

        Assert.Null(ArenaHudFormatter.BannerFor(UiText.French, playing, playing, 1));
        Assert.Null(ArenaHudFormatter.BannerFor(UiText.French, null, playing, 1));
        Assert.Null(ArenaHudFormatter.BannerFor(UiText.French, playing, ended, 1));
        Assert.Null(ArenaHudFormatter.BannerFor(UiText.French, playing, playing, 99));
    }

    [Fact]
    public void Formatters_SpeakEnglishWithEnglishCatalog()
    {
        Assert.Equal("YOU WIN THE ROUND", HudFormatter.RoundBanner(UiText.English, Duel(Side.Host), Side.Host));
        Assert.Equal(new[] { "Me — 1 round", "Bob — 3 rounds", "Round 4  ·  Kit: smg+knife" },
            HudFormatter.ScoreboardRows(UiText.English, Duel(), Side.Guest, "Me", "Bob"));
        var before = Arena(GameMode.GunGame, ArenaPhase.Playing, new ArenaWirePlayer(1, 0, 0, 0, true));
        var after = Arena(GameMode.GunGame, ArenaPhase.Playing, new ArenaWirePlayer(1, 1, 0, 1, true));
        Assert.Equal(("LEVEL 2: smg", BannerTone.Good), ArenaHudFormatter.BannerFor(UiText.English, before, after, 1));
    }
}

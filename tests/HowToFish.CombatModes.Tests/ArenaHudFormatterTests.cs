using System;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class ArenaHudFormatterTests
{
    private static string Name(ulong id) => id switch { 1 => "Moi", 2 => "Bob", _ => "Zed" };

    private static ArenaWire Wire(GameMode mode, params ArenaWirePlayer[] players) =>
        new(mode, ArenaPhase.Playing, 65_000, "smg+knife", null, players, new[] { "pistol", "smg", "knife" });

    [Fact]
    public void Ranking_Deathmatch_ByKillsThenDeathsThenId()
    {
        var w = Wire(GameMode.Deathmatch,
            new ArenaWirePlayer(3, 5, 2, 0, true), new ArenaWirePlayer(2, 5, 1, 0, true), new ArenaWirePlayer(1, 7, 9, 0, true));

        Assert.Equal(new ulong[] { 1, 2, 3 }, Array.ConvertAll(ArenaHudFormatter.Ranking(w).ToArrayCopy(), p => p.SteamId));
    }

    [Fact]
    public void Ranking_GunGame_ByLevelThenKills()
    {
        var w = Wire(GameMode.GunGame, new ArenaWirePlayer(1, 9, 0, 1, true), new ArenaWirePlayer(2, 2, 0, 2, true));

        Assert.Equal(2UL, ArenaHudFormatter.Ranking(w)[0].SteamId);
    }

    [Fact]
    public void TopLine_ShowsLocalScoreAndLeader()
    {
        var w = Wire(GameMode.Deathmatch, new ArenaWirePlayer(1, 3, 0, 0, true), new ArenaWirePlayer(2, 8, 0, 0, true));

        Assert.Equal("Toi : 3  ·  Leader : Bob 8", ArenaHudFormatter.TopLine(UiText.French, w, 1, Name));
    }

    [Fact]
    public void SubLine_Deathmatch_ShowsClockAndKit()
    {
        var w = Wire(GameMode.Deathmatch, new ArenaWirePlayer(1, 0, 0, 0, true));

        Assert.Equal("1:05  ·  Kit : smg+knife", ArenaHudFormatter.SubLine(UiText.French, w, 1, 65));
    }

    [Fact]
    public void SubLine_GunGame_ShowsLevelAndNextWeapon()
    {
        var w = Wire(GameMode.GunGame, new ArenaWirePlayer(1, 0, 0, 0, true));

        Assert.Equal("Niveau 1/3 : pistol, ensuite smg", ArenaHudFormatter.SubLine(UiText.French, w, 1, 0));
    }

    [Fact]
    public void LevelLine_LastLevel_AnnouncesKnifeFinish()
    {
        Assert.Equal("Niveau 3/3 : couteau, un kill pour gagner",
            ArenaHudFormatter.LevelLine(UiText.French, new[] { "pistol", "smg", "knife" }, 2));
    }

    [Fact]
    public void EndBanner_WinnerOrOther()
    {
        var w = Wire(GameMode.Deathmatch) with { Phase = ArenaPhase.Ended, Winner = 2 };

        Assert.Equal("BOB GAGNE", ArenaHudFormatter.EndBanner(UiText.French, w, 1, Name));
        Assert.Equal("VICTOIRE", ArenaHudFormatter.EndBanner(UiText.French, w, 2, Name));
    }

    [Fact]
    public void Row_FormatsPerMode()
    {
        var p = new ArenaWirePlayer(2, 4, 1, 1, true);

        Assert.Equal("1. Bob — 4 kills / 1 morts", ArenaHudFormatter.Row(UiText.French, 1, p, GameMode.Deathmatch, Name));
        Assert.Equal("2. Bob — niveau 2 — 4 kills", ArenaHudFormatter.Row(UiText.French, 2, p, GameMode.GunGame, Name));
    }
}

internal static class ListExtensions
{
    public static T[] ToArrayCopy<T>(this System.Collections.Generic.IReadOnlyList<T> list)
    {
        var array = new T[list.Count];
        for (int i = 0; i < list.Count; i++) array[i] = list[i];
        return array;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace HowToFish.CombatModes.Core.Arena;

public static class ArenaHudFormatter
{
    public static IReadOnlyList<ArenaWirePlayer> Ranking(ArenaWire w) => w.Mode == GameMode.GunGame
        ? w.Players.OrderByDescending(p => p.Level).ThenByDescending(p => p.Kills).ThenBy(p => p.SteamId).ToList()
        : w.Players.OrderByDescending(p => p.Kills).ThenBy(p => p.Deaths).ThenBy(p => p.SteamId).ToList();

    public static string TopLine(UiText t, ArenaWire w, ulong local, Func<ulong, string> name)
    {
        var me = w.Players.FirstOrDefault(p => p.SteamId == local);
        var leader = Ranking(w).FirstOrDefault();
        if (me == null || leader == null) return t.ModeLabel(w.Mode);
        return t.LocalAndLeader(Score(t, me, w.Mode), name(leader.SteamId), Score(t, leader, w.Mode));
    }

    public static string SubLine(UiText t, ArenaWire w, ulong local, double remainingSeconds)
    {
        if (w.Mode == GameMode.Deathmatch) return t.ClockKit(HudFormatter.Clock(remainingSeconds), w.KitName);
        var me = w.Players.FirstOrDefault(p => p.SteamId == local);
        return me == null || w.Ladder.Count == 0 ? "" : LevelLine(t, w.Ladder, me.Level);
    }

    public static string LevelLine(UiText t, IReadOnlyList<string> ladder, int level)
    {
        int last = ladder.Count - 1;
        int l = Math.Max(0, Math.Min(level, last));
        return t.LevelLine(l + 1, ladder.Count, ladder[l], l == last ? null : ladder[l + 1]);
    }

    public static string Row(UiText t, int rank, ArenaWirePlayer p, GameMode mode, Func<ulong, string> name) => mode == GameMode.GunGame
        ? t.GunGameRow(rank, name(p.SteamId), p.Level + 1, p.Kills)
        : t.DeathmatchRow(rank, name(p.SteamId), p.Kills, p.Deaths);

    public static string EndBanner(UiText t, ArenaWire w, ulong local, Func<ulong, string> name) =>
        w.Winner is not ulong winner ? t.GameOver
        : winner == local ? t.Victory
        : t.PlayerWins(name(winner));

    // Banner to show when the local player's state changes during the fight (kill, death, level up).
    public static (string Text, BannerTone Tone)? BannerFor(UiText t, ArenaWire? before, ArenaWire after, ulong local)
    {
        if (before == null || before.Phase != ArenaPhase.Playing || after.Phase != ArenaPhase.Playing) return null;
        var was = before.Players.FirstOrDefault(p => p.SteamId == local);
        var now = after.Players.FirstOrDefault(p => p.SteamId == local);
        if (was == null || now == null) return null;
        if (was.Alive && !now.Alive) return (t.Eliminated, BannerTone.Bad);
        if (after.Mode == GameMode.GunGame && now.Level > was.Level && after.Ladder.Count > 0)
            return (LevelUpText(t, after.Ladder, now.Level), BannerTone.Good);
        if (now.Kills > was.Kills) return (t.Kill, BannerTone.Good);
        return null;
    }

    private static string LevelUpText(UiText t, IReadOnlyList<string> ladder, int level) =>
        level >= ladder.Count - 1 ? t.FinalLevel : t.LevelUp(level + 1, ladder[level]);

    private static string Score(UiText t, ArenaWirePlayer p, GameMode mode) =>
        mode == GameMode.GunGame ? t.LevelScore(p.Level + 1) : p.Kills.ToString();
}

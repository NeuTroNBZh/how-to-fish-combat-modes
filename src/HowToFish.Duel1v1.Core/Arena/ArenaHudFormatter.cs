using System;
using System.Collections.Generic;
using System.Linq;

namespace HowToFish.Duel1v1.Core.Arena;

public static class ArenaHudFormatter
{
    public static IReadOnlyList<ArenaWirePlayer> Ranking(ArenaWire w) => w.Mode == GameMode.GunGame
        ? w.Players.OrderByDescending(p => p.Level).ThenByDescending(p => p.Kills).ThenBy(p => p.SteamId).ToList()
        : w.Players.OrderByDescending(p => p.Kills).ThenBy(p => p.Deaths).ThenBy(p => p.SteamId).ToList();

    public static string TopLine(ArenaWire w, ulong local, Func<ulong, string> name)
    {
        var me = w.Players.FirstOrDefault(p => p.SteamId == local);
        var leader = Ranking(w).FirstOrDefault();
        if (me == null || leader == null) return GameModes.Label(w.Mode);
        return $"Toi : {Score(me, w.Mode)}  ·  Leader : {name(leader.SteamId)} {Score(leader, w.Mode)}";
    }

    public static string SubLine(ArenaWire w, ulong local, double remainingSeconds)
    {
        if (w.Mode == GameMode.Deathmatch) return $"{HudFormatter.Clock(remainingSeconds)}  ·  Kit : {w.KitName}";
        var me = w.Players.FirstOrDefault(p => p.SteamId == local);
        return me == null || w.Ladder.Count == 0 ? "" : LevelLine(w.Ladder, me.Level);
    }

    public static string LevelLine(IReadOnlyList<string> ladder, int level)
    {
        int last = ladder.Count - 1;
        int l = Math.Max(0, Math.Min(level, last));
        return l == last
            ? $"Niveau {l + 1}/{ladder.Count} : couteau, un kill pour gagner"
            : $"Niveau {l + 1}/{ladder.Count} : {ladder[l]}, ensuite {ladder[l + 1]}";
    }

    public static string Row(int rank, ArenaWirePlayer p, GameMode mode, Func<ulong, string> name) => mode == GameMode.GunGame
        ? $"{rank}. {name(p.SteamId)} — niveau {p.Level + 1} — {p.Kills} kills"
        : $"{rank}. {name(p.SteamId)} — {p.Kills} kills / {p.Deaths} morts";

    public static string EndBanner(ArenaWire w, ulong local, Func<ulong, string> name) =>
        w.Winner is not ulong winner ? "PARTIE TERMINÉE"
        : winner == local ? "VICTOIRE"
        : $"{name(winner).ToUpperInvariant()} GAGNE";

    private static string Score(ArenaWirePlayer p, GameMode mode) =>
        mode == GameMode.GunGame ? $"niveau {p.Level + 1}" : p.Kills.ToString();
}

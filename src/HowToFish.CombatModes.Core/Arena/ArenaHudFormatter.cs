using System;
using System.Collections.Generic;
using System.Linq;

namespace HowToFish.CombatModes.Core.Arena;

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

    // Bannière à afficher quand l'état du joueur local change pendant le combat (kill, mort, montée de niveau).
    public static (string Text, BannerTone Tone)? BannerFor(ArenaWire? before, ArenaWire after, ulong local)
    {
        if (before == null || before.Phase != ArenaPhase.Playing || after.Phase != ArenaPhase.Playing) return null;
        var was = before.Players.FirstOrDefault(p => p.SteamId == local);
        var now = after.Players.FirstOrDefault(p => p.SteamId == local);
        if (was == null || now == null) return null;
        if (was.Alive && !now.Alive) return ("ÉLIMINÉ", BannerTone.Bad);
        if (after.Mode == GameMode.GunGame && now.Level > was.Level && after.Ladder.Count > 0) return (LevelUpText(after.Ladder, now.Level), BannerTone.Good);
        if (now.Kills > was.Kills) return ("KILL", BannerTone.Good);
        return null;
    }

    private static string LevelUpText(IReadOnlyList<string> ladder, int level)
    {
        int last = ladder.Count - 1;
        return level >= last ? "NIVEAU FINAL : couteau" : $"NIVEAU {level + 1} : {ladder[level]}";
    }

    private static string Score(ArenaWirePlayer p, GameMode mode) =>
        mode == GameMode.GunGame ? $"niveau {p.Level + 1}" : p.Kills.ToString();
}

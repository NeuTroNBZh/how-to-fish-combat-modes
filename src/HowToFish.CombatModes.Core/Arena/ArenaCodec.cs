using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HowToFish.CombatModes.Core.Arena;

public static class ArenaCodec
{
    public const int ProtocolVersion = 2;
    private const string Kind = "ARENA";
    private const int FieldCount = 10;
    private const int PlayerFieldCount = 5;
    private static readonly string Header = $"{DuelCodec.Prefix}|{ProtocolVersion}|{Kind}|";
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static bool IsArena(string text) => text.StartsWith(Header, StringComparison.Ordinal);

    public static string Encode(ArenaWire w) => string.Join("|",
        DuelCodec.Prefix, ProtocolVersion.ToString(Inv), Kind, ((int)w.Mode).ToString(Inv), ((int)w.Phase).ToString(Inv),
        w.RemainingMs.ToString(Inv), w.KitName, (w.Winner ?? 0UL).ToString(Inv),
        string.Join(",", w.Players.Select(EncodePlayer)), string.Join(";", w.Ladder));

    private static string EncodePlayer(ArenaWirePlayer p) => string.Join(":",
        p.SteamId.ToString(Inv), p.Kills.ToString(Inv), p.Deaths.ToString(Inv), p.Level.ToString(Inv), p.Alive ? "1" : "0");

    public static bool TryDecode(string text, out ArenaWire? wire, out string error)
    {
        wire = null;
        var p = text.Split('|');
        if (!IsArena(text) || p.Length != FieldCount) return Fail($"message arène mal formé ({p.Length} champs)", out error);
        if (!TryInt(p[3], out var mode) || !Enum.IsDefined(typeof(GameMode), mode) || mode == (int)GameMode.Duel)
            return Fail($"mode invalide '{p[3]}'", out error);
        if (!TryInt(p[4], out var phase) || !Enum.IsDefined(typeof(ArenaPhase), phase)) return Fail($"phase invalide '{p[4]}'", out error);
        if (!TryInt(p[5], out var remaining) || remaining < 0) return Fail($"temps invalide '{p[5]}'", out error);
        if (!ulong.TryParse(p[7], NumberStyles.None, Inv, out var winner)) return Fail($"gagnant invalide '{p[7]}'", out error);
        if (!TryPlayers(p[8], out var players, out error)) return false;
        var ladder = p[9].Length == 0 ? Array.Empty<string>() : p[9].Split(';');
        wire = new ArenaWire((GameMode)mode, (ArenaPhase)phase, remaining, p[6], winner == 0 ? null : winner, players, ladder);
        return true;
    }

    private static bool TryPlayers(string field, out IReadOnlyList<ArenaWirePlayer> players, out string error)
    {
        var list = new List<ArenaWirePlayer>();
        players = list;
        if (field.Length == 0)
        {
            error = "";
            return true;
        }
        var entries = field.Split(',');
        if (entries.Length > GameModes.ArenaMaxMembers) return Fail($"{entries.Length} joueurs, maximum {GameModes.ArenaMaxMembers}", out error);
        foreach (var entry in entries)
        {
            if (!TryPlayer(entry, out var player)) return Fail($"joueur invalide '{entry}'", out error);
            list.Add(player!);
        }
        error = "";
        return true;
    }

    private static bool TryPlayer(string entry, out ArenaWirePlayer? player)
    {
        player = null;
        var f = entry.Split(':');
        if (f.Length != PlayerFieldCount || !ulong.TryParse(f[0], NumberStyles.None, Inv, out var id)) return false;
        if (!TryInt(f[1], out var kills) || !TryInt(f[2], out var deaths) || !TryInt(f[3], out var level)) return false;
        if (deaths < 0 || level < 0 || (f[4] != "0" && f[4] != "1")) return false;
        player = new ArenaWirePlayer(id, kills, deaths, level, f[4] == "1");
        return true;
    }

    private static bool TryInt(string text, out int value) =>
        int.TryParse(text, NumberStyles.AllowLeadingSign, Inv, out value);

    private static bool Fail(string reason, out string error)
    {
        error = reason;
        return false;
    }
}

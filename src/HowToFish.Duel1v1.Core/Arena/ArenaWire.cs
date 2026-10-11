using System;
using System.Collections.Generic;
using System.Linq;

namespace HowToFish.Duel1v1.Core.Arena;

public sealed record ArenaWirePlayer(ulong SteamId, int Kills, int Deaths, int Level, bool Alive);

public sealed record ArenaWire(
    GameMode Mode, ArenaPhase Phase, int RemainingMs, string KitName, ulong? Winner,
    IReadOnlyList<ArenaWirePlayer> Players, IReadOnlyList<string> Ladder)
{
    public static ArenaWire From(ArenaSnapshot s, double now, IReadOnlyList<string> ladder)
    {
        var end = s.Phase != ArenaPhase.Playing ? s.PhaseEndsAt
            : s.Mode == GameMode.Deathmatch ? s.EndsAt : now;
        var players = s.Players.Select(p => new ArenaWirePlayer(p.SteamId, p.Kills, p.Deaths, p.Level, p.Alive)).ToList();
        return new ArenaWire(s.Mode, s.Phase, (int)Math.Round(Math.Max(0, end - now) * 1000), s.Kit?.Name ?? "",
            s.Winner, players, s.Mode == GameMode.GunGame ? ladder : Array.Empty<string>());
    }
}

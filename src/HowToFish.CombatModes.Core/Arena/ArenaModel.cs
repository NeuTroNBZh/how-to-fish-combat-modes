using System;
using System.Collections.Generic;
using System.Linq;

namespace HowToFish.CombatModes.Core.Arena;

public enum ArenaPhase
{
    Lobby,
    Preparing,
    Countdown,
    Playing,
    Ended,
}

public sealed record ArenaPlayer(
    ulong SteamId, int Kills, int Deaths, int Level, bool Alive, double RespawnAt, double ProtectedUntil);

public sealed record ArenaSnapshot(
    GameMode Mode, ArenaPhase Phase, IReadOnlyList<ArenaPlayer> Players, Kit? Kit, double KitChangesAt,
    double PhaseEndsAt, double EndsAt, int Island, ulong? Winner)
{
    public static ArenaSnapshot Lobby(GameMode mode) =>
        new(mode, ArenaPhase.Lobby, Array.Empty<ArenaPlayer>(), null, 0, 0, 0, -1, null);

    public bool IsLive => Phase is ArenaPhase.Preparing or ArenaPhase.Countdown or ArenaPhase.Playing;

    public ArenaPlayer? Find(ulong id) => Players.FirstOrDefault(p => p.SteamId == id);

    public ArenaSnapshot WithPlayer(ArenaPlayer player) => this with
    {
        Players = Players.Where(p => p.SteamId != player.SteamId).Append(player).OrderBy(p => p.SteamId).ToList(),
    };

    public ArenaSnapshot Without(ulong id) => this with { Players = Players.Where(p => p.SteamId != id).ToList() };
}

public abstract record ArenaEvent;
public sealed record ArenaStart : ArenaEvent;
public sealed record ArenaJoin(ulong Id) : ArenaEvent;
public sealed record ArenaLeave(ulong Id) : ArenaEvent;
public sealed record ArenaKill(ulong Killer, ulong Victim, string Weapon) : ArenaEvent;
public sealed record ArenaDeath(ulong Victim) : ArenaEvent;
public sealed record ArenaHit(ulong Attacker) : ArenaEvent;
public sealed record ArenaTick : ArenaEvent;

public abstract record ArenaAction;
public sealed record PrepareIsland(int Island) : ArenaAction;
public sealed record Respawn(ulong Id, Kit Kit) : ArenaAction;
public sealed record Equip(ulong Id, Kit Kit) : ArenaAction;
public sealed record RotateKit(Kit Kit) : ArenaAction;
public sealed record Strip(ulong Id) : ArenaAction;
public sealed record StartFight : ArenaAction;
public sealed record EndGame(ulong Winner) : ArenaAction;

public sealed record ArenaStep(ArenaSnapshot Snapshot, IReadOnlyList<ArenaAction> Actions);

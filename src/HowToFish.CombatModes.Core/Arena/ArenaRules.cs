using System;
using System.Collections.Generic;
using System.Linq;

namespace HowToFish.CombatModes.Core.Arena;

public sealed class ArenaRules
{
    public const int MinPlayers = 2;

    private readonly ArenaSettings _settings;
    private readonly IRandomSource _random;

    public ArenaRules(ArenaSettings settings, IRandomSource random)
    {
        _settings = settings;
        _random = random;
    }

    public ArenaStep Apply(ArenaSnapshot s, ArenaEvent e, double now) => e switch
    {
        ArenaStart => CanStart(s) ? Start(s, now) : Same(s),
        ArenaJoin join => Join(s, join.Id, now),
        ArenaLeave leave => Leave(s, leave.Id),
        ArenaKill kill when s.Phase == ArenaPhase.Playing => OnKill(s, kill, now),
        ArenaDeath death when s.Phase == ArenaPhase.Playing => OnDeath(s, death.Victim, now, penalize: true),
        ArenaHit hit => Unprotect(s, hit.Attacker),
        ArenaTick => OnTick(s, now),
        _ => Same(s),
    };

    public static bool AllowsDamage(ArenaSnapshot s, ulong victim, double now) =>
        s.Phase == ArenaPhase.Playing && s.Find(victim) is { Alive: true } v && now >= v.ProtectedUntil;

    private Kit KitFor(ArenaSnapshot s, ArenaPlayer p) =>
        s.Mode == GameMode.GunGame ? _settings.Ladder.KitAt(p.Level) : s.Kit!;

    private static bool CanStart(ArenaSnapshot s) =>
        (s.Phase is ArenaPhase.Lobby or ArenaPhase.Ended) && s.Players.Count >= MinPlayers;

    private ArenaStep Start(ArenaSnapshot s, double now)
    {
        int? previous = s.Island >= 0 ? s.Island : null;
        var island = IslandPicker.Pick(_settings.Islands, previous, _random);
        var kit = s.Mode == GameMode.Deathmatch ? KitPicker.Pick(_settings.Kits, null, _random) : null;
        var players = s.Players.Select(p => new ArenaPlayer(p.SteamId, 0, 0, 0, false, 0, 0)).ToList();
        var next = s with
        {
            Phase = ArenaPhase.Preparing, Players = players, Kit = kit, Island = island, Winner = null,
            PhaseEndsAt = now + ArenaSettings.PrepareSeconds, EndsAt = 0, KitChangesAt = 0,
        };
        return Step(next, new PrepareIsland(island));
    }

    private ArenaStep OnTick(ArenaSnapshot s, double now) => s.Phase switch
    {
        ArenaPhase.Preparing => now >= s.PhaseEndsAt ? BeginCountdown(s, now) : Same(s),
        ArenaPhase.Countdown => Chain(Respawns(s, now), r => now >= r.PhaseEndsAt ? BeginPlay(r, now) : Same(r)),
        ArenaPhase.Playing => Chain(Respawns(s, now), r => Clock(r, now)),
        _ => Same(s),
    };

    private ArenaStep BeginCountdown(ArenaSnapshot s, double now)
    {
        var players = s.Players.Select(p => p with { Alive = true, RespawnAt = 0, ProtectedUntil = 0 }).ToList();
        var next = s with { Phase = ArenaPhase.Countdown, Players = players, PhaseEndsAt = now + _settings.CountdownSeconds };
        return new ArenaStep(next, players.Select(p => (ArenaAction)new Respawn(p.SteamId, KitFor(next, p))).ToList());
    }

    private ArenaStep BeginPlay(ArenaSnapshot s, double now) => Step(s with
    {
        Phase = ArenaPhase.Playing,
        EndsAt = s.Mode == GameMode.Deathmatch ? now + _settings.TimeLimitSeconds : 0,
        KitChangesAt = now + _settings.KitRotationSeconds,
    }, new StartFight());

    private ArenaStep Clock(ArenaSnapshot s, double now)
    {
        if (s.Mode != GameMode.Deathmatch || s.Players.Count == 0) return Same(s);
        if (now >= s.EndsAt) return End(s, Leader(s), Array.Empty<ArenaAction>());
        if (now < s.KitChangesAt) return Same(s);
        var kit = KitPicker.Pick(_settings.Kits, s.Kit, _random);
        return Step(s with { Kit = kit, KitChangesAt = s.KitChangesAt + _settings.KitRotationSeconds }, new RotateKit(kit));
    }

    private static ulong Leader(ArenaSnapshot s) =>
        s.Players.OrderByDescending(p => p.Kills).ThenBy(p => p.Deaths).ThenBy(p => p.SteamId).First().SteamId;

    private ArenaStep Respawns(ArenaSnapshot s, double now)
    {
        var next = s;
        var actions = new List<ArenaAction>();
        foreach (var p in s.Players.Where(p => !p.Alive && p.RespawnAt <= now))
        {
            var alive = p with { Alive = true, ProtectedUntil = now + _settings.SpawnProtectionSeconds };
            next = next.WithPlayer(alive);
            actions.Add(new Respawn(p.SteamId, KitFor(next, alive)));
        }
        return actions.Count == 0 ? Same(s) : new ArenaStep(next, actions);
    }

    private static ArenaStep Join(ArenaSnapshot s, ulong id, double now)
    {
        if (s.Find(id) != null || s.Players.Count >= ArenaSettings.MaxPlayers) return Same(s);
        // Un retardataire en course à l'armement part du niveau le plus bas pour ne pas être distancé.
        int level = s.Mode == GameMode.GunGame && s.IsLive && s.Players.Count > 0 ? s.Players.Min(p => p.Level) : 0;
        return Same(s.WithPlayer(new ArenaPlayer(id, 0, 0, level, false, now, 0)));
    }

    private static ArenaStep Leave(ArenaSnapshot s, ulong id)
    {
        if (s.Find(id) == null) return Same(s);
        var next = s.Without(id);
        if (next.Players.Count == 0) return Same(ArenaSnapshot.Lobby(s.Mode) with { Island = s.Island });
        if (s.IsLive && next.Players.Count == 1) return End(next, next.Players[0].SteamId, new ArenaAction[] { new Strip(id) });
        return Step(next, new Strip(id));
    }

    // Pénalité seulement pour une mort sans adversaire (chute, noyade, suicide) : un tueur parti ne doit rien coûter.
    private ArenaStep OnDeath(ArenaSnapshot s, ulong id, double now, bool penalize)
    {
        if (s.Find(id) is not { Alive: true } p) return Same(s);
        var dead = Dead(p, now) with { Kills = penalize && s.Mode == GameMode.Deathmatch ? p.Kills - 1 : p.Kills };
        return Step(s.WithPlayer(dead), new Strip(id));
    }

    private ArenaStep OnKill(ArenaSnapshot s, ArenaKill k, double now)
    {
        if (k.Killer == k.Victim) return OnDeath(s, k.Victim, now, penalize: true);
        var killer = s.Find(k.Killer);
        if (killer == null) return OnDeath(s, k.Victim, now, penalize: false);
        if (s.Find(k.Victim) is not { Alive: true } victim) return Same(s);
        var actions = new List<ArenaAction> { new Strip(victim.SteamId) };
        var dead = Dead(victim, now);
        if (s.Mode == GameMode.GunGame) return GunGameKill(s, killer, dead, k.Weapon == GunGameLadder.FinalWeapon, actions);
        var scorer = killer with { Kills = killer.Kills + 1 };
        var next = s.WithPlayer(dead).WithPlayer(scorer);
        return scorer.Kills >= _settings.KillTarget ? End(next, scorer.SteamId, actions) : new ArenaStep(next, actions);
    }

    private ArenaStep GunGameKill(ArenaSnapshot s, ArenaPlayer killer, ArenaPlayer dead, bool knife, List<ArenaAction> actions)
    {
        var last = _settings.Ladder.LastLevel;
        var victim = knife ? dead with { Level = Math.Max(0, dead.Level - 1) } : dead;
        var promoted = killer with { Kills = killer.Kills + 1, Level = Math.Min(killer.Level + 1, last) };
        var next = s.WithPlayer(victim).WithPlayer(promoted);
        if (knife && killer.Level >= last) return End(next, killer.SteamId, actions);
        if (promoted.Alive && promoted.Level != killer.Level)
            actions.Add(new Equip(promoted.SteamId, _settings.Ladder.KitAt(promoted.Level)));
        return new ArenaStep(next, actions);
    }

    private ArenaPlayer Dead(ArenaPlayer p, double now) =>
        p with { Deaths = p.Deaths + 1, Alive = false, RespawnAt = now + _settings.RespawnSeconds, ProtectedUntil = 0 };

    private static ArenaStep Unprotect(ArenaSnapshot s, ulong id) =>
        s.Find(id) is { ProtectedUntil: > 0 } p ? Same(s.WithPlayer(p with { ProtectedUntil = 0 })) : Same(s);

    private static ArenaStep End(ArenaSnapshot s, ulong winner, IEnumerable<ArenaAction> actions) =>
        new(s with { Phase = ArenaPhase.Ended, Winner = winner }, actions.Append(new EndGame(winner)).ToList());

    private static ArenaStep Chain(ArenaStep first, Func<ArenaSnapshot, ArenaStep> next)
    {
        var second = next(first.Snapshot);
        return new ArenaStep(second.Snapshot, first.Actions.Concat(second.Actions).ToList());
    }

    private static ArenaStep Step(ArenaSnapshot s, ArenaAction action) => new(s, new[] { action });

    private static ArenaStep Same(ArenaSnapshot s) => new(s, Array.Empty<ArenaAction>());
}

using System;
using System.Linq;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;

namespace HowToFish.Duel1v1.Tests;

internal static class ArenaTestKit
{
    public const ulong A = 100, B = 200, C = 300;

    public static ArenaSettings Settings(Func<RawArenaSettings, RawArenaSettings>? tweak = null, int[]? islands = null)
    {
        var shared = DuelSettings.FromRaw(DuelSettings.Defaults with { Kits = "a;b;c" }, out _);
        var raw = tweak?.Invoke(ArenaSettings.Defaults) ?? ArenaSettings.Defaults;
        var s = ArenaSettings.FromRaw(raw with { Ladder = "pistol;smg;knife" }, shared, out _);
        return islands == null ? s : s with { Islands = islands };
    }

    public static ArenaRules Rules(Func<RawArenaSettings, RawArenaSettings>? tweak = null, int[]? islands = null) =>
        new(Settings(tweak, islands), new FakeRandom());

    public static ArenaSnapshot Lobby(ArenaRules rules, GameMode mode, params ulong[] ids) =>
        ids.Aggregate(ArenaSnapshot.Lobby(mode), (s, id) => rules.Apply(s, new ArenaJoin(id), 0).Snapshot);

    // Préparation à t=10, compte à rebours à t=11, combat à t=14.
    public static ArenaSnapshot Playing(ArenaRules rules, GameMode mode, params ulong[] ids)
    {
        var s = rules.Apply(Lobby(rules, mode, ids), new ArenaStart(), 10).Snapshot;
        s = rules.Apply(s, new ArenaTick(), 11).Snapshot;
        return rules.Apply(s, new ArenaTick(), 14).Snapshot;
    }

    public static ArenaPlayer P(ArenaSnapshot s, ulong id) => s.Find(id)!;
}

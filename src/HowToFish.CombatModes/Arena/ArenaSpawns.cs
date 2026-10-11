using System.Collections.Generic;
using System.Linq;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;
using HowToFish.CombatModes.Host;
using UnityEngine;
using NVector3 = System.Numerics.Vector3;

namespace HowToFish.CombatModes.Arena;

internal sealed class ArenaSpawns
{
    private const int CandidateCount = 12;
    private const int MinCandidates = 4;
    private const float RingFactor = 0.6f;
    private const float FallbackHeight = 2f;
    // Un joueur téléporté n'a pas encore bougé côté serveur : on garde les spawns récents comme occupés.
    private const float RecentSeconds = 3f;

    private readonly IReadOnlyList<NVector3> _points;
    private readonly NVector3 _center;
    private readonly List<(NVector3 Position, float At)> _recent = new();

    private ArenaSpawns(IReadOnlyList<NVector3> points, NVector3 center)
    {
        _points = points;
        _center = center;
    }

    public static ArenaSpawns Empty { get; } = new(new[] { NVector3.Zero }, NVector3.Zero);

    public static ArenaSpawns Compute(Vector3 center, float radius)
    {
        var c = GameActions.ToNumerics(center);
        var grounded = new List<NVector3>();
        foreach (var point in SpawnPicker.Ring(c, radius * RingFactor, CandidateCount))
            if (GameActions.TryGround(GameActions.ToUnity(point), out var ground)) grounded.Add(GameActions.ToNumerics(ground));
        if (grounded.Count >= MinCandidates) return new ArenaSpawns(grounded, c);
        CombatModesPlugin.Log.LogWarning($"Seulement {grounded.Count} spawns au sol, centre de l'île utilisé");
        return new ArenaSpawns(new[] { c + new NVector3(0, FallbackHeight, 0) }, c);
    }

    public SpawnPoint PickFor(Player self, float now)
    {
        _recent.RemoveAll(r => now - r.At > RecentSeconds);
        var occupied = PlayerManager.AlivePlayers
            .Where(p => p != null && p != self)
            .Select(p => GameActions.ToNumerics(p.Transform.position))
            .Concat(_recent.Select(r => r.Position))
            .ToList();
        var point = _points[SpawnPicker.Farthest(_points, occupied)];
        _recent.Add((point, now));
        return new SpawnPoint(point, ArenaGeometry.YawTowards(point, _center));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HowToFish.CombatModes.Core.Arena;

public static class SpawnPicker
{
    public static IReadOnlyList<Vector3> Ring(Vector3 center, float radius, int count) =>
        Enumerable.Range(0, count).Select(i =>
        {
            var angle = 2 * Math.PI * i / count;
            return new Vector3(center.X + radius * (float)Math.Cos(angle), center.Y, center.Z + radius * (float)Math.Sin(angle));
        }).ToList();

    // Le meilleur spawn est celui dont le joueur le plus proche est le plus loin ; égalité → premier candidat.
    public static int Farthest(IReadOnlyList<Vector3> candidates, IReadOnlyList<Vector3> occupied)
    {
        if (candidates.Count == 0) throw new ArgumentException("Au moins un spawn est requis.", nameof(candidates));
        if (occupied.Count == 0) return 0;
        int best = 0;
        float bestDistance = -1f;
        for (int i = 0; i < candidates.Count; i++)
        {
            var nearest = occupied.Min(o => Vector3.DistanceSquared(candidates[i], o));
            if (nearest <= bestDistance) continue;
            best = i;
            bestDistance = nearest;
        }
        return best;
    }
}

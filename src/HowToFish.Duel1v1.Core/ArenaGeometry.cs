using System;
using System.Numerics;

namespace HowToFish.Duel1v1.Core;

public sealed record SpawnPoint(Vector3 Position, float Yaw);

public static class ArenaGeometry
{
    public const float FallDepth = 10f;

    public static (SpawnPoint A, SpawnPoint B) AutoSpawns(Vector3 center, float radius)
    {
        var offset = new Vector3(radius / 2f, 0, 0);
        return Facing(center - offset, center + offset);
    }

    public static (SpawnPoint A, SpawnPoint B) Facing(Vector3 a, Vector3 b) =>
        (new SpawnPoint(a, YawTowards(a, b)), new SpawnPoint(b, YawTowards(b, a)));

    public static SpawnPoint For(Side side, bool hostAtSpawnA, (SpawnPoint A, SpawnPoint B) spawns) =>
        (side == Side.Host) == hostAtSpawnA ? spawns.A : spawns.B;

    public static bool IsOutside(Vector3 position, Vector3 center, float radius)
    {
        var dx = position.X - center.X;
        var dz = position.Z - center.Z;
        return dx * dx + dz * dz > radius * radius || position.Y < center.Y - FallDepth;
    }

    public static float YawTowards(Vector3 from, Vector3 to)
    {
        var degrees = (float)(Math.Atan2(to.X - from.X, to.Z - from.Z) * 180.0 / Math.PI);
        return (degrees + 360f) % 360f;
    }
}

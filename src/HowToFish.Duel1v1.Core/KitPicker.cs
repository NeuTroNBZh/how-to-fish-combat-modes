using System;
using System.Collections.Generic;
using System.Linq;

namespace HowToFish.Duel1v1.Core;

public interface IRandomSource
{
    int Next(int maxExclusive);
}

public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random;
    public SystemRandomSource(Random random) => _random = random;
    public int Next(int maxExclusive) => _random.Next(maxExclusive);
}

public static class KitPicker
{
    public static Kit Pick(IReadOnlyList<Kit> kits, Kit? previous, IRandomSource random)
    {
        if (kits.Count == 0) throw new ArgumentException("Au moins un kit est requis.", nameof(kits));
        if (kits.Count == 1 || previous is null) return kits[random.Next(kits.Count)];
        var candidates = kits.Where(k => k.Name != previous.Name).ToList();
        return candidates.Count == 0 ? kits[random.Next(kits.Count)] : candidates[random.Next(candidates.Count)];
    }
}

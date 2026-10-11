using System;
using System.Collections.Generic;
using System.Linq;

namespace HowToFish.CombatModes.Core;

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
    public static Kit Pick(IReadOnlyList<Kit> kits, Kit? previous, IRandomSource random) =>
        RandomPicker.PickAvoiding(kits, k => previous != null && k.Name == previous.Name, random, nameof(kits));
}

public static class IslandPicker
{
    public static int Pick(IReadOnlyList<int> islands, int? previous, IRandomSource random) =>
        RandomPicker.PickAvoiding(islands, i => i == previous, random, nameof(islands));
}

internal static class RandomPicker
{
    public static T PickAvoiding<T>(IReadOnlyList<T> items, Func<T, bool> isPrevious, IRandomSource random, string paramName)
    {
        if (items.Count == 0) throw new ArgumentException("At least one element is required.", paramName);
        var candidates = items.Where(i => !isPrevious(i)).ToList();
        return candidates.Count == 0 ? items[random.Next(items.Count)] : candidates[random.Next(candidates.Count)];
    }
}

using System.Collections.Generic;
using HowToFish.CombatModes.Core;

namespace HowToFish.CombatModes.Tests;

public sealed class FakeRandom : IRandomSource
{
    private readonly Queue<int> _values;
    public FakeRandom(params int[] values) => _values = new Queue<int>(values);
    public int Next(int maxExclusive) => _values.Count == 0 ? 0 : _values.Dequeue() % maxExclusive;
}

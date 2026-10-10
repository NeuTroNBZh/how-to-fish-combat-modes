using System;
using HowToFish.Duel1v1.Core;
using Xunit;

namespace HowToFish.Duel1v1.Tests;

public class KitPickerTests
{
    private static readonly Kit A = new("a", new[] { "a" });
    private static readonly Kit B = new("b", new[] { "b" });
    private static readonly Kit C = new("c", new[] { "c" });

    [Fact]
    public void Pick_SingleKit_AlwaysReturnsIt()
    {
        Assert.Equal(A, KitPicker.Pick(new[] { A }, A, new FakeRandom(5)));
    }

    [Fact]
    public void Pick_NoPrevious_UsesRandomIndex()
    {
        Assert.Equal(C, KitPicker.Pick(new[] { A, B, C }, null, new FakeRandom(2)));
    }

    [Fact]
    public void Pick_ExcludesPreviousKit()
    {
        Assert.Equal(C, KitPicker.Pick(new[] { A, B, C }, B, new FakeRandom(1)));
    }

    [Fact]
    public void Pick_NoKits_Throws()
    {
        Assert.Throws<ArgumentException>(() => KitPicker.Pick(Array.Empty<Kit>(), null, new FakeRandom()));
    }
}

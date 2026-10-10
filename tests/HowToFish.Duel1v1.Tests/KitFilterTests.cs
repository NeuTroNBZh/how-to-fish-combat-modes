using System.Linq;
using HowToFish.Duel1v1.Core;
using Xunit;

namespace HowToFish.Duel1v1.Tests;

public class KitFilterTests
{
    [Fact]
    public void KeepAvailable_RemovesKitsWithUnknownItems()
    {
        var kits = KitParser.Parse("pistol+knife;laser+knife;shotgun", out _)!;
        var known = new[] { "pistol", "knife", "shotgun" };

        var kept = KitFilter.KeepAvailable(kits, known.Contains, out var missing);

        Assert.Equal(new[] { "pistol+knife", "shotgun" }, kept.Select(k => k.Name));
        Assert.Equal(new[] { "laser" }, missing);
    }

    [Fact]
    public void KeepAvailable_NothingKnown_ReturnsEmpty()
    {
        var kits = KitParser.Parse("laser", out _)!;

        var kept = KitFilter.KeepAvailable(kits, _ => false, out var missing);

        Assert.Empty(kept);
        Assert.Equal(new[] { "laser" }, missing);
    }
}

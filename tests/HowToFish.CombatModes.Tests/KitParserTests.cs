using System.Linq;
using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class KitParserTests
{
    [Fact]
    public void Parse_SplitsKitsAndItems()
    {
        var kits = KitParser.Parse("pistol+knife;shotgun", out var error);

        Assert.NotNull(kits);
        Assert.Equal("", error);
        Assert.Equal(2, kits!.Count);
        Assert.Equal("pistol+knife", kits[0].Name);
        Assert.Equal(new[] { "pistol", "knife" }, kits[0].Items);
        Assert.Equal(new[] { "shotgun" }, kits[1].Items);
    }

    [Fact]
    public void Parse_NormalizesCaseSpacesAndEmptyKits()
    {
        var kits = KitParser.Parse(" Pistol + Knife ; ; SHOT gun ", out _);

        Assert.Equal(new[] { "pistol+knife", "shotgun" }, kits!.Select(k => k.Name));
    }

    [Theory]
    [InlineData("", "no kit")]
    [InlineData("   ;  ", "no kit")]
    [InlineData("pistol|knife", "|")]
    [InlineData("pistol++knife", "empty item")]
    public void Parse_RejectsInvalidText(string text, string expectedErrorPart)
    {
        var kits = KitParser.Parse(text, out var error);

        Assert.Null(kits);
        Assert.Contains(expectedErrorPart, error);
    }
}

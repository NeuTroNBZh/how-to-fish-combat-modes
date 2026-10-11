using HowToFish.CombatModes.Core.Arena;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class GunGameLadderTests
{
    [Fact]
    public void Parse_Default_HasSevenLevelsEndingWithKnife()
    {
        var ladder = GunGameLadder.Parse(ArenaSettings.DefaultLadder, out _)!;

        Assert.Equal(7, ladder.Weapons.Count);
        Assert.Equal(6, ladder.LastLevel);
        Assert.Equal("knife", ladder.Weapons[6]);
    }

    [Fact]
    public void DefaultLadder_StartsWithSniperThenStrongestWeapons()
    {
        var ladder = GunGameLadder.Parse(ArenaSettings.DefaultLadder, out _)!;

        Assert.Equal(new[] { "sniperrifle", "assaultrifle", "smg", "shotgun", "bow", "pistol", "knife" }, ladder.Weapons);
    }

    [Fact]
    public void Parse_NormalizesCaseAndSpaces()
    {
        var ladder = GunGameLadder.Parse(" Pistol ; KNIFE ;", out _)!;

        Assert.Equal(new[] { "pistol", "knife" }, ladder.Weapons);
    }

    [Theory]
    [InlineData("knife")]
    [InlineData("pistol;smg")]
    [InlineData("")]
    public void Parse_Invalid_Fails(string text)
    {
        Assert.Null(GunGameLadder.Parse(text, out var error));
        Assert.NotEmpty(error);
    }

    [Fact]
    public void KitAt_Intermediate_GivesWeaponPlusKnife()
    {
        var kit = GunGameLadder.Parse("pistol;knife", out _)!.KitAt(0);

        Assert.Equal("pistol+knife", kit.Name);
        Assert.Equal(new[] { "pistol", "knife" }, kit.Items);
    }

    [Fact]
    public void KitAt_Last_GivesKnifeOnly()
    {
        var kit = GunGameLadder.Parse("pistol;knife", out _)!.KitAt(1);

        Assert.Equal(new[] { "knife" }, kit.Items);
    }

    [Theory]
    [InlineData(-1, "pistol")]
    [InlineData(99, "knife")]
    public void KitAt_OutOfRange_IsClamped(int level, string firstItem)
    {
        var kit = GunGameLadder.Parse("pistol;knife", out _)!.KitAt(level);

        Assert.Equal(firstItem, kit.Items[0]);
    }

    [Fact]
    public void KeepAvailable_RemovesMissingWeapons()
    {
        var ladder = GunGameLadder.Parse("pistol;laser;knife", out _)!;

        var kept = ladder.KeepAvailable(w => w != "laser", out var missing);

        Assert.Equal(new[] { "pistol", "knife" }, kept!.Weapons);
        Assert.Equal(new[] { "laser" }, missing);
    }

    [Fact]
    public void KeepAvailable_KnifeMissing_ReturnsNull()
    {
        var ladder = GunGameLadder.Parse("pistol;smg;knife", out _)!;

        Assert.Null(ladder.KeepAvailable(w => w != "knife", out _));
    }
}

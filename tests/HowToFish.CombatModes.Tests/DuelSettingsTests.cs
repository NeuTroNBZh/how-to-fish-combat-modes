using System.Linq;
using System.Numerics;
using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class DuelSettingsTests
{
    [Fact]
    public void FromRaw_Defaults_AreValidWithoutWarnings()
    {
        var settings = DuelSettings.FromRaw(DuelSettings.Defaults, out var warnings);

        Assert.Empty(warnings);
        Assert.Equal(5, settings.RoundsToWin);
        Assert.Equal(90, settings.RoundTimeSeconds);
        Assert.Equal(3, settings.CountdownSeconds);
        Assert.Equal(60f, settings.ArenaRadius);
        Assert.NotEmpty(settings.Kits);
        Assert.Null(settings.SpawnA);
        Assert.False(settings.DebugSolo);
    }

    [Fact]
    public void DefaultKits_OnlyUseItemsThatExistInGame()
    {
        // Keys found in GameInfo by the prototype (docs/prototype-findings.md)
        var gameWeapons = new[]
        {
            "pistol", "shotgun", "smg", "assaultrifle", "sniperrifle", "bow", "knife", "brassknuckles", "dynamite",
        };

        var kits = KitParser.Parse(DuelSettings.DefaultKits, out _)!;

        Assert.All(kits.SelectMany(k => k.Items), item => Assert.Contains(item, gameWeapons));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    public void FromRaw_RoundsToWinOutOfRange_FallsBackWithWarning(int value)
    {
        var raw = DuelSettings.Defaults with { RoundsToWin = value };

        var settings = DuelSettings.FromRaw(raw, out var warnings);

        Assert.Equal(5, settings.RoundsToWin);
        Assert.Contains(warnings, w => w.Contains("RoundsToWin"));
    }

    [Fact]
    public void FromRaw_OtherNumbersOutOfRange_FallBack()
    {
        var raw = DuelSettings.Defaults with
        {
            RoundTimeSeconds = 10, CountdownSeconds = 11, ArenaRadius = 5f, IslandIndex = 300,
        };

        var settings = DuelSettings.FromRaw(raw, out var warnings);

        Assert.Equal(90, settings.RoundTimeSeconds);
        Assert.Equal(3, settings.CountdownSeconds);
        Assert.Equal(60f, settings.ArenaRadius);
        Assert.Equal(DuelSettings.Defaults.IslandIndex, settings.IslandIndex);
        Assert.Equal(4, warnings.Count);
    }

    [Fact]
    public void FromRaw_InvalidKits_FallBackToDefaultKits()
    {
        var raw = DuelSettings.Defaults with { Kits = "pistol|knife" };

        var settings = DuelSettings.FromRaw(raw, out var warnings);

        Assert.Equal(KitParser.Parse(DuelSettings.DefaultKits, out _)!.Select(k => k.Name), settings.Kits.Select(k => k.Name));
        Assert.Contains(warnings, w => w.Contains("Kits"));
    }

    [Fact]
    public void FromRaw_ParsesSpawnOverrides()
    {
        var raw = DuelSettings.Defaults with { SpawnA = "1.5, 2, -3", SpawnB = "abc" };

        var settings = DuelSettings.FromRaw(raw, out var warnings);

        Assert.Equal(new Vector3(1.5f, 2f, -3f), settings.SpawnA);
        Assert.Null(settings.SpawnB);
        Assert.Contains(warnings, w => w.Contains("SpawnB"));
    }

    [Fact]
    public void FromRaw_IslandsStartWithConfiguredIsland()
    {
        var settings = DuelSettings.FromRaw(DuelSettings.Defaults with { IslandIndex = 2 }, out _);

        Assert.Equal(new[] { 2 }, settings.Islands);
    }

    [Fact]
    public void Defaults_RotateIslands()
    {
        Assert.True(DuelSettings.FromRaw(DuelSettings.Defaults, out _).IslandRotation);
    }

    [Fact]
    public void Defaults_RandomizeUpgrades()
    {
        Assert.True(DuelSettings.FromRaw(DuelSettings.Defaults, out _).RandomUpgrades);
    }

    [Fact]
    public void DefaultKits_IncludeKnifeOnlyRound()
    {
        var kits = DuelSettings.FromRaw(DuelSettings.Defaults, out _).Kits;

        Assert.Contains(kits, k => k.Items.Count == 1 && k.Items[0] == "knife");
    }
}

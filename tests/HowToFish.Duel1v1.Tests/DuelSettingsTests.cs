using System.Linq;
using System.Numerics;
using HowToFish.Duel1v1.Core;
using Xunit;

namespace HowToFish.Duel1v1.Tests;

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
        // Clés relevées dans GameInfo par le prototype (docs/prototype-findings.md)
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
}

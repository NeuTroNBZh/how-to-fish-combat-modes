using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;
using Xunit;

namespace HowToFish.Duel1v1.Tests;

public class ArenaSettingsTests
{
    private static DuelSettings Shared() =>
        DuelSettings.FromRaw(DuelSettings.Defaults with { Kits = "a;b", CountdownSeconds = 4 }, out _);

    [Fact]
    public void FromRaw_Defaults_UseSpecValuesAndSharedSettings()
    {
        var s = ArenaSettings.FromRaw(ArenaSettings.Defaults, Shared(), out var warnings);

        Assert.Empty(warnings);
        Assert.Equal(20, s.KillTarget);
        Assert.Equal(600, s.TimeLimitSeconds);
        Assert.Equal(120, s.KitRotationSeconds);
        Assert.Equal(3, s.RespawnSeconds);
        Assert.Equal(2, s.SpawnProtectionSeconds);
        Assert.Equal(4, s.CountdownSeconds);
        Assert.Equal(new[] { "a", "b" }, new[] { s.Kits[0].Name, s.Kits[1].Name });
        Assert.Equal(new[] { 0 }, s.Islands);
        Assert.True(s.RandomUpgrades);
        Assert.Equal(7, s.Ladder.Weapons.Count);
    }

    [Fact]
    public void FromRaw_OutOfRange_FallsBackWithOneWarningEach()
    {
        var raw = ArenaSettings.Defaults with
        {
            KillTarget = 0, TimeLimitSeconds = 10, KitRotationSeconds = 5, RespawnSeconds = 99, SpawnProtectionSeconds = -1,
        };

        var s = ArenaSettings.FromRaw(raw, Shared(), out var warnings);

        Assert.Equal(5, warnings.Count);
        Assert.Equal(20, s.KillTarget);
        Assert.Equal(600, s.TimeLimitSeconds);
        Assert.Equal(120, s.KitRotationSeconds);
        Assert.Equal(3, s.RespawnSeconds);
        Assert.Equal(2, s.SpawnProtectionSeconds);
    }

    [Fact]
    public void FromRaw_InvalidLadder_UsesDefaultWithWarning()
    {
        var s = ArenaSettings.FromRaw(ArenaSettings.Defaults with { Ladder = "pistol;smg" }, Shared(), out var warnings);

        Assert.Single(warnings);
        Assert.Equal(7, s.Ladder.Weapons.Count);
    }
}

using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class UpgradeRollerTests
{
    [Fact]
    public void Roll_NoOptions_GivesBareWeapon()
    {
        var loadout = UpgradeRoller.Roll(WeaponOptions.None, new FakeRandom(1, 1, 1, 1, 1, 1, 1));

        Assert.Equal(Loadout.Bare, loadout);
    }

    [Fact]
    public void Roll_PicksEachAccessoryWithinItsRange()
    {
        var options = new WeaponOptions(Sights: 3, Barrels: 2, BulletLevels: 4, QuiverLevels: 0,
            HasExtendedMag: true, HasLaser: true, SharpnessLevels: 0);

        var loadout = UpgradeRoller.Roll(options, new FakeRandom(2, 1, 3, 1, 0));

        Assert.Equal(new Loadout(Sight: 2, Barrel: 1, Bullet: 3, Quiver: 0, ExtendedMag: true, Laser: false, Sharpness: 0), loadout);
    }

    [Fact]
    public void Roll_UnavailableToggles_StayOff()
    {
        var options = WeaponOptions.None with { HasExtendedMag = false, HasLaser = false };

        var loadout = UpgradeRoller.Roll(options, new FakeRandom(1, 1));

        Assert.False(loadout.ExtendedMag);
        Assert.False(loadout.Laser);
    }

    [Fact]
    public void Roll_MeleeAndBow_UseTheirOwnLevels()
    {
        var options = WeaponOptions.None with { QuiverLevels = 3, SharpnessLevels = 5 };

        var loadout = UpgradeRoller.Roll(options, new FakeRandom(2, 4));

        Assert.Equal(2, loadout.Quiver);
        Assert.Equal(4, loadout.Sharpness);
    }
}

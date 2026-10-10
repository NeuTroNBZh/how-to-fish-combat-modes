namespace HowToFish.Duel1v1.Core;

public sealed record WeaponOptions(
    int Sights, int Barrels, int BulletLevels, int QuiverLevels, bool HasExtendedMag, bool HasLaser, int SharpnessLevels)
{
    public static WeaponOptions None { get; } = new(0, 0, 0, 0, false, false, 0);
}

public sealed record Loadout(int Sight, int Barrel, int Bullet, int Quiver, bool ExtendedMag, bool Laser, int Sharpness)
{
    public static Loadout Bare { get; } = new(0, 0, 0, 0, false, false, 0);
}

public static class UpgradeRoller
{
    // Index 0 = accessoire d'origine : une arme nue fait partie des tirages possibles.
    public static Loadout Roll(WeaponOptions options, IRandomSource random) => new(
        Level(options.Sights, random),
        Level(options.Barrels, random),
        Level(options.BulletLevels, random),
        Level(options.QuiverLevels, random),
        Toggle(options.HasExtendedMag, random),
        Toggle(options.HasLaser, random),
        Level(options.SharpnessLevels, random));

    private static int Level(int count, IRandomSource random) => count > 1 ? random.Next(count) : 0;

    private static bool Toggle(bool available, IRandomSource random) => available && random.Next(2) == 1;
}

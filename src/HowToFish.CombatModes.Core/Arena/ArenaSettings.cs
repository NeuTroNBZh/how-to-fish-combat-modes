using System.Collections.Generic;

namespace HowToFish.CombatModes.Core.Arena;

public sealed record RawArenaSettings(
    int KillTarget, int TimeLimitSeconds, int KitRotationSeconds, int RespawnSeconds, int SpawnProtectionSeconds,
    string Ladder);

public sealed record ArenaSettings(
    int KillTarget, int TimeLimitSeconds, int KitRotationSeconds, int RespawnSeconds, int SpawnProtectionSeconds,
    int CountdownSeconds, GunGameLadder Ladder, IReadOnlyList<Kit> Kits, IReadOnlyList<int> Islands,
    bool RandomUpgrades)
{
    public const int MaxPlayers = GameModes.ArenaMaxMembers;
    public const double PrepareSeconds = 1.0;
    public const double KillCreditSeconds = 10.0;
    public const string DefaultLadder = "sniperrifle;assaultrifle;smg;shotgun;bow;pistol;knife";

    public static RawArenaSettings Defaults { get; } = new(20, 600, 120, 3, 2, DefaultLadder);

    public static ArenaSettings FromRaw(RawArenaSettings raw, DuelSettings shared, out IReadOnlyList<string> warnings)
    {
        var w = new List<string>();
        var d = Defaults;
        var ladder = GunGameLadder.Parse(raw.Ladder, out var error);
        if (ladder == null)
        {
            w.Add($"Ladder invalide ({error}), échelle par défaut utilisée");
            ladder = GunGameLadder.Parse(DefaultLadder, out _)!;
        }
        var settings = new ArenaSettings(
            SettingRange.Int(nameof(raw.KillTarget), raw.KillTarget, 1, 100, d.KillTarget, w),
            SettingRange.Int(nameof(raw.TimeLimitSeconds), raw.TimeLimitSeconds, 60, 3600, d.TimeLimitSeconds, w),
            SettingRange.Int(nameof(raw.KitRotationSeconds), raw.KitRotationSeconds, 30, 600, d.KitRotationSeconds, w),
            SettingRange.Int(nameof(raw.RespawnSeconds), raw.RespawnSeconds, 0, 15, d.RespawnSeconds, w),
            SettingRange.Int(nameof(raw.SpawnProtectionSeconds), raw.SpawnProtectionSeconds, 0, 10, d.SpawnProtectionSeconds, w),
            shared.CountdownSeconds,
            ladder,
            shared.Kits,
            shared.Islands,
            shared.RandomUpgrades);
        warnings = w;
        return settings;
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace HowToFish.Duel1v1.Core;

public sealed record RawDuelSettings(
    int RoundsToWin, int RoundTimeSeconds, int CountdownSeconds, float ArenaRadius,
    int IslandIndex, string Kits, string SpawnA, string SpawnB, bool DebugSolo);

public sealed record DuelSettings(
    int RoundsToWin, int RoundTimeSeconds, int CountdownSeconds, float ArenaRadius,
    int IslandIndex, IReadOnlyList<Kit> Kits, Vector3? SpawnA, Vector3? SpawnB, bool DebugSolo)
{
    public const double PrepareSeconds = 1.0;
    public const double RoundEndSeconds = 3.0;
    public const double DoubleKoWindowSeconds = 1.0;
    public const double StateBroadcastSeconds = 2.0;
    public const double HelloTimeoutSeconds = 5.0;

    // Valeurs issues de docs/prototype-findings.md
    public const string DefaultKits = "pistol+knife;shotgun+knife;smg+knife;assaultrifle+knife;sniperrifle+pistol;bow+arrow+knife";
    public const int DefaultIslandIndex = 0;

    public static RawDuelSettings Defaults { get; } =
        new(5, 90, 3, 60f, DefaultIslandIndex, DefaultKits, "", "", false);

    public DuelSettings WithKits(IReadOnlyList<Kit> kits) => this with { Kits = kits };

    public static DuelSettings FromRaw(RawDuelSettings raw, out IReadOnlyList<string> warnings)
    {
        var w = new List<string>();
        var d = Defaults;
        var settings = new DuelSettings(
            InRange(nameof(raw.RoundsToWin), raw.RoundsToWin, 1, 15, d.RoundsToWin, w),
            InRange(nameof(raw.RoundTimeSeconds), raw.RoundTimeSeconds, 15, 600, d.RoundTimeSeconds, w),
            InRange(nameof(raw.CountdownSeconds), raw.CountdownSeconds, 0, 10, d.CountdownSeconds, w),
            InRange(nameof(raw.ArenaRadius), raw.ArenaRadius, 10f, 500f, d.ArenaRadius, w),
            InRange(nameof(raw.IslandIndex), raw.IslandIndex, 0, 255, d.IslandIndex, w),
            ParseKits(raw.Kits, w),
            ParseSpawn(nameof(raw.SpawnA), raw.SpawnA, w),
            ParseSpawn(nameof(raw.SpawnB), raw.SpawnB, w),
            raw.DebugSolo);
        warnings = w;
        return settings;
    }

    private static int InRange(string name, int value, int min, int max, int fallback, List<string> warnings)
    {
        if (value >= min && value <= max) return value;
        warnings.Add($"{name}={value} hors limites [{min}-{max}], valeur par défaut {fallback} utilisée");
        return fallback;
    }

    private static float InRange(string name, float value, float min, float max, float fallback, List<string> warnings)
    {
        if (value >= min && value <= max) return value;
        warnings.Add($"{name}={value} hors limites [{min}-{max}], valeur par défaut {fallback} utilisée");
        return fallback;
    }

    private static IReadOnlyList<Kit> ParseKits(string text, List<string> warnings)
    {
        var kits = KitParser.Parse(text, out var error);
        if (kits != null) return kits;
        warnings.Add($"Kits invalides ({error}), kits par défaut utilisés");
        return KitParser.Parse(DefaultKits, out _)!;
    }

    private static Vector3? ParseSpawn(string name, string text, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var parts = text.Split(',');
        if (parts.Length == 3
            && TryFloat(parts[0], out var x) && TryFloat(parts[1], out var y) && TryFloat(parts[2], out var z))
        {
            return new Vector3(x, y, z);
        }
        warnings.Add($"{name}='{text}' invalide (format attendu x,y,z), calcul automatique utilisé");
        return null;
    }

    private static bool TryFloat(string text, out float value) =>
        float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}

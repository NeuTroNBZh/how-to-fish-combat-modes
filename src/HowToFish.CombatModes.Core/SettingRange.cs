using System.Collections.Generic;

namespace HowToFish.CombatModes.Core;

internal static class SettingRange
{
    public static int Int(string name, int value, int min, int max, int fallback, List<string> warnings)
    {
        if (value >= min && value <= max) return value;
        warnings.Add($"{name}={value} hors limites [{min}-{max}], valeur par défaut {fallback} utilisée");
        return fallback;
    }

    public static float Float(string name, float value, float min, float max, float fallback, List<string> warnings)
    {
        if (value >= min && value <= max) return value;
        warnings.Add($"{name}={value} hors limites [{min}-{max}], valeur par défaut {fallback} utilisée");
        return fallback;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace HowToFish.CombatModes.Core.Arena;

public sealed record GunGameLadder(IReadOnlyList<string> Weapons)
{
    public const string FinalWeapon = "knife";
    private const int MinLevels = 2;

    public int LastLevel => Weapons.Count - 1;

    public static GunGameLadder? Parse(string text, out string error)
    {
        var weapons = (text ?? "").Split(';')
            .Select(w => w.Replace(" ", "").Trim().ToLowerInvariant())
            .Where(w => w.Length > 0)
            .ToList();
        return Validate(weapons, out error);
    }

    public GunGameLadder? KeepAvailable(Func<string, bool> exists, out IReadOnlyList<string> missing)
    {
        missing = Weapons.Where(w => !exists(w)).Distinct().ToList();
        return Validate(Weapons.Where(exists).ToList(), out _);
    }

    // Before the last level the knife is kept as a secondary weapon: otherwise the humiliation kill would be impossible.
    public Kit KitAt(int level)
    {
        var clamped = Math.Max(0, Math.Min(level, LastLevel));
        var weapon = Weapons[clamped];
        var items = clamped == LastLevel ? new[] { weapon } : new[] { weapon, FinalWeapon };
        return new Kit(string.Join("+", items), items);
    }

    private static GunGameLadder? Validate(List<string> weapons, out string error)
    {
        if (weapons.Count < MinLevels)
        {
            error = $"at least {MinLevels} levels required";
            return null;
        }
        if (weapons[weapons.Count - 1] != FinalWeapon)
        {
            error = $"the last level must be {FinalWeapon}";
            return null;
        }
        error = "";
        return new GunGameLadder(weapons);
    }
}

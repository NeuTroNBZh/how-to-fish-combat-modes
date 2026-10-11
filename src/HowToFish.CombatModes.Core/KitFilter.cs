using System;
using System.Collections.Generic;
using System.Linq;

namespace HowToFish.CombatModes.Core;

public static class KitFilter
{
    public static IReadOnlyList<Kit> KeepAvailable(
        IReadOnlyList<Kit> kits, Func<string, bool> itemExists, out IReadOnlyList<string> missingItems)
    {
        var missing = kits.SelectMany(k => k.Items).Where(i => !itemExists(i)).Distinct().ToList();
        missingItems = missing;
        return kits.Where(k => k.Items.All(i => !missing.Contains(i))).ToList();
    }
}

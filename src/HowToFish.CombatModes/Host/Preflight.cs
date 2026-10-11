using System.Collections.Generic;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;

namespace HowToFish.CombatModes.Host;

internal static class Preflight
{
    // Shared by the menu (before creating the lobby) and the host (in-game safety net).
    public static bool TryValidate(DuelSettings settings, out DuelSettings validated, out string error)
    {
        validated = settings;
        if (!TryKits(settings, out var kits, out error)) return false;
        var (island, islands) = ValidIslands(settings);
        validated = settings.WithKits(kits) with { IslandIndex = island, Islands = islands };
        return true;
    }

    // Gun game does not use duel kits: invalid kits do not block it.
    public static bool TryValidateArena(
        ArenaSettings arena, DuelSettings duel, GameMode mode, out ArenaSettings validated, out string error)
    {
        validated = arena;
        if (!TryKits(duel, out var kits, out error) && mode != GameMode.GunGame) return false;
        var ladder = arena.Ladder.KeepAvailable(GameActions.ItemExists, out var missing);
        foreach (var item in missing) CombatModesPlugin.Log.LogWarning($"Unknown ladder weapon, removed : {item}");
        if (ladder == null)
        {
            error = Ui.Lang.Text.InvalidLadder;
            CombatModesPlugin.Log.LogError(error);
            return false;
        }
        var (_, islands) = ValidIslands(duel);
        validated = arena with { Ladder = ladder, Kits = kits, Islands = islands, RandomUpgrades = duel.RandomUpgrades };
        error = "";
        return true;
    }

    private static bool TryKits(DuelSettings settings, out IReadOnlyList<Kit> kits, out string error)
    {
        kits = KitFilter.KeepAvailable(settings.Kits, GameActions.ItemExists, out var missing);
        foreach (var item in missing) CombatModesPlugin.Log.LogWarning($"Unknown kit item, kit ignored : {item}");
        if (kits.Count > 0)
        {
            error = "";
            return true;
        }
        error = Ui.Lang.Text.NoValidKits;
        CombatModesPlugin.Log.LogError(error);
        return false;
    }

    private static (int Island, IReadOnlyList<int> Islands) ValidIslands(DuelSettings settings)
    {
        var island = settings.IslandIndex;
        if (!GameActions.IsMainIsland(island))
        {
            CombatModesPlugin.Log.LogWarning($"IslandIndex={island} is not a main island, using island {DuelSettings.DefaultIslandIndex}");
            island = DuelSettings.DefaultIslandIndex;
        }
        return (island, settings.IslandRotation ? GameActions.MainIslands() : new[] { island });
    }
}

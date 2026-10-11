using System.Collections.Generic;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;

namespace HowToFish.Duel1v1.Host;

internal static class DuelPreflight
{
    // Partagé par le menu (avant de créer le lobby) et par l'hôte (filet de sécurité en jeu).
    public static bool TryValidate(DuelSettings settings, out DuelSettings validated, out string error)
    {
        validated = settings;
        if (!TryKits(settings, out var kits, out error)) return false;
        var (island, islands) = ValidIslands(settings);
        validated = settings.WithKits(kits) with { IslandIndex = island, Islands = islands };
        return true;
    }

    // La course à l'armement n'utilise pas les kits du duel : des kits invalides ne la bloquent pas.
    public static bool TryValidateArena(
        ArenaSettings arena, DuelSettings duel, GameMode mode, out ArenaSettings validated, out string error)
    {
        validated = arena;
        if (!TryKits(duel, out var kits, out error) && mode != GameMode.GunGame) return false;
        var ladder = arena.Ladder.KeepAvailable(GameActions.ItemExists, out var missing);
        foreach (var item in missing) DuelPlugin.Log.LogWarning($"Arme de l'échelle inconnue, retirée : {item}");
        if (ladder == null)
        {
            error = "Échelle d'armes invalide dans htf.duel1v1.cfg (2 niveaux minimum, le dernier doit être knife)";
            DuelPlugin.Log.LogError(error);
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
        foreach (var item in missing) DuelPlugin.Log.LogWarning($"Objet de kit inconnu, kit ignoré : {item}");
        if (kits.Count > 0)
        {
            error = "";
            return true;
        }
        error = "Aucun kit d'armes valide dans htf.duel1v1.cfg (voir le log)";
        DuelPlugin.Log.LogError(error);
        return false;
    }

    private static (int Island, IReadOnlyList<int> Islands) ValidIslands(DuelSettings settings)
    {
        var island = settings.IslandIndex;
        if (!GameActions.IsMainIsland(island))
        {
            DuelPlugin.Log.LogWarning($"IslandIndex={island} n'est pas une île principale, île {DuelSettings.DefaultIslandIndex} utilisée");
            island = DuelSettings.DefaultIslandIndex;
        }
        return (island, settings.IslandRotation ? GameActions.MainIslands() : new[] { island });
    }
}

using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;

namespace HowToFish.Duel1v1.Host;

internal static class DuelPreflight
{
    // Partagé par le menu (avant de créer le lobby) et par l'hôte (filet de sécurité en jeu).
    public static bool TryValidate(DuelSettings settings, out DuelSettings validated, out string error)
    {
        validated = settings;
        var kits = KitFilter.KeepAvailable(settings.Kits, GameActions.ItemExists, out var missing);
        foreach (var item in missing) DuelPlugin.Log.LogWarning($"Objet de kit inconnu, kit ignoré : {item}");
        if (kits.Count == 0)
        {
            error = "Aucun kit d'armes valide dans htf.duel1v1.cfg (voir le log)";
            DuelPlugin.Log.LogError(error);
            return false;
        }

        var island = settings.IslandIndex;
        if (!GameActions.IsMainIsland(island))
        {
            DuelPlugin.Log.LogWarning($"IslandIndex={island} n'est pas une île principale, île {DuelSettings.DefaultIslandIndex} utilisée");
            island = DuelSettings.DefaultIslandIndex;
        }

        var islands = settings.IslandRotation ? GameActions.MainIslands() : new[] { island };
        validated = settings.WithKits(kits) with { IslandIndex = island, Islands = islands };
        error = "";
        return true;
    }

    public static bool TryValidateArena(ArenaSettings arena, DuelSettings duel, out ArenaSettings validated, out string error)
    {
        validated = arena;
        if (!TryValidate(duel, out var shared, out error)) return false;
        var ladder = arena.Ladder.KeepAvailable(GameActions.ItemExists, out var missing);
        foreach (var item in missing) DuelPlugin.Log.LogWarning($"Arme de l'échelle inconnue, retirée : {item}");
        if (ladder == null)
        {
            error = "Échelle d'armes invalide dans htf.duel1v1.cfg (2 niveaux minimum, le dernier doit être knife)";
            DuelPlugin.Log.LogError(error);
            return false;
        }
        validated = arena with { Ladder = ladder, Kits = shared.Kits, Islands = shared.Islands, RandomUpgrades = shared.RandomUpgrades };
        return true;
    }
}

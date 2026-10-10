using HowToFish.Duel1v1.Core;

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
}

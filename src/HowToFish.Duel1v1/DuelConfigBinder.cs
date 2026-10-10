using BepInEx.Configuration;
using BepInEx.Logging;
using HowToFish.Duel1v1.Core;

namespace HowToFish.Duel1v1;

internal static class DuelConfigBinder
{
    public static (DuelSettings Settings, string DebugPositionKey) Load(ConfigFile config, ManualLogSource log)
    {
        var d = DuelSettings.Defaults;
        var raw = new RawDuelSettings(
            config.Bind("Match", "RoundsToWin", d.RoundsToWin, "Manches à gagner pour remporter le duel (1-15)").Value,
            config.Bind("Match", "RoundTimeSeconds", d.RoundTimeSeconds, "Durée max d'une manche en secondes (15-600)").Value,
            config.Bind("Match", "CountdownSeconds", d.CountdownSeconds, "Compte à rebours avant chaque manche (0-10)").Value,
            config.Bind("Arena", "ArenaRadius", d.ArenaRadius, "Rayon de la zone de combat en mètres (10-500)").Value,
            config.Bind("Arena", "IslandIndex", d.IslandIndex, "Index de l'île utilisée comme arène (0-255)").Value,
            config.Bind("Match", "Kits", d.Kits, "Kits d'armes : kits séparés par ';', objets par '+'").Value,
            config.Bind("Arena", "SpawnA", d.SpawnA, "Spawn A 'x,y,z' (vide = automatique)").Value,
            config.Bind("Arena", "SpawnB", d.SpawnB, "Spawn B 'x,y,z' (vide = automatique)").Value,
            config.Bind("Debug", "DebugSolo", d.DebugSolo, "Duel contre un adversaire fictif (développement)").Value,
            config.Bind("Arena", "IslandRotation", d.IslandRotation, "Change d'île à chaque manche (sinon IslandIndex uniquement)").Value,
            config.Bind("Match", "RandomUpgrades", d.RandomUpgrades, "Accessoires d'armes tirés au hasard à chaque manche, identiques pour tous").Value);
        var key = config.Bind("Debug", "DebugPositionKey", "F9", "Touche qui affiche ta position (nom de touche Input System)").Value;

        var settings = DuelSettings.FromRaw(raw, out var warnings);
        foreach (var warning in warnings) log.LogWarning(warning);
        return (settings, key);
    }
}

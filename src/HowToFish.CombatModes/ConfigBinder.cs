using BepInEx.Configuration;
using BepInEx.Logging;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;

namespace HowToFish.CombatModes;

internal static class ConfigBinder
{
    public static (DuelSettings Settings, ArenaSettings Arena, string DebugPositionKey) Load(ConfigFile config, ManualLogSource log)
    {
        var settings = LoadDuel(config, log);
        var a = ArenaSettings.Defaults;
        var raw = new RawArenaSettings(
            config.Bind("Arena", "KillTarget", a.KillTarget, "Free-for-all: kills to win (1-100)").Value,
            config.Bind("Arena", "TimeLimitSeconds", a.TimeLimitSeconds, "Free-for-all: max duration in seconds (60-3600)").Value,
            config.Bind("Arena", "KitRotationSeconds", a.KitRotationSeconds, "Free-for-all: kit changes every N seconds (30-600)").Value,
            config.Bind("Arena", "RespawnSeconds", a.RespawnSeconds, "Respawn delay in seconds (0-15)").Value,
            config.Bind("Arena", "SpawnProtectionSeconds", a.SpawnProtectionSeconds, "Spawn protection in seconds (0-10)").Value,
            config.Bind("Arena", "Ladder", a.Ladder, "Gun game: weapons separated by ';', the last must be knife").Value);
        var arena = ArenaSettings.FromRaw(raw, settings, out var warnings);
        foreach (var warning in warnings) log.LogWarning(warning);
        var key = config.Bind("Debug", "DebugPositionKey", "F9", "Key that shows your position (Input System key name)").Value;
        return (settings, arena, key);
    }

    private static DuelSettings LoadDuel(ConfigFile config, ManualLogSource log)
    {
        var d = DuelSettings.Defaults;
        var raw = new RawDuelSettings(
            config.Bind("Match", "RoundsToWin", d.RoundsToWin, "Rounds to win the duel (1-15)").Value,
            config.Bind("Match", "RoundTimeSeconds", d.RoundTimeSeconds, "Max round duration in seconds (15-600)").Value,
            config.Bind("Match", "CountdownSeconds", d.CountdownSeconds, "Countdown before each round in seconds (0-10)").Value,
            config.Bind("Arena", "ArenaRadius", d.ArenaRadius, "Combat zone radius in meters (10-500)").Value,
            config.Bind("Arena", "IslandIndex", d.IslandIndex, "Index of the island used as arena (0-255)").Value,
            config.Bind("Match", "Kits", d.Kits, "Weapon kits: kits separated by ';', items by '+'").Value,
            config.Bind("Arena", "SpawnA", d.SpawnA, "Spawn A 'x,y,z' (vide = automatique)").Value,
            config.Bind("Arena", "SpawnB", d.SpawnB, "Spawn B 'x,y,z' (vide = automatique)").Value,
            config.Bind("Debug", "DebugSolo", d.DebugSolo, "Duel against a fake opponent (development)").Value,
            config.Bind("Arena", "IslandRotation", d.IslandRotation, "Switch island every round (otherwise IslandIndex only)").Value,
            config.Bind("Match", "RandomUpgrades", d.RandomUpgrades, "Random weapon attachments each round, identical for everyone").Value);
        var settings = DuelSettings.FromRaw(raw, out var warnings);
        foreach (var warning in warnings) log.LogWarning(warning);
        return settings;
    }
}

using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace HowToFish.Duel1v1;

internal static class CompatibilityCheck
{
    private static readonly (Type Type, string Name)[] Methods =
    {
        (typeof(Server), "TeleportPlayer"), (typeof(Server), "RespawnPlayer"),
        (typeof(PlayerVitals), "TakeDamage"), (typeof(PlayerVitals), "ServerResetVitals"),
        (typeof(PlayerDying), "ServerDie"), (typeof(SteamManager), "CreateLobby"),
        (typeof(SteamManager), "OnLobbyCreated"), (typeof(SteamManager), "LeaveLobby"),
        (typeof(SteamManager), "JoinLobby"), (typeof(SaveManager), "LoadAllServers"),
        (typeof(SaveManager), "SaveServer"), (typeof(SaveManager), "CreateServer"),
        (typeof(SaveManager), "OnServerLoaded"), (typeof(SaveManager), "PlayerHasFinishedTutorial"),
        (typeof(SaveSystem), "DeleteServer"), (typeof(MainMenuManager), "Start"),
        (typeof(MainMenuManager), "ToggleMenu"), (typeof(ConnectionManager), "KickSteamUserIfConnected"),
        (typeof(OnlineIslandManager), "TpToSpecificIsland"), (typeof(GameInfo), "GetSpawnable"),
        (typeof(Item), "SetSyncedHolder"), (typeof(Item), "DestroyItem"),
        (typeof(ServerSettings), "ToggleFriendlyFire"), (typeof(PlayerCamera), "ToggleMouse"),
    };

    private static readonly (Type Type, string Name)[] Fields =
    {
        (typeof(PlayerDying), "_player"), (typeof(PlayerInventory), "_items"),
    };

    public static bool Run(ManualLogSource log)
    {
        var missing = Methods
            .Where(m => !AccessTools.GetDeclaredMethods(m.Type).Any(x => x.Name == m.Name))
            .Concat(Fields.Where(f => AccessTools.Field(f.Type, f.Name) == null))
            .Select(x => $"{x.Type.Name}.{x.Name}")
            .ToList();

        foreach (var name in missing) log.LogError($"Incompatible : {name} introuvable dans cette version du jeu");
        return missing.Count == 0;
    }
}

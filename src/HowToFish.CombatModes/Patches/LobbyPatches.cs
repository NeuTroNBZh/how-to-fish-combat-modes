using HarmonyLib;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Net;
using Steamworks;

namespace HowToFish.CombatModes.Patches;

[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.CreateLobby))]
internal static class CreateDuelLobbyPatch
{
    // Le nom de sauvegarde évite qu'un PendingLobbyType resté en plan transforme une partie normale en duel.
    private static bool Prefix()
    {
        if (CombatSession.PendingLobbyType is not ELobbyType type) return true;
        if (SaveManager.CurServerSave?.Name != CombatSession.SaveName)
        {
            CombatSession.Reset();
            return true;
        }
        SteamMatchmaking.CreateLobby(type, GameModes.MaxMembers(CombatSession.Mode));
        return false;
    }
}

[HarmonyPatch(typeof(SteamManager), "OnLobbyCreated")]
internal static class TagDuelLobbyPatch
{
    private static void Postfix(LobbyCreated_t callback)
    {
        if (CombatSession.PendingLobbyType is null) return;
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            CombatModesPlugin.Log.LogError($"Création du lobby duel refusée par Steam : {callback.m_eResult}");
            CombatSession.Reset();
            CombatSession.ShowNotice("Steam a refusé la création du lobby duel", UnityEngine.Time.time);
            return;
        }
        CombatLobby.OnDuelLobbyCreated(new CSteamID(callback.m_ulSteamIDLobby));
    }
}

[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.LeaveLobby))]
internal static class LeaveDuelLobbyPatch
{
    private static void Prefix()
    {
        if (!CombatSession.IsDuelLobby && CombatSession.PendingLobbyType is null) return;
        bool wasHost = CombatSession.IsHost || CombatSession.PendingLobbyType is not null;
        CombatSession.Reset();
        if (!wasHost) return;
        try
        {
            SaveSystem.DeleteServer(CombatSession.SaveName);
        }
        catch (System.Exception ex)
        {
            CombatModesPlugin.Log.LogWarning($"Suppression de la sauvegarde duel impossible : {ex.Message}");
        }
    }
}

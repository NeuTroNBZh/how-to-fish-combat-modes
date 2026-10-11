using HarmonyLib;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Net;
using Steamworks;

namespace HowToFish.Duel1v1.Patches;

[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.CreateLobby))]
internal static class CreateDuelLobbyPatch
{
    // Le nom de sauvegarde évite qu'un PendingLobbyType resté en plan transforme une partie normale en duel.
    private static bool Prefix()
    {
        if (DuelSession.PendingLobbyType is not ELobbyType type) return true;
        if (SaveManager.CurServerSave?.Name != DuelSession.SaveName)
        {
            DuelSession.Reset();
            return true;
        }
        SteamMatchmaking.CreateLobby(type, GameModes.MaxMembers(DuelSession.Mode));
        return false;
    }
}

[HarmonyPatch(typeof(SteamManager), "OnLobbyCreated")]
internal static class TagDuelLobbyPatch
{
    private static void Postfix(LobbyCreated_t callback)
    {
        if (DuelSession.PendingLobbyType is null) return;
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            DuelPlugin.Log.LogError($"Création du lobby duel refusée par Steam : {callback.m_eResult}");
            DuelSession.Reset();
            DuelSession.ShowNotice("Steam a refusé la création du lobby duel", UnityEngine.Time.time);
            return;
        }
        DuelLobby.OnDuelLobbyCreated(new CSteamID(callback.m_ulSteamIDLobby));
    }
}

[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.LeaveLobby))]
internal static class LeaveDuelLobbyPatch
{
    private static void Prefix()
    {
        if (!DuelSession.IsDuelLobby && DuelSession.PendingLobbyType is null) return;
        bool wasHost = DuelSession.IsHost || DuelSession.PendingLobbyType is not null;
        DuelSession.Reset();
        if (!wasHost) return;
        try
        {
            SaveSystem.DeleteServer(DuelSession.SaveName);
        }
        catch (System.Exception ex)
        {
            DuelPlugin.Log.LogWarning($"Suppression de la sauvegarde duel impossible : {ex.Message}");
        }
    }
}

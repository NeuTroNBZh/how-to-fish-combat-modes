using HarmonyLib;
using HowToFish.Duel1v1.Net;
using Steamworks;

namespace HowToFish.Duel1v1.Patches;

[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.CreateLobby))]
internal static class CreateDuelLobbyPatch
{
    private static bool Prefix()
    {
        if (DuelSession.PendingLobbyType is not ELobbyType type) return true;
        SteamMatchmaking.CreateLobby(type, DuelLobby.MaxMembers);
        return false;
    }
}

[HarmonyPatch(typeof(SteamManager), "OnLobbyCreated")]
internal static class TagDuelLobbyPatch
{
    private static void Postfix(LobbyCreated_t callback)
    {
        if (DuelSession.PendingLobbyType is null || callback.m_eResult != EResult.k_EResultOK) return;
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

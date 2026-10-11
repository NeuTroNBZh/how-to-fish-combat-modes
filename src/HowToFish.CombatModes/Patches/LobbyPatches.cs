using HarmonyLib;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Net;
using Steamworks;

namespace HowToFish.CombatModes.Patches;

[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.CreateLobby))]
internal static class CreateDuelLobbyPatch
{
    // The save name prevents a stale PendingLobbyType from turning a normal game into a duel.
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
            CombatModesPlugin.Log.LogError($"Duel lobby creation refused by Steam : {callback.m_eResult}");
            CombatSession.Reset();
            CombatSession.ShowNotice(Ui.Lang.Text.SteamRefusedLobby, UnityEngine.Time.time);
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
            CombatModesPlugin.Log.LogWarning($"Could not delete duel save : {ex.Message}");
        }
    }
}

using System.Linq;
using HowToFish.CombatModes.Arena;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Net;
using Steamworks;
using UnityEngine;

namespace HowToFish.CombatModes.Ui;

// Vue de l'état de combat côté joueur local, commune au rendu natif et au repli IMGUI.
internal static class CombatUiState
{
    public static bool InCombat => CombatSession.IsDuelLobby;

    public static bool IsDuel => CombatSession.Mode == GameMode.Duel;

    public static CombatScreen Screen =>
        !InCombat ? CombatScreen.None
        : IsDuel ? CombatUiRules.ScreenFor(CombatSession.Display?.Phase)
        : CombatUiRules.ScreenFor(CombatSession.ArenaDisplay?.Phase);

    public static ulong LocalId => SteamUser.GetSteamID().m_SteamID;

    public static ulong OwnerId => SteamMatchmaking.GetLobbyOwner(CombatSession.LobbyId).m_SteamID;

    public static bool LocalAlive =>
        !IsDuel && CombatSession.ArenaDisplay != null
        && CombatSession.ArenaDisplay.Players.Any(p => p.SteamId == LocalId && p.Alive);

    public static string NameOf(ulong id) =>
        id == ArenaHost.BotId ? "Bot"
        : id == LocalId ? SteamFriends.GetPersonaName()
        : SteamFriends.GetFriendPersonaName(new CSteamID(id));

    public static string OpponentName
    {
        get
        {
            var opponent = CombatLobby.Opponent();
            return opponent == CSteamID.Nil ? "Adversaire" : SteamFriends.GetFriendPersonaName(opponent);
        }
    }

    public static double Remaining()
    {
        if (IsDuel)
        {
            var s = CombatSession.Display;
            return s == null ? 0 : s.RemainingMs / 1000.0 - (Time.time - CombatSession.DisplayReceivedAt);
        }
        var w = CombatSession.ArenaDisplay;
        return w == null ? 0 : w.RemainingMs / 1000.0 - (Time.time - CombatSession.ArenaDisplayReceivedAt);
    }
}

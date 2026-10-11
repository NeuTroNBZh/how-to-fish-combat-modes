using System.Linq;
using HowToFish.Duel1v1.Arena;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Net;
using Steamworks;
using UnityEngine;

namespace HowToFish.Duel1v1.Ui;

// Vue de l'état de combat côté joueur local, commune au rendu natif et au repli IMGUI.
internal static class CombatUiState
{
    public static bool InCombat => DuelSession.IsDuelLobby;

    public static bool IsDuel => DuelSession.Mode == GameMode.Duel;

    public static CombatScreen Screen =>
        !InCombat ? CombatScreen.None
        : IsDuel ? CombatUiRules.ScreenFor(DuelSession.Display?.Phase)
        : CombatUiRules.ScreenFor(DuelSession.ArenaDisplay?.Phase);

    public static ulong LocalId => SteamUser.GetSteamID().m_SteamID;

    public static ulong OwnerId => SteamMatchmaking.GetLobbyOwner(DuelSession.LobbyId).m_SteamID;

    public static bool LocalAlive =>
        !IsDuel && DuelSession.ArenaDisplay != null
        && DuelSession.ArenaDisplay.Players.Any(p => p.SteamId == LocalId && p.Alive);

    public static string NameOf(ulong id) =>
        id == ArenaHost.BotId ? "Bot"
        : id == LocalId ? SteamFriends.GetPersonaName()
        : SteamFriends.GetFriendPersonaName(new CSteamID(id));

    public static string OpponentName
    {
        get
        {
            var opponent = DuelLobby.Opponent();
            return opponent == CSteamID.Nil ? "Adversaire" : SteamFriends.GetFriendPersonaName(opponent);
        }
    }

    public static double Remaining()
    {
        if (IsDuel)
        {
            var s = DuelSession.Display;
            return s == null ? 0 : s.RemainingMs / 1000.0 - (Time.time - DuelSession.DisplayReceivedAt);
        }
        var w = DuelSession.ArenaDisplay;
        return w == null ? 0 : w.RemainingMs / 1000.0 - (Time.time - DuelSession.ArenaDisplayReceivedAt);
    }
}

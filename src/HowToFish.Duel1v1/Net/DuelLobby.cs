using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;
using Steamworks;
using UnityEngine;

namespace HowToFish.Duel1v1.Net;

internal sealed class DuelLobby : MonoBehaviour
{
    public const string TagKey = "htf_duel";
    public const string TagValue = "1";
    public const string VersionKey = "htf_duel_ver";
    public const string ModeKey = "htf_mode";
    private const string GameVersionKey = "version";
    private const float HelloInterval = 2f;
    private const float RescanInterval = 5f;
    private const float MergeJoinDelay = 1f;

    private float _nextHello;
    private float _nextRescan;
    private CSteamID _quickMatchLobby = CSteamID.Nil;

    public static DuelLobby? Instance { get; private set; }

    private Callback<LobbyEnter_t>? _entered;
    private Callback<LobbyChatUpdate_t>? _membersChanged;
    private CallResult<LobbyMatchList_t>? _lobbyList;

    private void Awake()
    {
        Instance = this;
        _entered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _membersChanged = Callback<LobbyChatUpdate_t>.Create(OnMembersChanged);
        _lobbyList = CallResult<LobbyMatchList_t>.Create(OnLobbyList);
    }

    private void OnDestroy()
    {
        _entered?.Dispose();
        _membersChanged?.Dispose();
        _lobbyList?.Dispose();
    }

    public void Host(GameMode mode, bool isPublic)
    {
        DuelSession.BeginHosting(isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly, mode);
        SaveManager.OnServerLoaded();
        SaveManager.CreateServer(DuelSession.SaveName, useSteam: true, isPublic: isPublic, Difficulty.Default);
        SteamManager.CreateLobby();
        DuelPlugin.Log.LogInfo($"Création d'un salon {GameModes.Tag(mode)} (public={isPublic})");
    }

    public void StartQuickMatch(GameMode mode)
    {
        DuelSession.BeginSearch(mode);
        RequestOpenLobbies(mode);
    }

    private void RequestOpenLobbies(GameMode mode)
    {
        SteamMatchmaking.AddRequestLobbyListStringFilter(TagKey, TagValue, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(VersionKey, DuelPlugin.PluginVersion, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(GameVersionKey, Application.version, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(ModeKey, GameModes.Tag(mode), ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        _lobbyList?.Set(SteamMatchmaking.RequestLobbyList());
    }

    public void CancelSearch()
    {
        DuelSession.SetSearching(false);
        SteamManager.LeaveLobby(DisconnectReason.None);
    }

    public static void OnDuelLobbyCreated(CSteamID lobby)
    {
        bool isPublic = DuelSession.PendingLobbyType == ELobbyType.k_ELobbyTypePublic;
        var mode = DuelSession.Mode;
        SteamMatchmaking.SetLobbyData(lobby, TagKey, TagValue);
        SteamMatchmaking.SetLobbyData(lobby, VersionKey, DuelPlugin.PluginVersion);
        SteamMatchmaking.SetLobbyData(lobby, ModeKey, GameModes.Tag(mode));
        SteamMatchmaking.SetLobbyMemberLimit(lobby, GameModes.MaxMembers(mode));
        DuelSession.EnterDuelLobby(lobby, isHost: true, mode);
        if (Instance != null) Instance._quickMatchLobby = isPublic ? lobby : CSteamID.Nil;
        if (!isPublic) SteamFriends.ActivateGameOverlayInviteDialog(lobby);
        DuelPlugin.Log.LogInfo($"Salon {GameModes.Tag(mode)} {lobby.m_SteamID} prêt");
    }

    public static CSteamID Opponent()
    {
        var me = SteamUser.GetSteamID();
        int count = SteamMatchmaking.GetNumLobbyMembers(DuelSession.LobbyId);
        for (int i = 0; i < count; i++)
        {
            var member = SteamMatchmaking.GetLobbyMemberByIndex(DuelSession.LobbyId, i);
            if (member != me) return member;
        }
        return CSteamID.Nil;
    }

    private void OnLobbyList(LobbyMatchList_t result, bool ioFailure)
    {
        if (IsWaitingAloneInQuickMatch())
        {
            if (!ioFailure) TryMerge(result);
            return;
        }
        if (!DuelSession.IsSearching) return;
        if (ioFailure || result.m_nLobbiesMatching == 0)
        {
            DuelPlugin.Log.LogInfo("Aucun salon ouvert trouvé, création d'un salon public");
            Host(DuelSession.Mode, isPublic: true);
            return;
        }
        var lobby = SteamMatchmaking.GetLobbyByIndex(0);
        DuelPlugin.Log.LogInfo($"Salon trouvé : {lobby.m_SteamID}");
        SteamManager.JoinLobby(lobby.m_SteamID);
    }

    private bool IsWaitingAloneInQuickMatch()
    {
        if (!DuelSession.IsDuelLobby || !DuelSession.IsHost || DuelSession.LobbyId != _quickMatchLobby) return false;
        bool waiting = DuelSession.Mode == GameMode.Duel
            ? DuelSession.Snapshot.Phase == DuelPhase.Waiting
            : DuelSession.ArenaSnapshot.Phase == ArenaPhase.Lobby;
        return waiting && SteamMatchmaking.GetNumLobbyMembers(DuelSession.LobbyId) == 1;
    }

    private void TryMerge(LobbyMatchList_t result)
    {
        var open = new List<OpenLobby>();
        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            var lobby = SteamMatchmaking.GetLobbyByIndex(i);
            open.Add(new OpenLobby(lobby.m_SteamID, SteamMatchmaking.GetNumLobbyMembers(lobby)));
        }
        bool arena = DuelSession.Mode != GameMode.Duel;
        if (QuickMatchPolicy.MergeTarget(DuelSession.LobbyId.m_SteamID, open, arena) is not ulong target) return;
        DuelPlugin.Log.LogInfo($"Autre salon en attente trouvé ({target}), on le rejoint");
        StartCoroutine(MoveTo(target, DuelSession.Mode));
    }

    private IEnumerator MoveTo(ulong lobby, GameMode mode)
    {
        _quickMatchLobby = CSteamID.Nil;
        SteamManager.LeaveLobby(DisconnectReason.None);
        DuelSession.BeginSearch(mode);
        yield return new WaitForSeconds(MergeJoinDelay);
        SteamManager.JoinLobby(lobby);
    }

    private void Update()
    {
        RescanWhileAlone();
        if (!DuelSession.IsDuelLobby || DuelSession.IsHost || !NeedsHello()) return;
        if (Time.time < _nextHello) return;
        _nextHello = Time.time + HelloInterval;
        DuelChannel.Send(DuelMessage.Hello());
    }

    // Renvoi du HELLO tant que l'hôte ne nous a pas pris en compte : un seul envoi peut arriver avant qu'il soit prêt.
    private static bool NeedsHello()
    {
        if (DuelSession.Mode == GameMode.Duel)
            return DuelSession.Display == null || DuelSession.Display.Phase == DuelPhase.Waiting;
        var me = SteamUser.GetSteamID().m_SteamID;
        return DuelSession.ArenaDisplay == null || DuelSession.ArenaDisplay.Players.All(p => p.SteamId != me);
    }

    private void RescanWhileAlone()
    {
        if (Time.time < _nextRescan || !IsWaitingAloneInQuickMatch()) return;
        _nextRescan = Time.time + RescanInterval;
        RequestOpenLobbies(DuelSession.Mode);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (callback.m_EChatRoomEnterResponse != 1)
        {
            EndSearch("Ce salon n'est plus disponible, relance une partie rapide");
            return;
        }
        var lobby = new CSteamID(callback.m_ulSteamIDLobby);
        if (SteamMatchmaking.GetLobbyData(lobby, TagKey) != TagValue) return;
        if (SteamMatchmaking.GetLobbyData(lobby, GameVersionKey) != Application.version)
        {
            EndSearch("Ce salon utilise une autre version du jeu");
            return;
        }
        if (SteamMatchmaking.GetLobbyOwner(lobby) == SteamUser.GetSteamID()) return;
        var mode = GameModes.TryParseTag(SteamMatchmaking.GetLobbyData(lobby, ModeKey), out var parsed) ? parsed : GameMode.Duel;
        DuelSession.EnterDuelLobby(lobby, isHost: false, mode);
        _nextHello = 0f;
    }

    private static void EndSearch(string notice)
    {
        if (!DuelSession.IsSearching) return;
        DuelSession.SetSearching(false);
        DuelSession.ShowNotice(notice, Time.time);
        DuelPlugin.Log.LogWarning(notice);
    }

    private void OnMembersChanged(LobbyChatUpdate_t update)
    {
        if (!DuelSession.IsDuelLobby || !DuelSession.IsHost || update.m_ulSteamIDLobby != DuelSession.LobbyId.m_SteamID) return;
        int members = SteamMatchmaking.GetNumLobbyMembers(DuelSession.LobbyId);
        SteamMatchmaking.SetLobbyJoinable(DuelSession.LobbyId, members < GameModes.MaxMembers(DuelSession.Mode));
    }
}

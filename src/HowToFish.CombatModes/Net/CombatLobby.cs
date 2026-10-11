using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;
using Steamworks;
using UnityEngine;

namespace HowToFish.CombatModes.Net;

internal sealed class CombatLobby : MonoBehaviour
{
    public const string TagKey = "htf_combat";
    public const string TagValue = "1";
    public const string VersionKey = "htf_combat_ver";
    public const string ModeKey = "htf_mode";
    private const string GameVersionKey = "version";
    private const float HelloInterval = 2f;
    private const float RescanInterval = 5f;
    private const float MergeJoinDelay = 1f;

    private float _nextHello;
    private float _nextRescan;
    private CSteamID _quickMatchLobby = CSteamID.Nil;

    public static CombatLobby? Instance { get; private set; }

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
        CombatSession.BeginHosting(isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly, mode);
        SaveManager.OnServerLoaded();
        SaveManager.CreateServer(CombatSession.SaveName, useSteam: true, isPublic: isPublic, Difficulty.Default);
        SteamManager.CreateLobby();
        CombatModesPlugin.Log.LogInfo($"Creating {GameModes.Tag(mode)} lobby (public={isPublic})");
    }

    public void StartQuickMatch(GameMode mode)
    {
        CombatSession.BeginSearch(mode);
        RequestOpenLobbies(mode);
    }

    private void RequestOpenLobbies(GameMode mode)
    {
        SteamMatchmaking.AddRequestLobbyListStringFilter(TagKey, TagValue, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(VersionKey, CombatModesPlugin.PluginVersion, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(GameVersionKey, Application.version, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(ModeKey, GameModes.Tag(mode), ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        _lobbyList?.Set(SteamMatchmaking.RequestLobbyList());
    }

    public void CancelSearch()
    {
        CombatSession.SetSearching(false);
        SteamManager.LeaveLobby(DisconnectReason.None);
    }

    public static void OnDuelLobbyCreated(CSteamID lobby)
    {
        bool isPublic = CombatSession.PendingLobbyType == ELobbyType.k_ELobbyTypePublic;
        var mode = CombatSession.Mode;
        SteamMatchmaking.SetLobbyData(lobby, TagKey, TagValue);
        SteamMatchmaking.SetLobbyData(lobby, VersionKey, CombatModesPlugin.PluginVersion);
        SteamMatchmaking.SetLobbyData(lobby, ModeKey, GameModes.Tag(mode));
        SteamMatchmaking.SetLobbyMemberLimit(lobby, GameModes.MaxMembers(mode));
        CombatSession.EnterDuelLobby(lobby, isHost: true, mode);
        if (Instance != null) Instance._quickMatchLobby = isPublic ? lobby : CSteamID.Nil;
        if (!isPublic) SteamFriends.ActivateGameOverlayInviteDialog(lobby);
        CombatModesPlugin.Log.LogInfo($"Lobby {GameModes.Tag(mode)} {lobby.m_SteamID} ready");
    }

    public static CSteamID Opponent()
    {
        var me = SteamUser.GetSteamID();
        int count = SteamMatchmaking.GetNumLobbyMembers(CombatSession.LobbyId);
        for (int i = 0; i < count; i++)
        {
            var member = SteamMatchmaking.GetLobbyMemberByIndex(CombatSession.LobbyId, i);
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
        if (!CombatSession.IsSearching) return;
        if (ioFailure || result.m_nLobbiesMatching == 0)
        {
            CombatModesPlugin.Log.LogInfo("No open lobby found, creating a public lobby");
            Host(CombatSession.Mode, isPublic: true);
            return;
        }
        var lobby = SteamMatchmaking.GetLobbyByIndex(0);
        CombatModesPlugin.Log.LogInfo($"Lobby found: {lobby.m_SteamID}");
        SteamManager.JoinLobby(lobby.m_SteamID);
    }

    private bool IsWaitingAloneInQuickMatch()
    {
        if (!CombatSession.IsDuelLobby || !CombatSession.IsHost || CombatSession.LobbyId != _quickMatchLobby) return false;
        bool waiting = CombatSession.Mode == GameMode.Duel
            ? CombatSession.Snapshot.Phase == DuelPhase.Waiting
            : CombatSession.ArenaSnapshot.Phase == ArenaPhase.Lobby;
        return waiting && SteamMatchmaking.GetNumLobbyMembers(CombatSession.LobbyId) == 1;
    }

    private void TryMerge(LobbyMatchList_t result)
    {
        var open = new List<OpenLobby>();
        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            var lobby = SteamMatchmaking.GetLobbyByIndex(i);
            open.Add(new OpenLobby(lobby.m_SteamID, SteamMatchmaking.GetNumLobbyMembers(lobby)));
        }
        bool arena = CombatSession.Mode != GameMode.Duel;
        if (QuickMatchPolicy.MergeTarget(CombatSession.LobbyId.m_SteamID, open, arena) is not ulong target) return;
        CombatModesPlugin.Log.LogInfo($"Found another waiting lobby ({target}), joining it");
        StartCoroutine(MoveTo(target, CombatSession.Mode));
    }

    private IEnumerator MoveTo(ulong lobby, GameMode mode)
    {
        _quickMatchLobby = CSteamID.Nil;
        SteamManager.LeaveLobby(DisconnectReason.None);
        CombatSession.BeginSearch(mode);
        yield return new WaitForSeconds(MergeJoinDelay);
        SteamManager.JoinLobby(lobby);
    }

    private void Update()
    {
        RescanWhileAlone();
        if (!CombatSession.IsDuelLobby || CombatSession.IsHost || !NeedsHello()) return;
        if (Time.time < _nextHello) return;
        _nextHello = Time.time + HelloInterval;
        CombatChannel.Send(DuelMessage.Hello());
    }

    // Resend HELLO until the host acknowledges us: a single send can arrive before it is ready.
    private static bool NeedsHello()
    {
        if (CombatSession.Mode == GameMode.Duel)
            return CombatSession.Display == null || CombatSession.Display.Phase == DuelPhase.Waiting;
        var me = SteamUser.GetSteamID().m_SteamID;
        return CombatSession.ArenaDisplay == null || CombatSession.ArenaDisplay.Players.All(p => p.SteamId != me);
    }

    private void RescanWhileAlone()
    {
        if (Time.time < _nextRescan || !IsWaitingAloneInQuickMatch()) return;
        _nextRescan = Time.time + RescanInterval;
        RequestOpenLobbies(CombatSession.Mode);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (callback.m_EChatRoomEnterResponse != 1)
        {
            EndSearch(Ui.Lang.Text.LobbyUnavailable);
            return;
        }
        var lobby = new CSteamID(callback.m_ulSteamIDLobby);
        if (SteamMatchmaking.GetLobbyData(lobby, TagKey) != TagValue) return;
        if (SteamMatchmaking.GetLobbyData(lobby, GameVersionKey) != Application.version)
        {
            EndSearch(Ui.Lang.Text.LobbyOtherGameVersion);
            return;
        }
        if (SteamMatchmaking.GetLobbyOwner(lobby) == SteamUser.GetSteamID()) return;
        var mode = GameModes.TryParseTag(SteamMatchmaking.GetLobbyData(lobby, ModeKey), out var parsed) ? parsed : GameMode.Duel;
        CombatSession.EnterDuelLobby(lobby, isHost: false, mode);
        _nextHello = 0f;
    }

    private static void EndSearch(string notice)
    {
        if (!CombatSession.IsSearching) return;
        CombatSession.SetSearching(false);
        CombatSession.ShowNotice(notice, Time.time);
        CombatModesPlugin.Log.LogWarning(notice);
    }

    private void OnMembersChanged(LobbyChatUpdate_t update)
    {
        if (!CombatSession.IsDuelLobby || !CombatSession.IsHost || update.m_ulSteamIDLobby != CombatSession.LobbyId.m_SteamID) return;
        int members = SteamMatchmaking.GetNumLobbyMembers(CombatSession.LobbyId);
        SteamMatchmaking.SetLobbyJoinable(CombatSession.LobbyId, !CombatSession.IsLobbyLocked && members < GameModes.MaxMembers(CombatSession.Mode));
    }
}

using Steamworks;
using UnityEngine;

namespace HowToFish.Duel1v1.Net;

internal sealed class DuelLobby : MonoBehaviour
{
    public const int MaxMembers = 2;
    public const string TagKey = "htf_duel";
    public const string TagValue = "1";
    public const string VersionKey = "htf_duel_ver";

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

    public void HostDuel(bool isPublic)
    {
        DuelSession.BeginHosting(isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly);
        SaveManager.OnServerLoaded();
        SaveManager.CreateServer(DuelSession.SaveName, useSteam: true, isPublic: isPublic, Difficulty.Default);
        SteamManager.CreateLobby();
        DuelPlugin.Log.LogInfo($"Création d'un lobby duel (public={isPublic})");
    }

    public void StartQuickMatch()
    {
        DuelSession.SetSearching(true);
        SteamMatchmaking.AddRequestLobbyListStringFilter(TagKey, TagValue, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(VersionKey, DuelPlugin.PluginVersion, ELobbyComparison.k_ELobbyComparisonEqual);
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
        SteamMatchmaking.SetLobbyData(lobby, TagKey, TagValue);
        SteamMatchmaking.SetLobbyData(lobby, VersionKey, DuelPlugin.PluginVersion);
        SteamMatchmaking.SetLobbyMemberLimit(lobby, MaxMembers);
        DuelSession.EnterDuelLobby(lobby, isHost: true);
        if (!isPublic) SteamFriends.ActivateGameOverlayInviteDialog(lobby);
        DuelPlugin.Log.LogInfo($"Lobby duel {lobby.m_SteamID} prêt");
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
        if (!DuelSession.IsSearching) return;
        if (ioFailure || result.m_nLobbiesMatching == 0)
        {
            DuelPlugin.Log.LogInfo("Aucun duel ouvert trouvé, création d'un lobby public");
            HostDuel(isPublic: true);
            return;
        }
        var lobby = SteamMatchmaking.GetLobbyByIndex(0);
        DuelPlugin.Log.LogInfo($"Duel trouvé : {lobby.m_SteamID}");
        SteamManager.JoinLobby(lobby.m_SteamID);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (callback.m_EChatRoomEnterResponse != 1) return;
        var lobby = new CSteamID(callback.m_ulSteamIDLobby);
        if (SteamMatchmaking.GetLobbyData(lobby, TagKey) != TagValue) return;
        bool isHost = SteamMatchmaking.GetLobbyOwner(lobby) == SteamUser.GetSteamID();
        if (isHost) return;
        DuelSession.EnterDuelLobby(lobby, isHost: false);
        // DuelChannel.Send(Core.DuelMessage.Hello());
    }

    private void OnMembersChanged(LobbyChatUpdate_t update)
    {
        if (!DuelSession.IsHostingDuel || update.m_ulSteamIDLobby != DuelSession.LobbyId.m_SteamID) return;
        int members = SteamMatchmaking.GetNumLobbyMembers(DuelSession.LobbyId);
        SteamMatchmaking.SetLobbyJoinable(DuelSession.LobbyId, members < MaxMembers);
    }
}

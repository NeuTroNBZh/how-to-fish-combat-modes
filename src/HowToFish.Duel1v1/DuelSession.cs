using HowToFish.Duel1v1.Core;
using Steamworks;

namespace HowToFish.Duel1v1;

internal static class DuelSession
{
    public const string SaveName = "__duel1v1";

    public static DuelSettings Settings { get; private set; } = DuelSettings.FromRaw(DuelSettings.Defaults, out _);
    public static string DebugPositionKey { get; private set; } = "F9";
    public static bool IsCompatible { get; private set; }
    public static bool IsDuelLobby { get; private set; }
    public static bool IsHost { get; private set; }
    public static bool IsSearching { get; private set; }
    public static ELobbyType? PendingLobbyType { get; private set; }
    public static CSteamID LobbyId { get; private set; } = CSteamID.Nil;
    public static DuelSnapshot Snapshot { get; set; } = DuelSnapshot.Initial;
    public static WireState? Display { get; private set; }
    public static float DisplayReceivedAt { get; private set; }
    public static string Notice { get; private set; } = "";
    public static float NoticeUntil { get; private set; }

    public static bool IsHostingDuel => IsDuelLobby && IsHost;
    public static bool BlocksDamage => IsHostingDuel && Snapshot.Phase != DuelPhase.Fighting;
    public static Side LocalSide => IsHost ? Side.Host : Side.Guest;

    public static void Initialize(DuelSettings settings, string debugPositionKey, bool compatible)
    {
        Settings = settings;
        DebugPositionKey = debugPositionKey;
        IsCompatible = compatible;
    }

    public static void UseSettings(DuelSettings settings) => Settings = settings;

    public static void BeginHosting(ELobbyType lobbyType) => PendingLobbyType = lobbyType;

    public static void EnterDuelLobby(CSteamID lobby, bool isHost)
    {
        IsDuelLobby = true;
        IsHost = isHost;
        LobbyId = lobby;
        PendingLobbyType = null;
        IsSearching = false;
        Snapshot = DuelSnapshot.Initial;
        Display = null;
    }

    public static void SetSearching(bool searching) => IsSearching = searching;

    public static void SetDisplay(WireState state, float receivedAt)
    {
        Display = state;
        DisplayReceivedAt = receivedAt;
    }

    public static void ShowNotice(string text, float now)
    {
        Notice = text;
        NoticeUntil = now + 5f;
    }

    public static void Reset()
    {
        IsDuelLobby = false;
        IsHost = false;
        IsSearching = false;
        PendingLobbyType = null;
        LobbyId = CSteamID.Nil;
        Snapshot = DuelSnapshot.Initial;
        Display = null;
    }
}

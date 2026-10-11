using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;
using Steamworks;

namespace HowToFish.CombatModes;

internal static class CombatSession
{
    public const string SaveName = "__combatmodes";

    public static DuelSettings Settings { get; private set; } = DuelSettings.FromRaw(DuelSettings.Defaults, out _);
    public static ArenaSettings ArenaSettings { get; private set; } = ArenaSettings.FromRaw(ArenaSettings.Defaults, Settings, out _);
    public static string DebugPositionKey { get; private set; } = "F9";
    public static bool IsCompatible { get; private set; }
    public static GameMode Mode { get; private set; } = GameMode.Duel;
    public static bool IsDuelLobby { get; private set; }
    public static bool IsHost { get; private set; }
    public static bool IsSearching { get; private set; }
    public static bool IsLobbyLocked { get; private set; }
    public static ELobbyType? PendingLobbyType { get; private set; }
    public static CSteamID LobbyId { get; private set; } = CSteamID.Nil;
    public static DuelSnapshot Snapshot { get; set; } = DuelSnapshot.Initial;
    public static WireState? Display { get; private set; }
    public static float DisplayReceivedAt { get; private set; }
    public static ArenaSnapshot ArenaSnapshot { get; set; } = ArenaSnapshot.Lobby(GameMode.Deathmatch);
    public static ArenaWire? ArenaDisplay { get; private set; }
    public static float ArenaDisplayReceivedAt { get; private set; }
    public static string Notice { get; private set; } = "";
    public static float NoticeUntil { get; private set; }

    public static bool IsHostingDuel => IsDuelLobby && IsHost && Mode == GameMode.Duel;
    public static bool IsHostingArena => IsDuelLobby && IsHost && Mode != GameMode.Duel;
    public static bool IsArenaLobby => IsDuelLobby && Mode != GameMode.Duel;
    public static bool BlocksDamage => IsHostingDuel && !DuelRules.AllowsDamage(Snapshot, UnityEngine.Time.time);
    public static Side LocalSide => IsHost ? Side.Host : Side.Guest;

    public static void Initialize(DuelSettings settings, ArenaSettings arena, string debugPositionKey, bool compatible)
    {
        Settings = settings;
        ArenaSettings = arena;
        DebugPositionKey = debugPositionKey;
        IsCompatible = compatible;
    }

    public static void UseSettings(DuelSettings settings) => Settings = settings;

    public static void UseArenaSettings(ArenaSettings settings) => ArenaSettings = settings;

    public static void BeginHosting(ELobbyType lobbyType, GameMode mode)
    {
        PendingLobbyType = lobbyType;
        Mode = mode;
    }

    public static void BeginSearch(GameMode mode)
    {
        IsSearching = true;
        Mode = mode;
    }

    public static void EnterDuelLobby(CSteamID lobby, bool isHost, GameMode mode)
    {
        IsDuelLobby = true;
        IsHost = isHost;
        LobbyId = lobby;
        Mode = mode;
        IsLobbyLocked = false;
        PendingLobbyType = null;
        IsSearching = false;
        Snapshot = DuelSnapshot.Initial;
        Display = null;
        ArenaSnapshot = ArenaSnapshot.Lobby(mode == GameMode.Duel ? GameMode.Deathmatch : mode);
        ArenaDisplay = null;
    }

    public static void SetSearching(bool searching) => IsSearching = searching;

    // Réglages invalides : le salon reste fermé même quand un membre part.
    public static void LockLobby() => IsLobbyLocked = true;

    public static void SetDisplay(WireState state, float receivedAt)
    {
        Display = state;
        DisplayReceivedAt = receivedAt;
    }

    public static void SetArenaDisplay(ArenaWire wire, float receivedAt)
    {
        ArenaDisplay = wire;
        ArenaDisplayReceivedAt = receivedAt;
    }

    public static void ShowNotice(string text, float now, float seconds = 5f)
    {
        Notice = text;
        NoticeUntil = now + seconds;
    }

    public static void Reset()
    {
        IsLobbyLocked = false;
        IsDuelLobby = false;
        IsHost = false;
        IsSearching = false;
        PendingLobbyType = null;
        LobbyId = CSteamID.Nil;
        Snapshot = DuelSnapshot.Initial;
        Display = null;
        ArenaSnapshot = ArenaSnapshot.Lobby(GameMode.Deathmatch);
        ArenaDisplay = null;
    }
}

using HowToFish.CombatModes.Core.Arena;

namespace HowToFish.CombatModes.Core;

public enum CombatScreen
{
    None,
    Lobby,
    Countdown,
    Fight,
    End,
}

public enum BannerTone
{
    Good,
    Bad,
    Neutral,
}

public static class CombatUiRules
{
    // With no state received from the host, the player is still in the lobby.
    public static CombatScreen ScreenFor(DuelPhase? phase) => phase switch
    {
        null or DuelPhase.Waiting => CombatScreen.Lobby,
        DuelPhase.Preparing or DuelPhase.Countdown => CombatScreen.Countdown,
        DuelPhase.MatchEnd => CombatScreen.End,
        _ => CombatScreen.Fight,
    };

    public static CombatScreen ScreenFor(ArenaPhase? phase) => phase switch
    {
        null or ArenaPhase.Lobby => CombatScreen.Lobby,
        ArenaPhase.Preparing or ArenaPhase.Countdown => CombatScreen.Countdown,
        ArenaPhase.Ended => CombatScreen.End,
        _ => CombatScreen.Fight,
    };

    public static bool BlocksJournal(bool inCombatLobby) => inCombatLobby;

    public static bool FreesCursor(CombatScreen screen) => screen is CombatScreen.Lobby or CombatScreen.End;

    public static bool BlocksInputs(CombatScreen screen) => FreesCursor(screen);

    // Only the mode host respawns players: the game's respawn drops the kit on the ground.
    public static bool BlocksGameRespawn(bool inCombatLobby) => inCombatLobby;

    // Save and server settings make no sense in a temporary combat game.
    public static bool HidesServerButtons(bool inCombatLobby) => inCombatLobby;
}

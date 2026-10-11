using HowToFish.Duel1v1.Core.Arena;

namespace HowToFish.Duel1v1.Core;

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
    // Sans état reçu de l'hôte, le joueur est encore au salon.
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

    // Seul l'hôte du mode fait réapparaître les joueurs : le respawn du jeu jette le kit au sol.
    public static bool BlocksGameRespawn(bool inCombatLobby) => inCombatLobby;

    // Sauvegarde et réglages du serveur n'ont pas de sens dans une partie de combat temporaire.
    public static bool HidesServerButtons(bool inCombatLobby) => inCombatLobby;
}

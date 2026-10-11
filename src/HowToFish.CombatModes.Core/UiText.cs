using System;

namespace HowToFish.CombatModes.Core;

public enum Language
{
    English,
    French,
}

/// <summary>Every player-facing string of the mod, in one catalog per language.</summary>
public abstract class UiText
{
    public static UiText English { get; } = new EnglishText();
    public static UiText French { get; } = new FrenchText();

    public static UiText For(Language language) => language == Language.French ? French : English;

    public static Language LanguageFromLocale(string? localeCode) =>
        localeCode != null && localeCode.StartsWith("fr", StringComparison.OrdinalIgnoreCase)
            ? Language.French
            : Language.English;

    // Menus
    public abstract string CombatModes { get; }
    public abstract string InviteFriends { get; }
    public abstract string QuickMatch { get; }
    public abstract string Back { get; }
    public abstract string Cancel { get; }
    public abstract string Incompatible { get; }
    public abstract string ModeLabel(GameMode mode);
    public abstract string SearchingLobby(string modeLabel);

    // Lobby and end screen
    public abstract string DuelTitle { get; }
    public abstract string LobbyTitle(string modeLabel, int players, int maxPlayers);
    public abstract string HostSuffix { get; }
    public abstract string WaitingForHost { get; }
    public abstract string WaitingForOpponent { get; }
    public abstract string ForfeitWin { get; }
    public abstract string ConnectingToHost { get; }
    public abstract string Start { get; }
    public abstract string Quit { get; }
    public abstract string Rematch { get; }
    public abstract string PlayAgain { get; }
    public abstract string Scoreboard { get; }
    public abstract string Opponent { get; }
    public abstract string Bot { get; }

    // Fight
    public abstract string PreparingIsland { get; }
    public abstract string Go { get; }
    public abstract string RoundWon { get; }
    public abstract string RoundLost { get; }
    public abstract string RoundDraw { get; }
    public abstract string Victory { get; }
    public abstract string Defeat { get; }
    public abstract string GameOver { get; }
    public abstract string Kill { get; }
    public abstract string Eliminated { get; }
    public abstract string FinalLevel { get; }
    public abstract string PlayerWins(string name);
    public abstract string LevelUp(int level, string weapon);
    public abstract string Rounds(int count);
    public abstract string RoundKit(int round, string kit);
    public abstract string RoundClockKit(int round, string clock, string kit);
    public abstract string ClockKit(string clock, string kit);
    public abstract string LevelScore(int level);
    public abstract string LocalAndLeader(string localScore, string leaderName, string leaderScore);
    public abstract string LevelLine(int level, int levels, string weapon, string? next);
    public abstract string DeathmatchRow(int rank, string name, int kills, int deaths);
    public abstract string GunGameRow(int rank, string name, int level, int kills);

    // Weapon upgrades
    public abstract string Bare { get; }
    public abstract string Laser { get; }
    public abstract string ExtendedMag { get; }
    public abstract string Sharpness(int level);
    public abstract string AmmoLevel(int level);
    public abstract string QuiverLevel(int level);

    // Notices
    public abstract string MatchImpossible(string error);
    public abstract string OpponentWithoutMod { get; }
    public abstract string PlayerWithoutMod(string name);
    public abstract string LobbyUnavailable { get; }
    public abstract string LobbyOtherGameVersion { get; }
    public abstract string SteamRefusedLobby { get; }
    public abstract string NoValidKits { get; }
    public abstract string InvalidLadder { get; }
    public abstract string Position(string coordinates);

    private sealed class EnglishText : UiText
    {
        public override string CombatModes => "Combat Modes";
        public override string InviteFriends => "Invite friends";
        public override string QuickMatch => "Quick match";
        public override string Back => "Back";
        public override string Cancel => "Cancel";
        public override string Incompatible => "This mod is not compatible with this version of the game.";
        public override string ModeLabel(GameMode mode) => mode switch
        {
            GameMode.Duel => "1v1 Duel",
            GameMode.Deathmatch => "Free-for-all",
            _ => "Gun Game",
        };
        public override string SearchingLobby(string modeLabel) => $"Searching for a {modeLabel} lobby…";
        public override string DuelTitle => "1V1 DUEL";
        public override string LobbyTitle(string modeLabel, int players, int maxPlayers) => $"LOBBY — {modeLabel} ({players}/{maxPlayers})";
        public override string HostSuffix => " (host)";
        public override string WaitingForHost => "Waiting for the host…";
        public override string WaitingForOpponent => "Waiting for an opponent…";
        public override string ForfeitWin => "Your opponent left — you win by forfeit.";
        public override string ConnectingToHost => "Connecting to the host…";
        public override string Start => "Start";
        public override string Quit => "Quit";
        public override string Rematch => "Rematch";
        public override string PlayAgain => "Play again";
        public override string Scoreboard => "SCOREBOARD";
        public override string Opponent => "Opponent";
        public override string Bot => "Bot";
        public override string PreparingIsland => "Preparing the island…";
        public override string Go => "GO!";
        public override string RoundWon => "YOU WIN THE ROUND";
        public override string RoundLost => "YOUR OPPONENT WINS THE ROUND";
        public override string RoundDraw => "DRAW";
        public override string Victory => "VICTORY";
        public override string Defeat => "DEFEAT";
        public override string GameOver => "GAME OVER";
        public override string Kill => "KILL";
        public override string Eliminated => "ELIMINATED";
        public override string FinalLevel => "FINAL LEVEL: knife";
        public override string PlayerWins(string name) => $"{name.ToUpperInvariant()} WINS";
        public override string LevelUp(int level, string weapon) => $"LEVEL {level}: {weapon}";
        public override string Rounds(int count) => count == 1 ? "1 round" : $"{count} rounds";
        public override string RoundKit(int round, string kit) => $"Round {round}  ·  Kit: {kit}";
        public override string RoundClockKit(int round, string clock, string kit) => $"Round {round}  ·  {clock}  ·  Kit: {kit}";
        public override string ClockKit(string clock, string kit) => $"{clock}  ·  Kit: {kit}";
        public override string LevelScore(int level) => $"level {level}";
        public override string LocalAndLeader(string localScore, string leaderName, string leaderScore) =>
            $"You: {localScore}  ·  Leader: {leaderName} {leaderScore}";
        public override string LevelLine(int level, int levels, string weapon, string? next) => next == null
            ? $"Level {level}/{levels}: knife, one kill to win"
            : $"Level {level}/{levels}: {weapon}, next {next}";
        public override string DeathmatchRow(int rank, string name, int kills, int deaths) => $"{rank}. {name} — {kills} kills / {deaths} deaths";
        public override string GunGameRow(int rank, string name, int level, int kills) => $"{rank}. {name} — level {level} — {kills} kills";
        public override string Bare => "bare";
        public override string Laser => "laser";
        public override string ExtendedMag => "extended mag";
        public override string Sharpness(int level) => $"sharpness {level}";
        public override string AmmoLevel(int level) => $"ammo lvl {level}";
        public override string QuiverLevel(int level) => $"quiver lvl {level}";
        public override string MatchImpossible(string error) => $"{error} — the match cannot start, go back to the menu";
        public override string OpponentWithoutMod => "Your opponent does not have the Combat Modes mod";
        public override string PlayerWithoutMod(string name) => $"{name} does not have the Combat Modes mod";
        public override string LobbyUnavailable => "This lobby is no longer available, start a new quick match";
        public override string LobbyOtherGameVersion => "This lobby uses a different version of the game";
        public override string SteamRefusedLobby => "Steam refused to create the lobby";
        public override string NoValidKits => "No valid weapon kit in htf.combatmodes.cfg (see the log)";
        public override string InvalidLadder => "Invalid gun game ladder in htf.combatmodes.cfg (at least 2 levels, the last one must be knife)";
        public override string Position(string coordinates) => $"Position: {coordinates}";
    }

    private sealed class FrenchText : UiText
    {
        public override string CombatModes => "Mods de combat";
        public override string InviteFriends => "Inviter des amis";
        public override string QuickMatch => "Partie rapide";
        public override string Back => "Retour";
        public override string Cancel => "Annuler";
        public override string Incompatible => "Mod incompatible avec cette version du jeu.";
        public override string ModeLabel(GameMode mode) => mode switch
        {
            GameMode.Duel => "Duel 1v1",
            GameMode.Deathmatch => "Mêlée générale",
            _ => "Course à l'armement",
        };
        public override string SearchingLobby(string modeLabel) => $"Recherche d'un salon {modeLabel}…";
        public override string DuelTitle => "DUEL 1V1";
        public override string LobbyTitle(string modeLabel, int players, int maxPlayers) => $"SALON — {modeLabel} ({players}/{maxPlayers})";
        public override string HostSuffix => " (hôte)";
        public override string WaitingForHost => "En attente de l'hôte…";
        public override string WaitingForOpponent => "En attente d'un adversaire…";
        public override string ForfeitWin => "L'adversaire a quitté — victoire par forfait.";
        public override string ConnectingToHost => "Connexion à l'hôte…";
        public override string Start => "Lancer";
        public override string Quit => "Quitter";
        public override string Rematch => "Revanche";
        public override string PlayAgain => "Rejouer";
        public override string Scoreboard => "CLASSEMENT";
        public override string Opponent => "Adversaire";
        public override string Bot => "Bot";
        public override string PreparingIsland => "Préparation de l'île…";
        public override string Go => "GO !";
        public override string RoundWon => "TU GAGNES LA MANCHE";
        public override string RoundLost => "L'ADVERSAIRE GAGNE LA MANCHE";
        public override string RoundDraw => "MANCHE NULLE";
        public override string Victory => "VICTOIRE";
        public override string Defeat => "DÉFAITE";
        public override string GameOver => "PARTIE TERMINÉE";
        public override string Kill => "KILL";
        public override string Eliminated => "ÉLIMINÉ";
        public override string FinalLevel => "NIVEAU FINAL : couteau";
        public override string PlayerWins(string name) => $"{name.ToUpperInvariant()} GAGNE";
        public override string LevelUp(int level, string weapon) => $"NIVEAU {level} : {weapon}";
        public override string Rounds(int count) => count <= 1 ? $"{count} manche" : $"{count} manches";
        public override string RoundKit(int round, string kit) => $"Manche {round}  ·  Kit : {kit}";
        public override string RoundClockKit(int round, string clock, string kit) => $"Manche {round}  ·  {clock}  ·  Kit : {kit}";
        public override string ClockKit(string clock, string kit) => $"{clock}  ·  Kit : {kit}";
        public override string LevelScore(int level) => $"niveau {level}";
        public override string LocalAndLeader(string localScore, string leaderName, string leaderScore) =>
            $"Toi : {localScore}  ·  Leader : {leaderName} {leaderScore}";
        public override string LevelLine(int level, int levels, string weapon, string? next) => next == null
            ? $"Niveau {level}/{levels} : couteau, un kill pour gagner"
            : $"Niveau {level}/{levels} : {weapon}, ensuite {next}";
        public override string DeathmatchRow(int rank, string name, int kills, int deaths) => $"{rank}. {name} — {kills} kills / {deaths} morts";
        public override string GunGameRow(int rank, string name, int level, int kills) => $"{rank}. {name} — niveau {level} — {kills} kills";
        public override string Bare => "nu";
        public override string Laser => "laser";
        public override string ExtendedMag => "chargeur étendu";
        public override string Sharpness(int level) => $"affûtage {level}";
        public override string AmmoLevel(int level) => $"munitions niv. {level}";
        public override string QuiverLevel(int level) => $"carquois niv. {level}";
        public override string MatchImpossible(string error) => $"{error} — partie impossible, retourne au menu";
        public override string OpponentWithoutMod => "Ton adversaire n'a pas le mod Combat Modes";
        public override string PlayerWithoutMod(string name) => $"{name} n'a pas le mod Combat Modes";
        public override string LobbyUnavailable => "Ce salon n'est plus disponible, relance une partie rapide";
        public override string LobbyOtherGameVersion => "Ce salon utilise une autre version du jeu";
        public override string SteamRefusedLobby => "Steam a refusé la création du salon";
        public override string NoValidKits => "Aucun kit d'armes valide dans htf.combatmodes.cfg (voir le log)";
        public override string InvalidLadder => "Échelle d'armes invalide dans htf.combatmodes.cfg (2 niveaux minimum, le dernier doit être knife)";
        public override string Position(string coordinates) => $"Position : {coordinates}";
    }
}

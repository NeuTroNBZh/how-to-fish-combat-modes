using System.Collections.Generic;

namespace HowToFish.CombatModes.Core;

public enum GameMode
{
    Duel,
    Deathmatch,
    GunGame,
}

public static class GameModes
{
    public const int ArenaMaxMembers = 8;
    private const int DuelMaxMembers = 2;

    public static IReadOnlyList<GameMode> All { get; } = new[] { GameMode.Duel, GameMode.Deathmatch, GameMode.GunGame };

    public static string Tag(GameMode mode) => mode switch
    {
        GameMode.Duel => "duel",
        GameMode.Deathmatch => "ffa",
        _ => "gungame",
    };

    public static bool TryParseTag(string? tag, out GameMode mode)
    {
        foreach (var candidate in All)
        {
            if (Tag(candidate) != tag) continue;
            mode = candidate;
            return true;
        }
        mode = GameMode.Duel;
        return false;
    }

    public static int MaxMembers(GameMode mode) => mode == GameMode.Duel ? DuelMaxMembers : ArenaMaxMembers;

    public static string Label(GameMode mode) => mode switch
    {
        GameMode.Duel => "Duel 1v1",
        GameMode.Deathmatch => "Mêlée générale",
        _ => "Course à l'armement",
    };
}

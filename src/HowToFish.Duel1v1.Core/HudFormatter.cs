using System;

namespace HowToFish.Duel1v1.Core;

public static class HudFormatter
{
    private const string Waiting = "En attente d'un adversaire…";

    public static string Clock(double seconds)
    {
        var total = (int)Math.Ceiling(Math.Max(0, seconds));
        return $"{total / 60}:{total % 60:00}";
    }

    public static int CountdownNumber(double seconds) => (int)Math.Ceiling(Math.Max(0, seconds));

    public static string ScoreLine(WireState s, Side local, string localName, string opponentName) =>
        $"{localName} {Score(s, local)} - {Score(s, local.Other())} {opponentName}";

    public static string RoundBanner(WireState s, Side local) =>
        s.LastRoundDraw || s.LastRoundWinner is null ? "MANCHE NULLE"
        : s.LastRoundWinner == local ? "TU GAGNES LA MANCHE" : "L'ADVERSAIRE GAGNE LA MANCHE";

    public static string MatchBanner(WireState s, Side local) =>
        $"{(s.MatchWinner == local ? "VICTOIRE" : "DÉFAITE")} {Score(s, local)} - {Score(s, local.Other())}";

    public static string WaitingText(WireState? s, Side local) =>
        s is { Forfeit: true } && s.MatchWinner == local
            ? $"L'adversaire a quitté — victoire par forfait. {Waiting}"
            : Waiting;

    private static int Score(WireState s, Side side) => side == Side.Host ? s.HostScore : s.GuestScore;
}

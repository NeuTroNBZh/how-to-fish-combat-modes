using System;
using System.Collections.Generic;

namespace HowToFish.CombatModes.Core;

public static class HudFormatter
{
    public static string Clock(double seconds)
    {
        var total = (int)Math.Ceiling(Math.Max(0, seconds));
        return $"{total / 60}:{total % 60:00}";
    }

    public static int CountdownNumber(double seconds) => (int)Math.Ceiling(Math.Max(0, seconds));

    public static string ScoreLine(WireState s, Side local, string localName, string opponentName) =>
        $"{localName} {Score(s, local)} - {Score(s, local.Other())} {opponentName}";

    public static string RoundBanner(UiText t, WireState s, Side local) =>
        s.LastRoundDraw || s.LastRoundWinner is null ? t.RoundDraw
        : s.LastRoundWinner == local ? t.RoundWon : t.RoundLost;

    public static string MatchBanner(UiText t, WireState s, Side local) =>
        $"{(s.MatchWinner == local ? t.Victory : t.Defeat)} {Score(s, local)} - {Score(s, local.Other())}";

    public static string WaitingText(UiText t, WireState? s, Side local) =>
        s is { Forfeit: true } && s.MatchWinner == local
            ? $"{t.ForfeitWin} {t.WaitingForOpponent}"
            : t.WaitingForOpponent;

    public static BannerTone RoundTone(WireState s, Side local) =>
        s.LastRoundDraw || s.LastRoundWinner is null ? BannerTone.Neutral
        : s.LastRoundWinner == local ? BannerTone.Good : BannerTone.Bad;

    public static BannerTone MatchTone(WireState s, Side local) =>
        s.MatchWinner == local ? BannerTone.Good : BannerTone.Bad;

    public static IReadOnlyList<string> ScoreboardRows(UiText t, WireState s, Side local, string localName, string opponentName) => new[]
    {
        $"{localName} — {t.Rounds(Score(s, local))}",
        $"{opponentName} — {t.Rounds(Score(s, local.Other()))}",
        t.RoundKit(s.Round, s.KitName),
    };

    private static int Score(WireState s, Side side) => side == Side.Host ? s.HostScore : s.GuestScore;
}

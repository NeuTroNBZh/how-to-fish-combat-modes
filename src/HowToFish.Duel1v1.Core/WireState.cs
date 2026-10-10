using System;

namespace HowToFish.Duel1v1.Core;

public sealed record WireState(
    DuelPhase Phase, int Round, int HostScore, int GuestScore, int RemainingMs, string KitName,
    Side? LastRoundWinner, bool LastRoundDraw, Side? MatchWinner, bool Forfeit, bool HostAtSpawnA)
{
    public static WireState From(DuelSnapshot s, double now) => new(
        s.Phase, s.Round, s.HostScore, s.GuestScore,
        (int)Math.Round(Math.Max(0, s.PhaseEndsAt - now) * 1000),
        s.Kit?.Name ?? "", s.LastRoundWinner, s.LastRoundDraw, s.MatchWinner, s.Forfeit, s.HostAtSpawnA);
}

public enum MessageKind
{
    Hello,
    State,
    Rematch,
}

public sealed record DuelMessage(MessageKind Kind, WireState? State)
{
    public static DuelMessage Hello() => new(MessageKind.Hello, null);
    public static DuelMessage Rematch() => new(MessageKind.Rematch, null);
    public static DuelMessage Of(WireState state) => new(MessageKind.State, state);
}

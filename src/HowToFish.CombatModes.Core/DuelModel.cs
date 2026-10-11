namespace HowToFish.CombatModes.Core;

public enum DuelPhase
{
    Waiting = 0,
    Preparing = 1,
    Countdown = 2,
    Fighting = 3,
    RoundEnd = 4,
    MatchEnd = 5,
}

public enum DuelEffect
{
    None,
    PrepareRound,
    StartFight,
    AnnounceRoundResult,
    AnnounceMatchResult,
    ResetToWaiting,
}

public sealed record DuelSnapshot(
    DuelPhase Phase, int Round, int HostScore, int GuestScore, double PhaseEndsAt, Kit? Kit,
    Side? LastRoundWinner, bool LastRoundDraw, Side? MatchWinner, bool Forfeit, bool HostAtSpawnA,
    double RoundDecidedAt, int Island)
{
    public static DuelSnapshot Initial { get; } =
        new(DuelPhase.Waiting, 0, 0, 0, 0, null, null, false, null, false, true, 0, 0);

    public int ScoreOf(Side side) => side == Side.Host ? HostScore : GuestScore;
}

public abstract record DuelEvent;
public sealed record PlayersReady : DuelEvent;
public sealed record PlayerLeft(Side Side) : DuelEvent;
public sealed record PlayerDied(Side Side) : DuelEvent;
public sealed record Tick(int HostHealth, int GuestHealth) : DuelEvent;
public sealed record RematchRequested : DuelEvent;

public sealed record DuelStep(DuelSnapshot Snapshot, DuelEffect Effect);

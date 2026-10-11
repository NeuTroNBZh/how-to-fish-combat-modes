namespace HowToFish.CombatModes.Core;

public sealed class DuelRules
{
    private readonly DuelSettings _settings;
    private readonly IRandomSource _random;

    public DuelRules(DuelSettings settings, IRandomSource random)
    {
        _settings = settings;
        _random = random;
    }

    public DuelStep Apply(DuelSnapshot snapshot, DuelEvent duelEvent, double now) => duelEvent switch
    {
        PlayerLeft left => OnPlayerLeft(snapshot, left.Side),
        PlayersReady => snapshot.Phase == DuelPhase.Waiting ? StartMatch(null, now) : Same(snapshot),
        RematchRequested => snapshot.Phase == DuelPhase.MatchEnd ? StartMatch(snapshot.Island, now) : Same(snapshot),
        PlayerDied died => OnPlayerDied(snapshot, died.Side, now),
        Tick tick => OnTick(snapshot, tick, now),
        _ => Same(snapshot),
    };

    // The window after round end lets through the second hit of the same explosion (double KO).
    public static bool AllowsDamage(DuelSnapshot s, double now) =>
        s.Phase == DuelPhase.Fighting
        || (s.Phase == DuelPhase.RoundEnd && !s.LastRoundDraw
            && now - s.RoundDecidedAt <= DuelSettings.DoubleKoWindowSeconds);

    private static DuelStep Same(DuelSnapshot s) => new(s, DuelEffect.None);

    private DuelStep StartMatch(int? previousIsland, double now)
    {
        var kit = KitPicker.Pick(_settings.Kits, null, _random);
        var s = DuelSnapshot.Initial with
        {
            Phase = DuelPhase.Preparing, Round = 1, Kit = kit, PhaseEndsAt = now + DuelSettings.PrepareSeconds,
            Island = IslandPicker.Pick(_settings.Islands, previousIsland, _random),
        };
        return new DuelStep(s, DuelEffect.PrepareRound);
    }

    private static DuelStep OnPlayerLeft(DuelSnapshot s, Side side)
    {
        if (s.Phase == DuelPhase.Waiting) return Same(s);
        var waiting = DuelSnapshot.Initial with { Forfeit = true, MatchWinner = side.Other() };
        return new DuelStep(waiting, DuelEffect.ResetToWaiting);
    }

    private static DuelStep OnPlayerDied(DuelSnapshot s, Side side, double now)
    {
        if (s.Phase == DuelPhase.Fighting) return EndRound(s, side.Other(), now);
        bool isDoubleKo = s.Phase == DuelPhase.RoundEnd && !s.LastRoundDraw && s.LastRoundWinner == side
            && now - s.RoundDecidedAt <= DuelSettings.DoubleKoWindowSeconds;
        if (!isDoubleKo) return Same(s);
        var draw = s with
        {
            HostScore = side == Side.Host ? s.HostScore - 1 : s.HostScore,
            GuestScore = side == Side.Guest ? s.GuestScore - 1 : s.GuestScore,
            LastRoundWinner = null,
            LastRoundDraw = true,
        };
        return new DuelStep(draw, DuelEffect.AnnounceRoundResult);
    }

    private static DuelStep EndRound(DuelSnapshot s, Side? winner, double now)
    {
        var ended = s with
        {
            Phase = DuelPhase.RoundEnd,
            HostScore = winner == Side.Host ? s.HostScore + 1 : s.HostScore,
            GuestScore = winner == Side.Guest ? s.GuestScore + 1 : s.GuestScore,
            LastRoundWinner = winner,
            LastRoundDraw = winner is null,
            RoundDecidedAt = now,
            PhaseEndsAt = now + DuelSettings.RoundEndSeconds,
        };
        return new DuelStep(ended, DuelEffect.AnnounceRoundResult);
    }

    private DuelStep OnTick(DuelSnapshot s, Tick tick, double now)
    {
        if (now < s.PhaseEndsAt) return Same(s);
        return s.Phase switch
        {
            DuelPhase.Preparing => Same(s with
            {
                Phase = DuelPhase.Countdown, PhaseEndsAt = now + _settings.CountdownSeconds,
            }),
            DuelPhase.Countdown => new DuelStep(
                s with { Phase = DuelPhase.Fighting, PhaseEndsAt = now + _settings.RoundTimeSeconds },
                DuelEffect.StartFight),
            DuelPhase.Fighting => EndRound(s, TimeoutWinner(tick), now),
            DuelPhase.RoundEnd => AfterRound(s, now),
            _ => Same(s),
        };
    }

    private static Side? TimeoutWinner(Tick tick) =>
        tick.HostHealth == tick.GuestHealth ? null
        : tick.HostHealth > tick.GuestHealth ? Side.Host : Side.Guest;

    private DuelStep AfterRound(DuelSnapshot s, double now)
    {
        if (s.LastRoundWinner is Side winner && s.ScoreOf(winner) >= _settings.RoundsToWin)
        {
            return new DuelStep(s with { Phase = DuelPhase.MatchEnd, MatchWinner = winner }, DuelEffect.AnnounceMatchResult);
        }
        var next = s with
        {
            Phase = DuelPhase.Preparing,
            Round = s.LastRoundDraw ? s.Round : s.Round + 1,
            Kit = KitPicker.Pick(_settings.Kits, s.Kit, _random),
            Island = IslandPicker.Pick(_settings.Islands, s.Island, _random),
            HostAtSpawnA = !s.HostAtSpawnA,
            LastRoundWinner = null,
            LastRoundDraw = false,
            PhaseEndsAt = now + DuelSettings.PrepareSeconds,
        };
        return new DuelStep(next, DuelEffect.PrepareRound);
    }
}

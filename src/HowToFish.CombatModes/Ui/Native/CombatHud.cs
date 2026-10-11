using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;
using TMPro;
using UnityEngine;

namespace HowToFish.CombatModes.Ui.Native;

internal sealed class CombatHud : NativeScreen
{
    private const float BannerSeconds = 2f;
    private const float BannerFade = 0.5f;
    private const float CountdownPulse = 0.35f;

    private TextMeshProUGUI? _top, _sub, _loadout, _countdown, _banner;
    private DuelPhase? _lastDuelPhase;
    private ArenaWire? _lastArena;
    private float _bannerUntil;

    protected override bool ShouldShow(CombatScreen screen) => screen is CombatScreen.Countdown or CombatScreen.Fight;

    protected override string Key() => CombatUiState.IsDuel ? "duel" : "arena";

    protected override void Build(RectTransform layer)
    {
        var bar = GameUiKit.Panel(layer, new Vector2(820, 150)).rectTransform;
        bar.anchorMin = new Vector2(0.5f, 1f);
        bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0, -16);
        _top = GameUiKit.Label(bar, "", 32, GameUiKit.TextColor);
        GameUiKit.Fit(_top, 44);
        _sub = GameUiKit.Label(bar, "", 24, GameUiKit.TextColor);
        GameUiKit.Fit(_sub, 32);
        _loadout = GameUiKit.Label(bar, "", 18, GameUiKit.TextColor);
        GameUiKit.Fit(_loadout, 26);
        _countdown = Centered(layer, 160, 0);
        _banner = Centered(layer, 64, 160);
    }

    private static TextMeshProUGUI Centered(RectTransform layer, float size, float y)
    {
        var label = GameUiKit.Label(layer, "", size, GameUiKit.TextColor);
        label.rectTransform.sizeDelta = new Vector2(1400, size * 1.5f);
        label.rectTransform.anchoredPosition = new Vector2(0, y);
        return label;
    }

    protected override void Tick()
    {
        if (_top == null || _sub == null || _loadout == null || _countdown == null || _banner == null) return;
        if (CombatUiState.IsDuel) TickDuel();
        else TickArena();
        _loadout.text = CombatControllers.LoadoutText;
        TickCountdown();
        TickBanner();
    }

    private void TickDuel()
    {
        var s = CombatSession.Display;
        if (s == null) return;
        var local = CombatSession.LocalSide;
        _top!.text = HudFormatter.ScoreLine(s, local, CombatUiState.NameOf(CombatUiState.LocalId), CombatUiState.OpponentName);
        _sub!.text = Lang.Text.RoundClockKit(s.Round, HudFormatter.Clock(CombatUiState.Remaining()), s.KitName);
        if (s.Phase != _lastDuelPhase)
        {
            if (s.Phase == DuelPhase.RoundEnd) ShowBanner(HudFormatter.RoundBanner(Lang.Text, s, local), HudFormatter.RoundTone(s, local));
            if (s.Phase == DuelPhase.Fighting && _lastDuelPhase == DuelPhase.Countdown) ShowBanner(Lang.Text.Go, BannerTone.Good);
            _lastDuelPhase = s.Phase;
        }
    }

    private void TickArena()
    {
        var w = CombatSession.ArenaDisplay;
        if (w == null) return;
        _top!.text = ArenaHudFormatter.TopLine(Lang.Text, w, CombatUiState.LocalId, CombatUiState.NameOf);
        _sub!.text = ArenaHudFormatter.SubLine(Lang.Text, w, CombatUiState.LocalId, CombatUiState.Remaining());
        if (ReferenceEquals(w, _lastArena)) return;
        if (w.Phase == ArenaPhase.Playing && _lastArena?.Phase == ArenaPhase.Countdown) ShowBanner(Lang.Text.Go, BannerTone.Good);
        else if (ArenaHudFormatter.BannerFor(Lang.Text, _lastArena, w, CombatUiState.LocalId) is { } banner) ShowBanner(banner.Text, banner.Tone);
        _lastArena = w;
    }

    // Each countdown second starts large and opaque, then shrinks away.
    private void TickCountdown()
    {
        if (CombatUiState.Screen != CombatScreen.Countdown)
        {
            _countdown!.text = "";
            return;
        }
        bool preparing = CombatUiState.IsDuel
            ? CombatSession.Display?.Phase == DuelPhase.Preparing
            : CombatSession.ArenaDisplay?.Phase == ArenaPhase.Preparing;
        if (preparing)
        {
            _countdown!.text = $"<size=50%>{Lang.Text.PreparingIsland}</size>";
            _countdown.transform.localScale = Vector3.one;
            _countdown.alpha = 1f;
            return;
        }
        var remaining = CombatUiState.Remaining();
        var fraction = (float)(remaining - System.Math.Floor(remaining));
        _countdown!.text = HudFormatter.CountdownNumber(remaining).ToString();
        _countdown.transform.localScale = Vector3.one * (1f + CountdownPulse * fraction);
        _countdown.alpha = 0.4f + 0.6f * fraction;
    }

    private void ShowBanner(string text, BannerTone tone)
    {
        _banner!.text = text;
        _banner.color = ToneColors.Of(tone);
        _bannerUntil = Time.time + BannerSeconds;
    }

    private void TickBanner()
    {
        var left = _bannerUntil - Time.time;
        _banner!.alpha = left <= 0 ? 0f : Mathf.Clamp01(left / BannerFade);
    }
}

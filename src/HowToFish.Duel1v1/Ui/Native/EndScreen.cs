using System.Linq;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;
using UnityEngine;
using UnityEngine.UI;

namespace HowToFish.Duel1v1.Ui.Native;

internal static class ToneColors
{
    public static Color Of(BannerTone tone) => tone switch
    {
        BannerTone.Good => GameInfo.GreenColor,
        BannerTone.Bad => GameInfo.RedColor,
        _ => GameInfo.OrangeColor,
    };
}

internal sealed class EndScreen : NativeScreen
{
    private const float RowHeight = 40f;
    private static readonly Color Dim = new(0f, 0f, 0f, 0.55f);

    protected override bool ShouldShow(CombatScreen screen) => screen == CombatScreen.End;

    protected override string Key()
    {
        if (CombatUiState.IsDuel)
        {
            var s = DuelSession.Display;
            return s == null ? "duel" : $"duel|{s.MatchWinner}|{s.HostScore}-{s.GuestScore}";
        }
        var w = DuelSession.ArenaDisplay;
        return w == null ? "arena" : $"arena|{w.Winner}|{string.Join(",", w.Players.Select(p => $"{p.SteamId}:{p.Kills}:{p.Level}"))}|{DuelSession.IsHost}";
    }

    protected override void Build(RectTransform layer)
    {
        AddDim(layer);
        if (CombatUiState.IsDuel) BuildDuel(layer);
        else BuildArena(layer);
    }

    private static void AddDim(RectTransform layer)
    {
        var go = new GameObject("HTF_Dim", typeof(RectTransform));
        go.transform.SetParent(layer, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = Dim;
    }

    private static void BuildDuel(RectTransform layer)
    {
        var s = DuelSession.Display;
        if (s == null) return;
        var local = DuelSession.LocalSide;
        var panel = GameUiKit.Panel(layer, new Vector2(700, 360)).transform;
        GameUiKit.Fit(GameUiKit.Label(panel, HudFormatter.MatchBanner(s, local), 56, ToneColors.Of(HudFormatter.MatchTone(s, local))), 90);
        var score = HudFormatter.ScoreLine(s, local, CombatUiState.NameOf(CombatUiState.LocalId), CombatUiState.OpponentName);
        GameUiKit.Fit(GameUiKit.Label(panel, score, 30, GameUiKit.TextColor), 50);
        GameUiKit.Fit(GameUiKit.Button(panel, "Revanche", CombatActions.Replay), 64);
        GameUiKit.Fit(GameUiKit.Button(panel, "Quitter", CombatActions.Quit), 64);
    }

    private static void BuildArena(RectTransform layer)
    {
        var w = DuelSession.ArenaDisplay;
        if (w == null) return;
        var ranking = ArenaHudFormatter.Ranking(w);
        var panel = GameUiKit.Panel(layer, new Vector2(760, 330 + ranking.Count * RowHeight)).transform;
        var tone = w.Winner == CombatUiState.LocalId ? BannerTone.Good : BannerTone.Neutral;
        GameUiKit.Fit(GameUiKit.Label(panel, ArenaHudFormatter.EndBanner(w, CombatUiState.LocalId, CombatUiState.NameOf), 52, ToneColors.Of(tone)), 84);
        for (int i = 0; i < ranking.Count; i++)
        {
            var color = ranking[i].SteamId == CombatUiState.LocalId ? GameInfo.GreenColor : GameUiKit.TextColor;
            GameUiKit.Fit(GameUiKit.Label(panel, ArenaHudFormatter.Row(i + 1, ranking[i], w.Mode, CombatUiState.NameOf), 26, color), RowHeight);
        }
        AddReplay(panel, w.Players.Count >= ArenaRules.MinPlayers);
        GameUiKit.Fit(GameUiKit.Button(panel, "Quitter", CombatActions.Quit), 64);
    }

    private static void AddReplay(Transform panel, bool enoughPlayers)
    {
        if (!DuelSession.IsHost)
        {
            GameUiKit.Fit(GameUiKit.Label(panel, "En attente de l'hôte…", 24, GameInfo.OrangeColor), 40);
            return;
        }
        var replay = GameUiKit.Button(panel, "Rejouer", CombatActions.Replay);
        replay.interactable = enoughPlayers;
        GameUiKit.Fit(replay, 64);
    }
}

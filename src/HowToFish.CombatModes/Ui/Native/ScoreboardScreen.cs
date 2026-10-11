using System.Collections.Generic;
using System.Linq;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;
using UnityEngine;

namespace HowToFish.CombatModes.Ui.Native;

// While the journal key (action "PlayerTab") is held: the player's key rebinding is respected.
internal sealed class ScoreboardScreen : NativeScreen
{
    private const float RowHeight = 40f;

    protected override bool ShouldShow(CombatScreen screen) =>
        screen is CombatScreen.Countdown or CombatScreen.Fight && TabHeld();

    private static bool TabHeld()
    {
        try
        {
            var input = GameInfo.Input;
            return input != null && input.actions["PlayerTab"].IsPressed();
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    protected override string Key() => string.Join("\n", Rows().Select(r => r.Text));

    protected override void Build(RectTransform layer)
    {
        var rows = Rows();
        var panel = GameUiKit.Panel(layer, new Vector2(720, 130 + rows.Count * RowHeight)).transform;
        GameUiKit.Fit(GameUiKit.Label(panel, CombatUiState.IsDuel ? Lang.Text.DuelTitle : Lang.Text.Scoreboard, 40, GameUiKit.TextColor), 60);
        foreach (var row in rows) GameUiKit.Fit(GameUiKit.Label(panel, row.Text, 26, row.Color), RowHeight);
    }

    private static IReadOnlyList<(string Text, Color Color)> Rows()
    {
        if (CombatUiState.IsDuel)
        {
            var s = CombatSession.Display;
            if (s == null) return new List<(string, Color)>();
            var lines = HudFormatter.ScoreboardRows(Lang.Text, s, CombatSession.LocalSide, CombatUiState.NameOf(CombatUiState.LocalId), CombatUiState.OpponentName);
            return lines.Select((text, i) => (text, i == 0 ? GameInfo.GreenColor : GameUiKit.TextColor)).ToList();
        }
        var w = CombatSession.ArenaDisplay;
        if (w == null) return new List<(string, Color)>();
        var ranking = ArenaHudFormatter.Ranking(w);
        return ranking.Select((p, i) => (ArenaHudFormatter.Row(Lang.Text, i + 1, p, w.Mode, CombatUiState.NameOf),
            p.SteamId == CombatUiState.LocalId ? GameInfo.GreenColor : GameUiKit.TextColor)).ToList();
    }
}

using System.Collections.Generic;
using System.Linq;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;
using HowToFish.CombatModes.Net;
using Steamworks;
using UnityEngine;

namespace HowToFish.CombatModes.Ui.Native;

internal sealed class LobbyScreen : NativeScreen
{
    private const float RowHeight = 44f;

    protected override bool ShouldShow(CombatScreen screen) => screen == CombatScreen.Lobby;

    protected override string Key() => $"{Title()}|{string.Join(",", Names())}|{CombatSession.IsHost}|{CanStart()}";

    protected override void Build(RectTransform layer)
    {
        var names = Names();
        var panel = GameUiKit.Panel(layer, new Vector2(640, 300 + names.Count * RowHeight)).transform;
        GameUiKit.Fit(GameUiKit.Label(panel, Title(), 40, GameUiKit.TextColor), 60);
        foreach (var name in names) GameUiKit.Fit(GameUiKit.Label(panel, name, 28, GameUiKit.TextColor), RowHeight);
        AddStatusOrStart(panel);
        GameUiKit.Fit(GameUiKit.Button(panel, Lang.Text.Quit, CombatActions.Quit), 64);
    }

    private static void AddStatusOrStart(Transform panel)
    {
        if (CombatUiState.IsDuel)
        {
            var waiting = HudFormatter.WaitingText(Lang.Text, CombatSession.Display, CombatSession.LocalSide);
            GameUiKit.Fit(GameUiKit.Label(panel, waiting, 24, GameInfo.OrangeColor), 40);
            return;
        }
        if (!CombatSession.IsHost)
        {
            GameUiKit.Fit(GameUiKit.Label(panel, Lang.Text.WaitingForHost, 24, GameInfo.OrangeColor), 40);
            return;
        }
        var start = GameUiKit.Button(panel, Lang.Text.Start, CombatActions.Replay);
        start.interactable = CanStart();
        GameUiKit.Fit(start, 64);
    }

    private static string Title()
    {
        if (CombatUiState.IsDuel) return Lang.Text.DuelTitle;
        var count = CombatSession.ArenaDisplay?.Players.Count ?? 0;
        return Lang.Text.LobbyTitle(Lang.Text.ModeLabel(CombatSession.Mode), count, ArenaSettings.MaxPlayers);
    }

    private static bool CanStart() => (CombatSession.ArenaDisplay?.Players.Count ?? 0) >= ArenaRules.MinPlayers;

    private static IReadOnlyList<string> Names()
    {
        var owner = CombatUiState.OwnerId;
        IEnumerable<ulong> ids = CombatUiState.IsDuel
            ? new[] { CombatUiState.LocalId, CombatLobby.Opponent().m_SteamID }.Where(id => id != CSteamID.Nil.m_SteamID)
            : (CombatSession.ArenaDisplay?.Players.Select(p => p.SteamId) ?? Enumerable.Empty<ulong>());
        return ids.Select(id => id == owner ? $"{CombatUiState.NameOf(id)}{Lang.Text.HostSuffix}" : CombatUiState.NameOf(id)).ToList();
    }
}

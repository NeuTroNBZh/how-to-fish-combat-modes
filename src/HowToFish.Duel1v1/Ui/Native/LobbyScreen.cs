using System.Collections.Generic;
using System.Linq;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;
using HowToFish.Duel1v1.Net;
using Steamworks;
using UnityEngine;

namespace HowToFish.Duel1v1.Ui.Native;

internal sealed class LobbyScreen : NativeScreen
{
    private const float RowHeight = 44f;

    protected override bool ShouldShow(CombatScreen screen) => screen == CombatScreen.Lobby;

    protected override string Key() => $"{Title()}|{string.Join(",", Names())}|{DuelSession.IsHost}|{CanStart()}";

    protected override void Build(RectTransform layer)
    {
        var names = Names();
        var panel = GameUiKit.Panel(layer, new Vector2(640, 300 + names.Count * RowHeight)).transform;
        GameUiKit.Fit(GameUiKit.Label(panel, Title(), 40, GameUiKit.TextColor), 60);
        foreach (var name in names) GameUiKit.Fit(GameUiKit.Label(panel, name, 28, GameUiKit.TextColor), RowHeight);
        AddStatusOrStart(panel);
        GameUiKit.Fit(GameUiKit.Button(panel, "Quitter", CombatActions.Quit), 64);
    }

    private static void AddStatusOrStart(Transform panel)
    {
        if (CombatUiState.IsDuel)
        {
            var waiting = HudFormatter.WaitingText(DuelSession.Display, DuelSession.LocalSide);
            GameUiKit.Fit(GameUiKit.Label(panel, waiting, 24, GameInfo.OrangeColor), 40);
            return;
        }
        if (!DuelSession.IsHost)
        {
            GameUiKit.Fit(GameUiKit.Label(panel, "En attente de l'hôte…", 24, GameInfo.OrangeColor), 40);
            return;
        }
        var start = GameUiKit.Button(panel, "Lancer", CombatActions.Replay);
        start.interactable = CanStart();
        GameUiKit.Fit(start, 64);
    }

    private static string Title()
    {
        if (CombatUiState.IsDuel) return "DUEL 1V1";
        var count = DuelSession.ArenaDisplay?.Players.Count ?? 0;
        return $"SALON — {GameModes.Label(DuelSession.Mode)} ({count}/{ArenaSettings.MaxPlayers})";
    }

    private static bool CanStart() => (DuelSession.ArenaDisplay?.Players.Count ?? 0) >= ArenaRules.MinPlayers;

    private static IReadOnlyList<string> Names()
    {
        var owner = CombatUiState.OwnerId;
        IEnumerable<ulong> ids = CombatUiState.IsDuel
            ? new[] { CombatUiState.LocalId, DuelLobby.Opponent().m_SteamID }.Where(id => id != CSteamID.Nil.m_SteamID)
            : (DuelSession.ArenaDisplay?.Players.Select(p => p.SteamId) ?? Enumerable.Empty<ulong>());
        return ids.Select(id => id == owner ? $"{CombatUiState.NameOf(id)} (hôte)" : CombatUiState.NameOf(id)).ToList();
    }
}

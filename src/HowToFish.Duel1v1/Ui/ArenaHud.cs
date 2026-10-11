using System.Linq;
using HowToFish.Duel1v1.Arena;
using HowToFish.Duel1v1.Host;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;
using Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;

using HowToFish.Duel1v1.Ui.Native;

namespace HowToFish.Duel1v1.Ui;

internal sealed class ArenaHud : MonoBehaviour
{
    private const float ReferenceHeight = 1080f;
    private const float PanelWidth = 560f;
    private const float RowHeight = 34f;
    private GUIStyle? _top, _big, _banner, _row;

    private void OnGUI()
    {
        if (GameUiKit.HudReady) return;
        if (!DuelSession.IsArenaLobby) return;
        EnsureStyles();
        var scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        var width = Screen.width / scale;
        DrawNotice(width);
        var w = DuelSession.ArenaDisplay;
        if (w == null)
        {
            GUI.Label(new Rect(0, 40, width, 60), "Connexion à l'hôte…", _top);
            return;
        }
        DrawPhase(w, width);
    }

    private void DrawPhase(ArenaWire w, float width)
    {
        switch (w.Phase)
        {
            case ArenaPhase.Lobby:
                DrawLobby(w, width);
                break;
            case ArenaPhase.Preparing:
                GUI.Label(new Rect(0, 40, width, 60), "Préparation de l'île…", _top);
                break;
            case ArenaPhase.Countdown:
                DrawTop(w, width);
                GUI.Label(new Rect(0, 380, width, 200), HudFormatter.CountdownNumber(Remaining(w)).ToString(), _big);
                break;
            case ArenaPhase.Playing:
                DrawTop(w, width);
                if (Keyboard.current?.tabKey.isPressed == true) DrawScoreboard(w, width, 200);
                break;
            case ArenaPhase.Ended:
                DrawEnd(w, width);
                break;
        }
    }

    private static double Remaining(ArenaWire w) => w.RemainingMs / 1000.0 - (Time.time - DuelSession.ArenaDisplayReceivedAt);

    private void DrawTop(ArenaWire w, float width)
    {
        GUI.Box(new Rect(width / 2 - 320, 20, 640, 90), GUIContent.none);
        GUI.Label(new Rect(width / 2 - 320, 25, 640, 45), ArenaHudFormatter.TopLine(w, CombatUiState.LocalId, CombatUiState.NameOf), _top);
        GUI.Label(new Rect(width / 2 - 320, 65, 640, 40), ArenaHudFormatter.SubLine(w, CombatUiState.LocalId, Remaining(w)), _row);
    }

    private void DrawLobby(ArenaWire w, float width)
    {
        var x = width / 2 - PanelWidth / 2;
        GUI.Box(new Rect(x, 200, PanelWidth, 160 + w.Players.Count * RowHeight), GUIContent.none);
        GUI.Label(new Rect(x, 210, PanelWidth, 40), $"SALON — {GameModes.Label(w.Mode)} ({w.Players.Count}/{ArenaSettings.MaxPlayers})", _top);
        for (int i = 0; i < w.Players.Count; i++)
            GUI.Label(new Rect(x, 260 + i * RowHeight, PanelWidth, RowHeight), CombatUiState.NameOf(w.Players[i].SteamId), _row);
        var y = 270 + w.Players.Count * RowHeight;
        if (DuelSession.IsHost)
        {
            GUI.enabled = w.Players.Count >= ArenaRules.MinPlayers;
            if (GUI.Button(new Rect(x + 40, y, 220, 50), "Lancer")) CombatActions.Replay();
            GUI.enabled = true;
        }
        else
        {
            GUI.Label(new Rect(x + 20, y, 260, 50), "En attente de l'hôte…", _row);
        }
        if (GUI.Button(new Rect(x + PanelWidth - 260, y, 220, 50), "Quitter")) CombatActions.Quit();
    }

    private void DrawScoreboard(ArenaWire w, float width, float y)
    {
        var ranking = ArenaHudFormatter.Ranking(w);
        var x = width / 2 - PanelWidth / 2;
        GUI.Box(new Rect(x, y, PanelWidth, 20 + ranking.Count * RowHeight), GUIContent.none);
        for (int i = 0; i < ranking.Count; i++)
            GUI.Label(new Rect(x, y + 10 + i * RowHeight, PanelWidth, RowHeight), ArenaHudFormatter.Row(i + 1, ranking[i], w.Mode, CombatUiState.NameOf), _row);
    }

    private void DrawEnd(ArenaWire w, float width)
    {
        GUI.Label(new Rect(0, 100, width, 100), ArenaHudFormatter.EndBanner(w, CombatUiState.LocalId, CombatUiState.NameOf), _banner);
        DrawScoreboard(w, width, 220);
        var y = 260 + w.Players.Count * RowHeight;
        if (DuelSession.IsHost)
        {
            GUI.enabled = w.Players.Count >= ArenaRules.MinPlayers;
            if (GUI.Button(new Rect(width / 2 - 230, y, 210, 60), "Rejouer")) CombatActions.Replay();
            GUI.enabled = true;
        }
        if (GUI.Button(new Rect(width / 2 + 20, y, 210, 60), "Quitter")) CombatActions.Quit();
    }

    private void DrawNotice(float width)
    {
        if (Time.time > DuelSession.NoticeUntil || DuelSession.Notice.Length == 0) return;
        GUI.Label(new Rect(0, 130, width, 40), DuelSession.Notice, _top);
    }

    private void EnsureStyles()
    {
        if (_top != null) return;
        _top = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28, fontStyle = FontStyle.Bold };
        _big = new GUIStyle(_top) { fontSize = 140 };
        _banner = new GUIStyle(_top) { fontSize = 60 };
        _row = new GUIStyle(_top) { fontSize = 22, fontStyle = FontStyle.Normal };
    }
}

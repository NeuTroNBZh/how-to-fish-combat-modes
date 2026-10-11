using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Host;
using HowToFish.CombatModes.Net;
using Steamworks;
using UnityEngine;

using HowToFish.CombatModes.Ui.Native;

namespace HowToFish.CombatModes.Ui;

internal sealed class DuelHud : MonoBehaviour
{
    private const float ReferenceHeight = 1080f;
    private GUIStyle? _top, _big, _banner, _small;

    private void OnGUI()
    {
        if (GameUiKit.HudReady) return;
        if (!CombatSession.IsDuelLobby || CombatSession.Mode != GameMode.Duel) return;
        EnsureStyles();
        var scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        var width = Screen.width / scale;

        DrawNotice(width);
        var s = CombatSession.Display;
        if (s == null || s.Phase == DuelPhase.Waiting)
        {
            GUI.Label(new Rect(0, 40, width, 60), HudFormatter.WaitingText(s, CombatSession.LocalSide), _top);
            return;
        }
        DrawTopBar(s, width);
        DrawCenter(s, width);
    }

    private static double Remaining(WireState s) => s.RemainingMs / 1000.0 - (Time.time - CombatSession.DisplayReceivedAt);

    private void DrawTopBar(WireState s, float width)
    {
        var opponent = SteamFriends.GetFriendPersonaName(CombatLobby.Opponent());
        var line = HudFormatter.ScoreLine(s, CombatSession.LocalSide, SteamFriends.GetPersonaName(), opponent);
        GUI.Box(new Rect(width / 2 - 260, 20, 520, 90), GUIContent.none);
        GUI.Label(new Rect(width / 2 - 260, 25, 520, 45), line, _top);
        var info = $"Manche {s.Round} · {HudFormatter.Clock(Remaining(s))} · Kit : {s.KitName}";
        GUI.Label(new Rect(width / 2 - 260, 65, 520, 40), info, _top);
        if (CombatControllers.LoadoutText.Length > 0) GUI.Label(new Rect(0, 115, width, 40), CombatControllers.LoadoutText, _small);
    }

    private void DrawCenter(WireState s, float width)
    {
        var center = new Rect(0, 380, width, 200);
        switch (s.Phase)
        {
            case DuelPhase.Countdown:
                GUI.Label(center, HudFormatter.CountdownNumber(Remaining(s)).ToString(), _big);
                break;
            case DuelPhase.RoundEnd:
                GUI.Label(center, HudFormatter.RoundBanner(s, CombatSession.LocalSide), _banner);
                break;
            case DuelPhase.MatchEnd:
                DrawMatchEnd(s, width);
                break;
        }
    }

    private void DrawMatchEnd(WireState s, float width)
    {
        GUI.Label(new Rect(0, 360, width, 120), HudFormatter.MatchBanner(s, CombatSession.LocalSide), _banner);
        if (GUI.Button(new Rect(width / 2 - 220, 500, 200, 60), "Revanche")) CombatActions.Replay();
        if (GUI.Button(new Rect(width / 2 + 20, 500, 200, 60), "Quitter")) CombatActions.Quit();
    }

    private void DrawNotice(float width)
    {
        if (Time.time > CombatSession.NoticeUntil || CombatSession.Notice.Length == 0) return;
        GUI.Label(new Rect(0, 130, width, 40), CombatSession.Notice, _top);
    }

    private void EnsureStyles()
    {
        if (_top != null) return;
        _top = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28, fontStyle = FontStyle.Bold };
        _big = new GUIStyle(_top) { fontSize = 140 };
        _banner = new GUIStyle(_top) { fontSize = 60 };
        _small = new GUIStyle(_top) { fontSize = 20, fontStyle = FontStyle.Normal };
    }
}

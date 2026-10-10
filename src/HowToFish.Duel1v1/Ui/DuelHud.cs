using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Host;
using HowToFish.Duel1v1.Net;
using Steamworks;
using UnityEngine;

namespace HowToFish.Duel1v1.Ui;

internal sealed class DuelHud : MonoBehaviour
{
    private const float ReferenceHeight = 1080f;
    private bool _cursorFreed;
    private const float LoadoutRefreshInterval = 0.5f;
    private float _nextLoadoutRefresh;
    private string _loadout = "";
    private GUIStyle? _top, _big, _banner, _small;

    private void OnGUI()
    {
        if (!DuelSession.IsDuelLobby) return;
        EnsureStyles();
        var scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        var width = Screen.width / scale;

        DrawNotice(width);
        var s = DuelSession.Display;
        if (s == null || s.Phase == DuelPhase.Waiting)
        {
            GUI.Label(new Rect(0, 40, width, 60), HudFormatter.WaitingText(s, DuelSession.LocalSide), _top);
            return;
        }
        DrawTopBar(s, width);
        DrawCenter(s, width);
    }

    private void Update()
    {
        TrackPhase();
        bool matchEnd = DuelSession.IsDuelLobby && DuelSession.Display?.Phase == DuelPhase.MatchEnd;
        if (matchEnd == _cursorFreed) return;
        _cursorFreed = matchEnd;
        PlayerCamera.ToggleMouse(matchEnd);
    }

    // Les accessoires arrivent par SyncVar après les armes : pendant le compte à rebours on relit
    // régulièrement et on remplit le chargeur (étendu ou non) pour partir plein.
    private void TrackPhase()
    {
        var phase = DuelSession.IsDuelLobby ? DuelSession.Display?.Phase : null;
        if (phase is not (DuelPhase.Countdown or DuelPhase.Fighting) || Player.LocalPlayer == null)
        {
            _loadout = "";
            return;
        }
        if (Time.time < _nextLoadoutRefresh) return;
        _nextLoadoutRefresh = Time.time + LoadoutRefreshInterval;
        try
        {
            if (phase == DuelPhase.Countdown) WeaponUpgrades.RefillLocalAmmo(Player.LocalPlayer);
            _loadout = WeaponUpgrades.Describe(Player.LocalPlayer);
        }
        catch (System.Exception ex)
        {
            DuelPlugin.Log.LogError($"Lecture des accessoires impossible : {ex}");
        }
    }

    private static double Remaining(WireState s) => s.RemainingMs / 1000.0 - (Time.time - DuelSession.DisplayReceivedAt);

    private void DrawTopBar(WireState s, float width)
    {
        var opponent = SteamFriends.GetFriendPersonaName(DuelLobby.Opponent());
        var line = HudFormatter.ScoreLine(s, DuelSession.LocalSide, SteamFriends.GetPersonaName(), opponent);
        GUI.Box(new Rect(width / 2 - 260, 20, 520, 90), GUIContent.none);
        GUI.Label(new Rect(width / 2 - 260, 25, 520, 45), line, _top);
        var info = $"Manche {s.Round} · {HudFormatter.Clock(Remaining(s))} · Kit : {s.KitName}";
        GUI.Label(new Rect(width / 2 - 260, 65, 520, 40), info, _top);
        if (_loadout.Length > 0) GUI.Label(new Rect(0, 115, width, 40), _loadout, _small);
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
                GUI.Label(center, HudFormatter.RoundBanner(s, DuelSession.LocalSide), _banner);
                break;
            case DuelPhase.MatchEnd:
                DrawMatchEnd(s, width);
                break;
        }
    }

    private void DrawMatchEnd(WireState s, float width)
    {
        GUI.Label(new Rect(0, 360, width, 120), HudFormatter.MatchBanner(s, DuelSession.LocalSide), _banner);
        if (GUI.Button(new Rect(width / 2 - 220, 500, 200, 60), "Revanche")) RequestRematch();
        if (GUI.Button(new Rect(width / 2 + 20, 500, 200, 60), "Quitter")) Quit();
    }

    private static void RequestRematch()
    {
        if (DuelSession.IsHost) DuelHost.Instance?.RequestRematch();
        else DuelChannel.Send(DuelMessage.Rematch());
    }

    private static void Quit()
    {
        SteamManager.LeaveLobby(DisconnectReason.None);
        MainMenuManager.ToggleMenu(enabled: true);
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
        _small = new GUIStyle(_top) { fontSize = 20, fontStyle = FontStyle.Normal };
    }
}

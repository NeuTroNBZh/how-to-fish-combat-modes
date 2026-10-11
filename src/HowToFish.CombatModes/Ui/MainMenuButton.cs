using System.Collections;
using System.Linq;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Net;
using HowToFish.CombatModes.Ui.Native;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HowToFish.CombatModes.Ui;

internal sealed class MainMenuButton : MonoBehaviour
{
    private const string TemplateMethod = "QuitButton";
    private const float PanelWidth = 440f;
    private const float PanelHeight = 380f;
    private const int WindowId = 0x0D0E1;

    public static MainMenuButton? Instance { get; private set; }

    private bool _panelOpen;
    private bool _useFallbackButton;
    private GameMode? _mode;
    private GameObject? _button;

    private void Awake() => Instance = this;

    public void InjectButton() => StartCoroutine(InjectNextFrame());

    private IEnumerator InjectNextFrame()
    {
        yield return null;
        if (_button != null) yield break;
        var template = Resources.FindObjectsOfTypeAll<Button>()
            .FirstOrDefault(b => b.gameObject.scene.IsValid() && GameUiKit.CallsMethod(b, TemplateMethod));
        if (template == null)
        {
            CombatModesPlugin.Log.LogWarning("Quit button not found, using fallback button (IMGUI)");
            _useFallbackButton = true;
            yield break;
        }
        _button = CloneButton(template);
        CombatModesPlugin.Log.LogInfo($"{Lang.Text.CombatModes} added to menu ({template.transform.parent.name})");
    }

    private GameObject CloneButton(Button template)
    {
        var clone = Instantiate(template.gameObject, template.transform.parent);
        clone.name = "DuelButton";
        clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex());
        foreach (var component in clone.GetComponentsInChildren<MonoBehaviour>(true)
                     .Where(c => c.GetType().Name.Contains("Locali")))
            Destroy(component);
        var label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = Lang.Text.CombatModes;
        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(OnButton);
        button.interactable = CombatSession.IsCompatible;
        return clone;
    }

    // Native menu if the game UI is available, otherwise the IMGUI fallback panel.
    private void OnButton()
    {
        if (CombatMenuScreen.Instance != null && CombatMenuScreen.Instance.TryOpen()) return;
        Open();
    }

    private void Open()
    {
        _panelOpen = true;
        _mode = null;
    }

    private void OnGUI()
    {
        if (!MainMenuManager.IsInMenu)
        {
            _panelOpen = false;
            return;
        }
        if (_useFallbackButton && GUI.Button(new Rect(20, Screen.height - 80, 220, 50), Lang.Text.CombatModes)) Open();
        if (!_panelOpen) return;
        var rect = new Rect((Screen.width - PanelWidth) / 2, (Screen.height - PanelHeight) / 2, PanelWidth, PanelHeight);
        GUI.Window(WindowId, rect, DrawPanel, Lang.Text.CombatModes.ToUpperInvariant());
    }

    private void DrawPanel(int id)
    {
        GUILayout.Space(20);
        if (!CombatSession.IsCompatible) GUILayout.Label(Lang.Text.Incompatible);
        else if (CombatSession.IsSearching) DrawSearching();
        else if (_mode is GameMode mode) DrawLaunch(mode);
        else DrawModes();
        if (Time.time <= CombatSession.NoticeUntil && CombatSession.Notice.Length > 0) GUILayout.Label(CombatSession.Notice);
        GUILayout.FlexibleSpace();
        if (!CombatSession.IsSearching && GUILayout.Button(Lang.Text.Back, GUILayout.Height(40))) Back();
    }

    private void Back()
    {
        if (_mode != null) _mode = null;
        else _panelOpen = false;
    }

    private void DrawModes()
    {
        foreach (var mode in GameModes.All)
        {
            if (GUILayout.Button(Lang.Text.ModeLabel(mode), GUILayout.Height(55))) _mode = mode;
            GUILayout.Space(6);
        }
    }

    private void DrawLaunch(GameMode mode)
    {
        GUILayout.Label(Lang.Text.ModeLabel(mode));
        GUILayout.Space(6);
        if (GUILayout.Button(Lang.Text.InviteFriends, GUILayout.Height(55))) AfterLaunch(CombatLaunch.Launch(mode, quickMatch: false));
        GUILayout.Space(10);
        if (GUILayout.Button(Lang.Text.QuickMatch, GUILayout.Height(55))) AfterLaunch(CombatLaunch.Launch(mode, quickMatch: true));
    }

    private void AfterLaunch(bool started)
    {
        if (!started || CombatSession.IsSearching) return;
        _panelOpen = false;
        _mode = null;
    }

    private static void DrawSearching()
    {
        GUILayout.Label(Lang.Text.SearchingLobby(Lang.Text.ModeLabel(CombatSession.Mode)));
        GUILayout.Space(10);
        if (GUILayout.Button(Lang.Text.Cancel, GUILayout.Height(40))) CombatLobby.Instance?.CancelSearch();
    }

}

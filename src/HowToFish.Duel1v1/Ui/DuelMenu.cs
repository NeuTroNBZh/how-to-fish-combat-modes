using System.Collections;
using System.Linq;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Net;
using HowToFish.Duel1v1.Ui.Native;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HowToFish.Duel1v1.Ui;

internal sealed class DuelMenu : MonoBehaviour
{
    private const string TemplateMethod = "QuitButton";
    private const string ButtonLabel = "Mods de combat";
    private const float PanelWidth = 440f;
    private const float PanelHeight = 380f;
    private const int WindowId = 0x0D0E1;

    public static DuelMenu? Instance { get; private set; }

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
            DuelPlugin.Log.LogWarning("Bouton Quitter introuvable, bouton de secours (IMGUI)");
            _useFallbackButton = true;
            yield break;
        }
        _button = CloneButton(template);
        DuelPlugin.Log.LogInfo($"Bouton {ButtonLabel} ajouté au menu ({template.transform.parent.name})");
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
        if (label != null) label.text = ButtonLabel;
        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(OnButton);
        button.interactable = DuelSession.IsCompatible;
        return clone;
    }

    // Menu natif si l'interface du jeu est disponible, sinon le panneau IMGUI de secours.
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
        if (_useFallbackButton && GUI.Button(new Rect(20, Screen.height - 80, 220, 50), ButtonLabel)) Open();
        if (!_panelOpen) return;
        var rect = new Rect((Screen.width - PanelWidth) / 2, (Screen.height - PanelHeight) / 2, PanelWidth, PanelHeight);
        GUI.Window(WindowId, rect, DrawPanel, ButtonLabel.ToUpperInvariant());
    }

    private void DrawPanel(int id)
    {
        GUILayout.Space(20);
        if (!DuelSession.IsCompatible) GUILayout.Label("Mod incompatible avec cette version du jeu.");
        else if (DuelSession.IsSearching) DrawSearching();
        else if (_mode is GameMode mode) DrawLaunch(mode);
        else DrawModes();
        if (Time.time <= DuelSession.NoticeUntil && DuelSession.Notice.Length > 0) GUILayout.Label(DuelSession.Notice);
        GUILayout.FlexibleSpace();
        if (!DuelSession.IsSearching && GUILayout.Button("Retour", GUILayout.Height(40))) Back();
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
            if (GUILayout.Button(GameModes.Label(mode), GUILayout.Height(55))) _mode = mode;
            GUILayout.Space(6);
        }
    }

    private void DrawLaunch(GameMode mode)
    {
        GUILayout.Label(GameModes.Label(mode));
        GUILayout.Space(6);
        if (GUILayout.Button("Inviter des amis", GUILayout.Height(55))) AfterLaunch(CombatLaunch.Launch(mode, quickMatch: false));
        GUILayout.Space(10);
        if (GUILayout.Button("Partie rapide", GUILayout.Height(55))) AfterLaunch(CombatLaunch.Launch(mode, quickMatch: true));
    }

    private void AfterLaunch(bool started)
    {
        if (!started || DuelSession.IsSearching) return;
        _panelOpen = false;
        _mode = null;
    }

    private static void DrawSearching()
    {
        GUILayout.Label($"Recherche d'un salon {GameModes.Label(DuelSession.Mode)}…");
        GUILayout.Space(10);
        if (GUILayout.Button("Annuler", GUILayout.Height(40))) DuelLobby.Instance?.CancelSearch();
    }

}

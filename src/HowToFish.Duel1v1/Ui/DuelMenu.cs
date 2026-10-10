using System;
using System.Collections;
using System.Linq;
using HowToFish.Duel1v1.Host;
using HowToFish.Duel1v1.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HowToFish.Duel1v1.Ui;

internal sealed class DuelMenu : MonoBehaviour
{
    private const string TemplateMethod = "QuitButton";
    private const float PanelWidth = 420f;
    private const float PanelHeight = 300f;
    private const int WindowId = 0x0D0E1;

    public static DuelMenu? Instance { get; private set; }

    private bool _panelOpen;
    private bool _useFallbackButton;
    private GameObject? _button;

    private void Awake() => Instance = this;

    public void InjectButton() => StartCoroutine(InjectNextFrame());

    private IEnumerator InjectNextFrame()
    {
        yield return null;
        if (_button != null) yield break;
        var template = Resources.FindObjectsOfTypeAll<Button>()
            .FirstOrDefault(b => b.gameObject.scene.IsValid() && CallsMethod(b, TemplateMethod));
        if (template == null)
        {
            DuelPlugin.Log.LogWarning("Bouton Quitter introuvable, bouton Duel de secours (IMGUI)");
            _useFallbackButton = true;
            yield break;
        }
        _button = CloneButton(template);
        DuelPlugin.Log.LogInfo($"Bouton Duel 1v1 ajouté au menu ({template.transform.parent.name})");
    }

    private static bool CallsMethod(Button button, string method)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            if (button.onClick.GetPersistentMethodName(i) == method) return true;
        return false;
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
        if (label != null) label.text = "Duel 1v1";
        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => _panelOpen = true);
        button.interactable = DuelSession.IsCompatible;
        return clone;
    }

    private void OnGUI()
    {
        if (!MainMenuManager.IsInMenu)
        {
            _panelOpen = false;
            return;
        }
        if (_useFallbackButton && GUI.Button(new Rect(20, Screen.height - 80, 200, 50), "Duel 1v1")) _panelOpen = true;
        if (!_panelOpen) return;
        var rect = new Rect((Screen.width - PanelWidth) / 2, (Screen.height - PanelHeight) / 2, PanelWidth, PanelHeight);
        GUI.Window(WindowId, rect, DrawPanel, "DUEL 1v1");
    }

    private void DrawPanel(int id)
    {
        GUILayout.Space(20);
        if (!DuelSession.IsCompatible)
            GUILayout.Label("Mod incompatible avec cette version du jeu.");
        else if (DuelSession.IsSearching)
            DrawSearching();
        else
            DrawChoices();
        if (Time.time <= DuelSession.NoticeUntil && DuelSession.Notice.Length > 0)
            GUILayout.Label(DuelSession.Notice);
        GUILayout.FlexibleSpace();
        if (!DuelSession.IsSearching && GUILayout.Button("Retour", GUILayout.Height(40))) _panelOpen = false;
    }

    private void DrawChoices()
    {
        if (GUILayout.Button("Inviter un ami", GUILayout.Height(60))) Launch(lobby => lobby.HostDuel(isPublic: false));
        GUILayout.Space(10);
        if (GUILayout.Button("Partie rapide", GUILayout.Height(60))) Launch(lobby => lobby.StartQuickMatch());
    }

    private static void DrawSearching()
    {
        GUILayout.Label("Recherche d'un adversaire…");
        GUILayout.Space(10);
        if (GUILayout.Button("Annuler", GUILayout.Height(40))) DuelLobby.Instance?.CancelSearch();
    }

    private void Launch(Action<DuelLobby> action)
    {
        if (DuelLobby.Instance == null)
        {
            DuelPlugin.Log.LogError("DuelLobby absent");
            return;
        }
        if (!DuelPreflight.TryValidate(DuelSession.Settings, out _, out var error))
        {
            DuelSession.ShowNotice(error, Time.time);
            return;
        }
        action(DuelLobby.Instance);
        if (!DuelSession.IsSearching) _panelOpen = false;
    }
}

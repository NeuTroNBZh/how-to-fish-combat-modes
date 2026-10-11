using System.Collections.Generic;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Net;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HowToFish.Duel1v1.Ui.Native;

// Sous-menu ajouté à CanvasManager._allMainMenuButtons : le jeu le masque avec ses autres sous-menus.
internal sealed class CombatMenuScreen : MonoBehaviour
{
    private const float TitleSize = 46f;
    private const float TextSize = 26f;
    private const float ButtonHeight = 70f;
    private const float LabelWidth = 640f;

    private enum Page
    {
        Modes,
        Launch,
        Searching,
    }

    public static CombatMenuScreen? Instance { get; private set; }

    private GameObject? _root;
    private TextMeshProUGUI? _notice;
    private Page _page;
    private GameMode _mode;
    private string _built = "";

    private void Awake() => Instance = this;

    public bool TryOpen()
    {
        var panels = GameUiKit.MenuPanels;
        if (panels == null || panels.Count == 0) return false;
        if (_root == null) Build(panels);
        foreach (var panel in panels) panel.SetActive(false);
        _root!.SetActive(true);
        _page = Page.Modes;
        _built = "";
        return true;
    }

    private void Build(List<GameObject> panels)
    {
        var template = (RectTransform)panels[panels.Count > 1 ? 1 : 0].transform;
        _root = new GameObject("HTF_CombatMenu", typeof(RectTransform));
        var rect = (RectTransform)_root.transform;
        rect.SetParent(template.parent, false);
        rect.anchorMin = template.anchorMin;
        rect.anchorMax = template.anchorMax;
        rect.pivot = template.pivot;
        rect.anchoredPosition = template.anchoredPosition;
        rect.sizeDelta = template.sizeDelta;
        var layout = _root.AddComponent<VerticalLayoutGroup>();
        var source = template.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = source != null ? source.childAlignment : TextAnchor.MiddleCenter;
        layout.spacing = source != null ? source.spacing : 14f;
        layout.padding = source != null ? source.padding : new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        panels.Add(_root);
        _root.SetActive(false);
    }

    private void Update()
    {
        if (_root == null || !_root.activeSelf) return;
        if (!MainMenuManager.IsInMenu)
        {
            _root.SetActive(false);
            return;
        }
        var page = DuelSession.IsSearching ? Page.Searching : _page;
        var key = $"{page}|{_mode}|{DuelSession.Mode}";
        if (key != _built)
        {
            Rebuild(page);
            _built = key;
        }
        UpdateNotice();
        if (!DuelSession.IsSearching && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Back();
    }

    private void Rebuild(Page page)
    {
        foreach (Transform child in _root!.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        switch (page)
        {
            case Page.Modes:
                Title("MODS DE COMBAT");
                foreach (var mode in GameModes.All) AddButton(GameModes.Label(mode), () => Choose(mode));
                AddButton("Retour", Back);
                break;
            case Page.Launch:
                Title(GameModes.Label(_mode).ToUpperInvariant());
                AddButton("Inviter des amis", () => Launch(quickMatch: false));
                AddButton("Partie rapide", () => Launch(quickMatch: true));
                AddButton("Retour", Back);
                break;
            case Page.Searching:
                Title($"Recherche d'un salon {GameModes.Label(DuelSession.Mode)}…");
                AddButton("Annuler", () => DuelLobby.Instance?.CancelSearch());
                break;
        }
        _notice = GameUiKit.Label(_root.transform, "", TextSize, GameInfo.RedColor);
        Size(_notice, 60f);
    }

    private void Title(string text) => Size(GameUiKit.Label(_root!.transform, text, TitleSize, GameUiKit.TextColor), 80f);

    private void AddButton(string label, System.Action onClick)
    {
        var button = GameUiKit.Button(_root!.transform, label, onClick);
        GameUiKit.Fit(button, ButtonHeight);
    }

    private static void Size(TextMeshProUGUI label, float height) =>
        label.rectTransform.sizeDelta = new Vector2(LabelWidth, height);

    private void UpdateNotice()
    {
        if (_notice == null) return;
        _notice.text = Time.time <= DuelSession.NoticeUntil ? DuelSession.Notice : "";
    }

    private void Choose(GameMode mode)
    {
        _mode = mode;
        _page = Page.Launch;
    }

    private void Launch(bool quickMatch)
    {
        if (!CombatLaunch.Launch(_mode, quickMatch) || DuelSession.IsSearching) return;
        _root!.SetActive(false);
    }

    private void Back()
    {
        if (_page == Page.Launch)
        {
            _page = Page.Modes;
            return;
        }
        _root!.SetActive(false);
        var panels = GameUiKit.MenuPanels;
        if (panels != null && panels.Count > 0) panels[0].SetActive(true);
    }
}

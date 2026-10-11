using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HowToFish.Duel1v1.Ui.Native;

// Modèles pris dans l'interface du jeu : nos écrans en sont des clones, ils héritent police, sons et animations.
internal static class GameUiKit
{
    private const string TemplateMethod = "QuitButton";
    private const float RetrySeconds = 5f;

    private static readonly AccessTools.FieldRef<CanvasManager, List<GameObject>> MenuPanelsOf =
        AccessTools.FieldRefAccess<CanvasManager, List<GameObject>>("_allMainMenuButtons");
    private static readonly AccessTools.FieldRef<PauseManager, GameObject> PauseHolderOf =
        AccessTools.FieldRefAccess<PauseManager, GameObject>("_pauseHolder");

    private static Button? _button;
    private static TMP_Text? _text;
    private static Image? _panel;
    private static List<GameObject>? _menuPanels;
    private static float _nextTry;
    private static bool _warned;
    private static string _lastSummary = "";

    public static bool ForceFallback { get; set; }

    public static bool MenuReady => !ForceFallback && Locate() && _menuPanels != null;

    public static bool HudReady => !ForceFallback && Locate() && _panel != null;

    public static List<GameObject>? MenuPanels => MenuReady ? _menuPanels : null;

    public static Color TextColor => _text != null ? _text.color : Color.white;

    private static bool Locate()
    {
        if (_button != null && _text != null && _panel != null && _menuPanels != null) return true;
        if (Time.unscaledTime < _nextTry) return _button != null && _text != null;
        _nextTry = Time.unscaledTime + RetrySeconds;
        try
        {
            Find();
        }
        catch (Exception ex)
        {
            DuelPlugin.Log.LogError($"Lecture de l'interface du jeu impossible : {ex}");
        }
        bool ok = _button != null && _text != null;
        if (!ok && !_warned)
        {
            _warned = true;
            DuelPlugin.Log.LogWarning("Modèles d'interface du jeu introuvables, affichage de secours (IMGUI)");
        }
        return ok;
    }

    private static void Find()
    {
        if (_button == null)
        {
            _button = Resources.FindObjectsOfTypeAll<Button>()
                .FirstOrDefault(b => b.gameObject.scene.IsValid() && CallsMethod(b, TemplateMethod));
            _text = _button != null ? _button.GetComponentInChildren<TMP_Text>(true) : null;
        }
        if (_menuPanels == null)
        {
            var canvas = Resources.FindObjectsOfTypeAll<CanvasManager>().FirstOrDefault(c => c.gameObject.scene.IsValid());
            if (canvas != null) _menuPanels = MenuPanelsOf(canvas);
        }
        if (_panel == null)
        {
            var pause = Resources.FindObjectsOfTypeAll<PauseManager>().FirstOrDefault(p => p.gameObject.scene.IsValid());
            var holder = pause != null ? PauseHolderOf(pause) : null;
            if (holder != null) _panel = LargestSpriteImage(holder);
        }
        var summary = $"Interface du jeu : bouton={(_button != null)}, texte={(_text != null)}, panneau={(_panel != null)}, menu={(_menuPanels != null)}";
        if (summary == _lastSummary) return;
        _lastSummary = summary;
        DuelPlugin.Log.LogInfo(summary);
    }

    private static Image? LargestSpriteImage(GameObject root) =>
        root.GetComponentsInChildren<Image>(true)
            .Where(i => i.sprite != null)
            .OrderByDescending(i => i.rectTransform.rect.width * i.rectTransform.rect.height)
            .FirstOrDefault();

    public static bool CallsMethod(Button button, string method)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            if (button.onClick.GetPersistentMethodName(i) == method) return true;
        return false;
    }

    public static Button Button(Transform parent, string label, Action onClick)
    {
        var clone = UnityEngine.Object.Instantiate(_button!.gameObject, parent, false);
        clone.name = "HTF_Button";
        clone.SetActive(true);
        foreach (var component in clone.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c.GetType().Name.Contains("Locali")))
            UnityEngine.Object.Destroy(component);
        var text = clone.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => onClick());
        return button;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var go = new GameObject("HTF_Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = _text!.font;
        label.fontSharedMaterial = _text.fontSharedMaterial;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    public static Image Panel(Transform parent, Vector2 size)
    {
        var go = new GameObject("HTF_Panel", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = _panel!.sprite;
        image.type = _panel.type;
        image.color = _panel.color;
        image.material = _panel.material;
        image.pixelsPerUnitMultiplier = _panel.pixelsPerUnitMultiplier;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = size;
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 28, 28);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        // La hauteur demandée n'est qu'un point de départ : le panneau grandit avec son contenu.
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return image;
    }

    public static void Fit(Component element, float height)
    {
        var layout = element.GetComponent<LayoutElement>();
        if (layout == null) layout = element.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
    }
}

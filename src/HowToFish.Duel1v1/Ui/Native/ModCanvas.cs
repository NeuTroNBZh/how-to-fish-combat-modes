using UnityEngine;
using UnityEngine.UI;

namespace HowToFish.Duel1v1.Ui.Native;

internal static class ModCanvas
{
    private const int SortingOrder = 500;
    private static Canvas? _canvas;

    public static RectTransform Layer(string name)
    {
        if (_canvas == null) Create();
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_canvas!.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void Create()
    {
        var go = new GameObject("HTF_CombatCanvas", typeof(RectTransform));
        Object.DontDestroyOnLoad(go);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = SortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
    }
}

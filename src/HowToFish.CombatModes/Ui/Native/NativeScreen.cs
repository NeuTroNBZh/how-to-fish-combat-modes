using System;
using HowToFish.CombatModes.Core;
using UnityEngine;

namespace HowToFish.CombatModes.Ui.Native;

// Mod screen on the canvas: rebuilt when its key changes (structure), updated every frame by Tick (texts).
internal abstract class NativeScreen : MonoBehaviour
{
    private RectTransform? _layer;
    private string _key = "";

    protected abstract bool ShouldShow(CombatScreen screen);

    protected abstract string Key();

    protected abstract void Build(RectTransform layer);

    protected virtual void Tick()
    {
    }

    // The game hides its own canvases during pause: do the same to keep the pause menu clickable.
    protected virtual bool ShowsDuringPause => false;

    private void Update()
    {
        bool show = GameUiKit.HudReady && CombatUiState.InCombat && ShouldShow(CombatUiState.Screen)
            && (ShowsDuringPause || !PauseManager.IsPaused);
        if (!show)
        {
            if (_layer != null && _layer.gameObject.activeSelf) _layer.gameObject.SetActive(false);
            _key = "";
            return;
        }
        if (_layer == null) _layer = ModCanvas.Layer(GetType().Name);
        if (!_layer.gameObject.activeSelf) _layer.gameObject.SetActive(true);
        try
        {
            var key = Key();
            if (key != _key)
            {
                Clear(_layer);
                Build(_layer);
                _key = key;
            }
            Tick();
        }
        catch (Exception ex)
        {
            CombatModesPlugin.Log.LogError($"Screen {GetType().Name} : {ex}");
            _key = "";
        }
    }

    private static void Clear(RectTransform layer)
    {
        foreach (Transform child in layer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}

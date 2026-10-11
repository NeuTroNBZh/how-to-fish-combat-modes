using System;
using HowToFish.Duel1v1.Core;
using UnityEngine;

namespace HowToFish.Duel1v1.Ui.Native;

// Écran du mod sur le canvas : reconstruit quand sa clé change (structure), mis à jour à chaque frame par Tick (textes).
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

    private void Update()
    {
        bool show = GameUiKit.HudReady && CombatUiState.InCombat && ShouldShow(CombatUiState.Screen);
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
            DuelPlugin.Log.LogError($"Écran {GetType().Name} : {ex}");
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

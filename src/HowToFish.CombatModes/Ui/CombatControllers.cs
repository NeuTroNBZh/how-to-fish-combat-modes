using System;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Host;
using UnityEngine;

namespace HowToFish.CombatModes.Ui;

internal sealed class CombatControllers : MonoBehaviour
{
    private const float RefreshInterval = 0.5f;
    private const float RefillWindow = 2f;

    private bool _cursorFreed;
    private bool _wasAlive;
    private float _refillUntil;
    private float _nextRefresh;

    public static string LoadoutText { get; private set; } = "";

    private void Update()
    {
        var screen = CombatUiState.Screen;
        KeepCursor(screen);
        TrackLoadout(screen);
    }

    // Le jeu reverrouille le curseur (retour de focus, fin de pause) : on le relibère tant qu'il doit l'être.
    private void KeepCursor(CombatScreen screen)
    {
        bool free = CombatUiRules.FreesCursor(screen);
        if (free != _cursorFreed)
        {
            _cursorFreed = free;
            PlayerCamera.ToggleMouse(free);
            return;
        }
        if (free && Cursor.lockState != CursorLockMode.None) PlayerCamera.ToggleMouse(true);
    }

    // Les accessoires arrivent par SyncVar après l'arme : on remplit le chargeur pendant le compte à rebours
    // et juste après chaque apparition en arène.
    private void TrackLoadout(CombatScreen screen)
    {
        bool alive = CombatUiState.LocalAlive;
        if (alive && !_wasAlive) _refillUntil = Time.time + RefillWindow;
        _wasAlive = alive;
        if (screen is not (CombatScreen.Countdown or CombatScreen.Fight) || Player.LocalPlayer == null)
        {
            LoadoutText = "";
            return;
        }
        if (Time.time < _nextRefresh) return;
        _nextRefresh = Time.time + RefreshInterval;
        try
        {
            if (screen == CombatScreen.Countdown || Time.time < _refillUntil) WeaponUpgrades.RefillLocalAmmo(Player.LocalPlayer);
            LoadoutText = WeaponUpgrades.Describe(Player.LocalPlayer);
        }
        catch (Exception ex)
        {
            CombatModesPlugin.Log.LogError($"Lecture des accessoires impossible : {ex}");
        }
    }
}

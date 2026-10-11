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

    // The game relocks the cursor (focus return, end of pause): unlock it again while it must stay free.
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

    // Attachments arrive via SyncVar after the weapon: refill the magazine during the countdown
    // and right after each arena spawn.
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
            CombatModesPlugin.Log.LogError($"Could not read attachments : {ex}");
        }
    }
}

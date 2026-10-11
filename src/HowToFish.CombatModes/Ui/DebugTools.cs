using System;
using HowToFish.CombatModes.Arena;
using HowToFish.CombatModes.Host;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HowToFish.CombatModes.Ui;

internal sealed class DebugTools : MonoBehaviour
{
    private Key _positionKey = Key.F9;

    private void Awake()
    {
        if (Enum.TryParse(CombatSession.DebugPositionKey, true, out Key key)) _positionKey = key;
        else CombatModesPlugin.Log.LogWarning($"DebugPositionKey '{CombatSession.DebugPositionKey}' inconnue, F9 utilisée");
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard[_positionKey].wasPressedThisFrame && Player.LocalPlayer != null)
        {
            var p = Player.LocalPlayer.Transform.position;
            var text = FormattableString.Invariant($"{p.x:F1},{p.y:F1},{p.z:F1}");
            CombatModesPlugin.Log.LogInfo($"Position : {text}");
            CombatSession.ShowNotice($"Position : {text}", Time.time);
        }
        if (!CombatSession.Settings.DebugSolo) return;
        if (keyboard.f10Key.wasPressedThisFrame)
        {
            if (CombatSession.IsHostingArena) ArenaHost.Instance?.KillBot();
            else DuelHost.Instance?.SimulateGuestDeath();
        }
        if (keyboard.f11Key.wasPressedThisFrame && CombatSession.IsHostingArena) ArenaHost.Instance?.AddBot();
    }
}

using System;
using HowToFish.Duel1v1.Arena;
using HowToFish.Duel1v1.Host;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HowToFish.Duel1v1.Ui;

internal sealed class DebugTools : MonoBehaviour
{
    private Key _positionKey = Key.F9;

    private void Awake()
    {
        if (Enum.TryParse(DuelSession.DebugPositionKey, true, out Key key)) _positionKey = key;
        else DuelPlugin.Log.LogWarning($"DebugPositionKey '{DuelSession.DebugPositionKey}' inconnue, F9 utilisée");
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard[_positionKey].wasPressedThisFrame && Player.LocalPlayer != null)
        {
            var p = Player.LocalPlayer.Transform.position;
            var text = FormattableString.Invariant($"{p.x:F1},{p.y:F1},{p.z:F1}");
            DuelPlugin.Log.LogInfo($"Position : {text}");
            DuelSession.ShowNotice($"Position : {text}", Time.time);
        }
        if (!DuelSession.Settings.DebugSolo) return;
        if (keyboard.f10Key.wasPressedThisFrame)
        {
            if (DuelSession.IsHostingArena) ArenaHost.Instance?.KillBot();
            else DuelHost.Instance?.SimulateGuestDeath();
        }
        if (keyboard.f11Key.wasPressedThisFrame && DuelSession.IsHostingArena) ArenaHost.Instance?.AddBot();
    }
}

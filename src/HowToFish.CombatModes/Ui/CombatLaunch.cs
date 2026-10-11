using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Host;
using HowToFish.CombatModes.Net;
using UnityEngine;

namespace HowToFish.CombatModes.Ui;

internal static class CombatLaunch
{
    public static bool Launch(GameMode mode, bool quickMatch)
    {
        if (CombatLobby.Instance == null)
        {
            CombatModesPlugin.Log.LogError("CombatLobby missing");
            return false;
        }
        if (!Validate(mode, out var error))
        {
            CombatSession.ShowNotice(error, Time.time);
            return false;
        }
        if (quickMatch) CombatLobby.Instance.StartQuickMatch(mode);
        else CombatLobby.Instance.Host(mode, isPublic: false);
        return true;
    }

    private static bool Validate(GameMode mode, out string error) => mode == GameMode.Duel
        ? Preflight.TryValidate(CombatSession.Settings, out _, out error)
        : Preflight.TryValidateArena(CombatSession.ArenaSettings, CombatSession.Settings, mode, out _, out error);
}

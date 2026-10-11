using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Host;
using HowToFish.Duel1v1.Net;
using UnityEngine;

namespace HowToFish.Duel1v1.Ui;

internal static class CombatLaunch
{
    public static bool Launch(GameMode mode, bool quickMatch)
    {
        if (DuelLobby.Instance == null)
        {
            DuelPlugin.Log.LogError("DuelLobby absent");
            return false;
        }
        if (!Validate(mode, out var error))
        {
            DuelSession.ShowNotice(error, Time.time);
            return false;
        }
        if (quickMatch) DuelLobby.Instance.StartQuickMatch(mode);
        else DuelLobby.Instance.Host(mode, isPublic: false);
        return true;
    }

    private static bool Validate(GameMode mode, out string error) => mode == GameMode.Duel
        ? DuelPreflight.TryValidate(DuelSession.Settings, out _, out error)
        : DuelPreflight.TryValidateArena(DuelSession.ArenaSettings, DuelSession.Settings, mode, out _, out error);
}

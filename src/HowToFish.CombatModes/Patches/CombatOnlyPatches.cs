using System.Collections.Generic;
using HarmonyLib;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Ui;
using UnityEngine;

namespace HowToFish.CombatModes.Patches;

// Game respawn (hold click on death): it drops the inventory and bypasses our respawns.
[HarmonyPatch(typeof(Server), "RpcLogic___RespawnPlayer___2210451296")]
internal static class BlockGameRespawnPatch
{
    private static bool Prefix() => !CombatUiRules.BlocksGameRespawn(CombatSession.IsDuelLobby);
}

// Reviving a body by another player: forbidden in combat.
[HarmonyPatch(typeof(Server), "RpcLogic___ResurrectPlayer___2247010027")]
internal static class BlockCorpseResurrectionPatch
{
    private static bool Prefix() => !CombatUiRules.BlocksGameRespawn(CombatSession.IsDuelLobby);
}

// Player side: holding click no longer fills the respawn gauge.
[HarmonyPatch(typeof(PlayerDying), "MouseClick")]
internal static class BlockGiveUpClickPatch
{
    private static bool Prefix() => !CombatUiRules.BlocksGameRespawn(CombatUiState.InCombat);
}

[HarmonyPatch(typeof(DeathUI), nameof(DeathUI.ToggleDeathUI))]
internal static class HideRespawnHintsPatch
{
    private static readonly string[] HintFields = { "_friendsHelpText", "_giveUpMask", "_giveUpEvent", "_giveUpHolderEvent" };
    private static readonly HashSet<GameObject> Hidden = new();

    // Outside combat, only re-show what we hid, without touching the game's own choices.
    private static void Postfix(DeathUI __instance, bool to)
    {
        if (!to) return;
        bool hide = CombatUiRules.BlocksGameRespawn(CombatUiState.InCombat);
        foreach (var field in HintFields)
        {
            var value = Traverse.Create(__instance).Field(field).GetValue();
            var go = value is Component component ? component.gameObject : value as GameObject;
            if (go == null) continue;
            if (hide && go.activeSelf)
            {
                go.SetActive(false);
                Hidden.Add(go);
            }
            else if (!hide && Hidden.Remove(go))
            {
                go.SetActive(true);
            }
        }
    }
}

[HarmonyPatch(typeof(PauseManager), "OnPause")]
internal static class HideServerButtonsInCombatPatch
{
    private static void Postfix()
    {
        if (!CombatUiRules.HidesServerButtons(CombatUiState.InCombat)) return;
        var instance = Traverse.Create<PauseManager>().Field("_instance").GetValue<PauseManager>();
        if (instance == null) return;
        var buttons = Traverse.Create(instance).Field("_serverButtons").GetValue<List<GameObject>>();
        if (buttons == null) return;
        foreach (var button in buttons)
            if (button != null) button.SetActive(false);
    }
}

using System.Collections.Generic;
using HarmonyLib;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Ui;
using UnityEngine;

namespace HowToFish.Duel1v1.Patches;

// Respawn du jeu (maintenir le clic à la mort) : il vide l'inventaire au sol et contourne nos réapparitions.
[HarmonyPatch(typeof(Server), "RpcLogic___RespawnPlayer___2210451296")]
internal static class BlockGameRespawnPatch
{
    private static bool Prefix() => !CombatUiRules.BlocksGameRespawn(DuelSession.IsDuelLobby);
}

// Réanimation d'un corps par un autre joueur : interdite en combat.
[HarmonyPatch(typeof(Server), "RpcLogic___ResurrectPlayer___2247010027")]
internal static class BlockCorpseResurrectionPatch
{
    private static bool Prefix() => !CombatUiRules.BlocksGameRespawn(DuelSession.IsDuelLobby);
}

// Côté joueur : le clic maintenu ne remplit plus la jauge de réapparition.
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

    // On ne réaffiche hors combat que ce que l'on a masqué, sans toucher aux choix du jeu.
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

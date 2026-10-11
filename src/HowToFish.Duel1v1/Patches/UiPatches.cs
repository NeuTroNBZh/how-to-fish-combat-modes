using HarmonyLib;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Ui;
using HowToFish.Duel1v1.Ui.Native;

namespace HowToFish.Duel1v1.Patches;

// Tab ouvre le journal du pêcheur : inutile en combat, notre tableau des scores prend la touche.
[HarmonyPatch(typeof(PlayerThinking), "ThinkingInput")]
internal static class BlockJournalInCombatPatch
{
    private static bool Prefix() => !CombatUiRules.BlocksJournal(CombatUiState.InCombat);
}

// Salon et écran de fin se comportent comme un menu du jeu : pas de tir, pas de visée, pas de relock du curseur.
[HarmonyPatch(typeof(Player), nameof(Player.BlockInputs), MethodType.Getter)]
internal static class BlockInputsOnCombatScreensPatch
{
    private static void Postfix(Player __instance, ref bool __result)
    {
        if (__result || __instance != Player.LocalPlayer) return;
        __result = CombatUiRules.BlocksInputs(CombatUiState.Screen);
    }
}

// Échap au menu principal ouvre la pause du jeu : dans notre sous-menu, il doit d'abord revenir en arrière.
[HarmonyPatch(typeof(PauseManager), "PauseInput")]
internal static class EscapeBacksOutOfCombatMenuPatch
{
    private static bool Prefix() => CombatMenuScreen.Instance == null || !CombatMenuScreen.Instance.HandleEscape();
}

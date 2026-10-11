using HarmonyLib;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Ui;
using HowToFish.CombatModes.Ui.Native;

namespace HowToFish.CombatModes.Patches;

// Tab opens the fisher's journal: useless in combat, our scoreboard takes the key.
[HarmonyPatch(typeof(PlayerThinking), "ThinkingInput")]
internal static class BlockJournalInCombatPatch
{
    private static bool Prefix() => !CombatUiRules.BlocksJournal(CombatUiState.InCombat);
}

// Lobby and end screen behave like a game menu: no shooting, no aiming, no cursor relock.
[HarmonyPatch(typeof(Player), nameof(Player.BlockInputs), MethodType.Getter)]
internal static class BlockInputsOnCombatScreensPatch
{
    private static void Postfix(Player __instance, ref bool __result)
    {
        if (__result || __instance != Player.LocalPlayer) return;
        __result = CombatUiRules.BlocksInputs(CombatUiState.Screen);
    }
}

// Escape in the main menu opens the game's pause: in our submenu it must go back first.
[HarmonyPatch(typeof(PauseManager), "PauseInput")]
internal static class EscapeBacksOutOfCombatMenuPatch
{
    private static bool Prefix() => CombatMenuScreen.Instance == null || !CombatMenuScreen.Instance.HandleEscape();
}

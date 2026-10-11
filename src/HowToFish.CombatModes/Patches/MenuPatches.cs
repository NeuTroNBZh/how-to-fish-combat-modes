using HarmonyLib;
using HowToFish.CombatModes.Ui;

namespace HowToFish.CombatModes.Patches;

[HarmonyPatch(typeof(MainMenuManager), "Start")]
internal static class InjectDuelButtonPatch
{
    private static void Postfix() => MainMenuButton.Instance?.InjectButton();
}

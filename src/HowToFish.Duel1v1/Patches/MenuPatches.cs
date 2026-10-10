using HarmonyLib;
using HowToFish.Duel1v1.Ui;

namespace HowToFish.Duel1v1.Patches;

[HarmonyPatch(typeof(MainMenuManager), "Start")]
internal static class InjectDuelButtonPatch
{
    private static void Postfix() => DuelMenu.Instance?.InjectButton();
}

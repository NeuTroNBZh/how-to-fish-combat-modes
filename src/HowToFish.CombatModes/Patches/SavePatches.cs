using HarmonyLib;

namespace HowToFish.CombatModes.Patches;

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadAllServers))]
internal static class HideDuelSavePatch
{
    private static void Postfix() => SaveManager.ServerSaves.RemoveAll(s => s.Name == CombatSession.SaveName);
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveServer))]
internal static class SkipDuelSavePatch
{
    private static bool Prefix() => SaveManager.CurServerSave?.Name != CombatSession.SaveName;
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.PlayerHasFinishedTutorial))]
internal static class SkipTutorialInDuelPatch
{
    private static bool Prefix(ref bool __result)
    {
        if (!CombatSession.IsDuelLobby) return true;
        __result = true;
        return false;
    }
}

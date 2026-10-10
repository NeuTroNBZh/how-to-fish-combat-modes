using HarmonyLib;
using HowToFish.Duel1v1.Host;

namespace HowToFish.Duel1v1.Patches;

[HarmonyPatch(typeof(PlayerVitals), nameof(PlayerVitals.TakeDamage))]
internal static class BlockDamageOutsideFightPatch
{
    private static bool Prefix() => !DuelSession.BlocksDamage;
}

[HarmonyPatch(typeof(PlayerDying), nameof(PlayerDying.ServerDie))]
internal static class DetectDuelDeathPatch
{
    private static readonly AccessTools.FieldRef<PlayerDying, Player> PlayerOf =
        AccessTools.FieldRefAccess<PlayerDying, Player>("_player");

    private static void Postfix(PlayerDying __instance)
    {
        if (!DuelSession.IsHostingDuel) return;
        DuelHost.Instance?.OnPlayerDied(PlayerOf(__instance));
    }
}

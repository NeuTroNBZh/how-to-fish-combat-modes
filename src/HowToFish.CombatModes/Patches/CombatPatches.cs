using HarmonyLib;
using HowToFish.CombatModes.Arena;
using HowToFish.CombatModes.Host;
using UnityEngine;

namespace HowToFish.CombatModes.Patches;

[HarmonyPatch(typeof(PlayerVitals), nameof(PlayerVitals.TakeDamage))]
internal static class BlockDamageOutsideFightPatch
{
    private static readonly AccessTools.FieldRef<PlayerVitals, Player> PlayerOf =
        AccessTools.FieldRefAccess<PlayerVitals, Player>("_player");

    private static bool Prefix(PlayerVitals __instance)
    {
        if (CombatSession.IsHostingArena) return ArenaHost.Instance == null || ArenaHost.Instance.AllowsDamage(PlayerOf(__instance));
        return !CombatSession.BlocksDamage;
    }
}

// Les accessoires tirés au hasard ne doivent pas débloquer le succès « tous les accessoires ».
[HarmonyPatch(typeof(AchievementManager), nameof(AchievementManager.CheckAttachmentsAchievement))]
internal static class NoAttachmentAchievementInDuelPatch
{
    private static bool Prefix() => !CombatSession.IsDuelLobby;
}

// Logique serveur du ServerRpc HitPlayer : seul endroit où le jeu connaît l'attaquant (__5).
[HarmonyPatch(typeof(Server), "RpcLogic___HitPlayer___2449261505")]
internal static class TrackHitsPatch
{
    private static void Prefix(Player __0, Player __5)
    {
        if (CombatSession.IsHostingArena) HitTracker.Record(__0, __5, Time.time);
    }
}

[HarmonyPatch(typeof(PlayerDying), nameof(PlayerDying.ServerDie))]
internal static class DetectDuelDeathPatch
{
    private static readonly AccessTools.FieldRef<PlayerDying, Player> PlayerOf =
        AccessTools.FieldRefAccess<PlayerDying, Player>("_player");

    private static void Postfix(PlayerDying __instance)
    {
        var player = PlayerOf(__instance);
        if (CombatSession.IsHostingDuel) DuelHost.Instance?.OnPlayerDied(player);
        else if (CombatSession.IsHostingArena) ArenaHost.Instance?.OnPlayerDied(player);
    }
}

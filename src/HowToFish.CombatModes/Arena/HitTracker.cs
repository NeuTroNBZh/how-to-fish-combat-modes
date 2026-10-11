using System.Collections.Generic;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Core.Arena;

namespace HowToFish.CombatModes.Arena;

// The game does not report the killer on death, so remember the last player who hit each victim.
internal static class HitTracker
{
    public sealed class Hit
    {
        public Hit(ulong attacker, string weapon, float at)
        {
            Attacker = attacker;
            Weapon = weapon;
            At = at;
        }

        public ulong Attacker { get; }
        public string Weapon { get; }
        public float At { get; }
    }

    private static readonly Dictionary<ulong, Hit> LastHits = new();

    public static void Record(Player victim, Player attacker, float now)
    {
        if (victim == null || attacker == null || victim == attacker) return;
        ArenaHost.Instance?.OnHitDealt(attacker.SteamID);
        // Pellets landing after death must not re-arm a kill credit on the victim.
        if (ArenaHost.Instance == null || !ArenaHost.Instance.AllowsDamage(victim)) return;
        var held = attacker.Holding.HeldItem;
        var weapon = held != null ? ItemNames.KeyOf(held.name) : "";
        LastHits[victim.SteamID] = new Hit(attacker.SteamID, weapon, now);
    }

    public static Hit? TakeCredit(ulong victim, float now)
    {
        if (!LastHits.TryGetValue(victim, out var hit)) return null;
        LastHits.Remove(victim);
        return now - hit.At <= ArenaSettings.KillCreditSeconds ? hit : null;
    }

    public static void Clear() => LastHits.Clear();
}

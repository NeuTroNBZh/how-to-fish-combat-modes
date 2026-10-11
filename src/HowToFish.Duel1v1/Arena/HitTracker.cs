using System.Collections.Generic;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;

namespace HowToFish.Duel1v1.Arena;

// Le jeu ne transmet pas le tueur à la mort : on retient le dernier joueur qui a touché chaque victime.
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
        var held = attacker.Holding.HeldItem;
        var weapon = held != null ? ItemNames.KeyOf(held.name) : "";
        LastHits[victim.SteamID] = new Hit(attacker.SteamID, weapon, now);
        ArenaHost.Instance?.OnHitDealt(attacker.SteamID);
    }

    public static Hit? TakeCredit(ulong victim, float now)
    {
        if (!LastHits.TryGetValue(victim, out var hit)) return null;
        LastHits.Remove(victim);
        return now - hit.At <= ArenaSettings.KillCreditSeconds ? hit : null;
    }

    public static void Clear() => LastHits.Clear();
}

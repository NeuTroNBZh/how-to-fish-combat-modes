using System.Collections.Generic;
using System.Linq;
using FishNet;
using HowToFish.Duel1v1.Core;
using UnityEngine;
using NVector3 = System.Numerics.Vector3;

namespace HowToFish.Duel1v1.Host;

internal static class GameActions
{
    private const float GroundProbeHeight = 100f;
    private const float GroundProbeDistance = 300f;
    private const int MaxHealth = 100;
    private const byte CorpseDestroyReason = 7;

    public static bool ItemExists(string name) => GameInfo.GetSpawnable(name) != null;

    public static bool IsMainIsland(int index) => index >= 0 && index <= byte.MaxValue && IslandManager.IsMainIsland((byte)index);

    // Équivalent serveur de Server.RespawnPlayer sans son ServerDropAll : le RPC s'exécute au tick
    // suivant et jetterait au sol le kit donné entre-temps.
    public static void Revive(Player p)
    {
        var corpse = p.Dying.DeadPlayer;
        if (corpse != null) corpse.DestroyItem(CorpseDestroyReason);
        p.Vitals.ServerResetVitals();
    }

    // Pas pendant la préparation : le client détecte une réapparition quand la vie revient à la vie de départ (50).
    public static void HealToFull(Player p) => p.Vitals.Heal(MaxHealth);

    public static void ClearInventory(Player p)
    {
        var items = p.Inventory._items;
        foreach (var key in items.Keys.ToList())
        {
            var item = items[key];
            if (item != null) item.DestroyItem();
            items[key] = null;
        }
        var held = p.Holding.HeldItem;
        if (held != null) held.DestroyItem();
    }

    public static void Destroy(IEnumerable<Item> items)
    {
        foreach (var item in items.Where(i => i != null && !i.IsDestroying)) item.DestroyItem();
    }

    public static (IReadOnlyList<Item> Given, IReadOnlyList<string> Missing) GiveKit(Player p, Kit kit)
    {
        var given = new List<Item>();
        var missing = new List<string>();
        byte slot = 0;
        foreach (var name in kit.Items)
        {
            var prefab = GameInfo.GetSpawnable(name);
            if (prefab == null)
            {
                missing.Add(name);
                continue;
            }
            var item = Object.Instantiate(prefab, p.CamObject.position + p.CamObject.forward, Quaternion.identity);
            item.SetSyncedHolder(p);
            InstanceFinder.ServerManager.Spawn(item.gameObject);
            p.Inventory._items[slot] = item;
            given.Add(item);
            slot++;
        }
        return (given, missing);
    }

    public static void Place(Player p, SpawnPoint spawn) =>
        Server.Instance.TeleportPlayer(p, ToUnity(spawn.Position), spawn.Yaw);

    public static bool EnsureIsland(int index)
    {
        if (IslandManager.IsLoading || OnlineIslandManager.TeleportPlayers) return false;
        var island = Island.CurIsland;
        if (OnlineIslandManager.CurIsland == index && island != null && island.gameObject.scene.buildIndex == index + 1)
            return true;
        if (OnlineIslandManager.CurIsland != index) OnlineIslandManager.TpToSpecificIsland((byte)index);
        return false;
    }

    public static Vector3 ArenaCenter() => Island.IslandPos;

    public static Vector3 Ground(Vector3 around)
    {
        var origin = around + Vector3.up * GroundProbeHeight;
        return Physics.Raycast(origin, Vector3.down, out var hit, GroundProbeDistance)
            ? hit.point + Vector3.up
            : around;
    }

    public static Vector3 ToUnity(NVector3 v) => new(v.X, v.Y, v.Z);
    public static NVector3 ToNumerics(Vector3 v) => new(v.x, v.y, v.z);
}

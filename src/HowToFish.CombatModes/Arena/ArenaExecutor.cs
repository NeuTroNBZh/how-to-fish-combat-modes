using System.Collections.Generic;
using System.Linq;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Host;

namespace HowToFish.CombatModes.Arena;

internal sealed class ArenaExecutor
{
    // The client detects a respawn when health returns to 50, so heal slightly afterwards.
    private const float HealDelay = 0.5f;

    private readonly bool _randomUpgrades;
    private readonly Dictionary<ulong, List<Item>> _items = new();
    private readonly Dictionary<ulong, float> _healAt = new();
    private readonly LoadoutCache _loadouts = new();
    private ArenaSpawns _spawns = ArenaSpawns.Empty;

    public ArenaExecutor(bool randomUpgrades) => _randomUpgrades = randomUpgrades;

    public void UseSpawns(ArenaSpawns spawns) => _spawns = spawns;

    public void ResetForGame()
    {
        foreach (var items in _items.Values) GameActions.Destroy(items);
        _items.Clear();
        _healAt.Clear();
        _loadouts.Clear();
    }

    public void ClearLoadouts() => _loadouts.Clear();

    public void Respawn(Player player, Kit kit, float now)
    {
        GameActions.Revive(player);
        GameActions.Place(player, _spawns.PickFor(player, now));
        Equip(player, kit);
        _healAt[player.SteamID] = now + HealDelay;
    }

    public void Equip(Player player, Kit kit)
    {
        Strip(player.SteamID);
        GameActions.ClearInventory(player);
        var (given, missing) = GameActions.GiveKit(player, kit, ApplyLoadout);
        _items[player.SteamID] = given.ToList();
        foreach (var item in missing) CombatModesPlugin.Log.LogWarning($"Item not found when giving : {item}");
    }

    public void Strip(ulong id)
    {
        if (!_items.TryGetValue(id, out var items)) return;
        GameActions.Destroy(items);
        _items.Remove(id);
    }

    public void Update(float now)
    {
        foreach (var id in _healAt.Where(h => now >= h.Value).Select(h => h.Key).ToList())
        {
            _healAt.Remove(id);
            var player = PlayerManager.Players.FirstOrDefault(p => p != null && p.SteamID == id);
            if (player != null) GameActions.HealToFull(player);
        }
    }

    private void ApplyLoadout(string itemName, Item item)
    {
        if (_randomUpgrades) _loadouts.Apply(itemName, item);
    }
}

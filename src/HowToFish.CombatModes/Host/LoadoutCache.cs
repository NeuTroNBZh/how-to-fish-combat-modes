using System.Collections.Generic;
using HowToFish.CombatModes.Core;

namespace HowToFish.CombatModes.Host;

// One roll per weapon name: all players get exactly the same attachments.
internal sealed class LoadoutCache
{
    private readonly Dictionary<string, Loadout> _loadouts = new();
    private readonly SystemRandomSource _random = new(new System.Random());

    public void Clear() => _loadouts.Clear();

    public void Apply(string itemName, Item item)
    {
        if (!_loadouts.TryGetValue(itemName, out var loadout))
        {
            loadout = UpgradeRoller.Roll(WeaponUpgrades.ReadOptions(item), _random);
            _loadouts[itemName] = loadout;
            CombatModesPlugin.Log.LogInfo($"Attachments {itemName} : {loadout}");
        }
        WeaponUpgrades.Apply(item, loadout);
    }
}

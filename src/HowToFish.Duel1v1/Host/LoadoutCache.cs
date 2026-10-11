using System.Collections.Generic;
using HowToFish.Duel1v1.Core;

namespace HowToFish.Duel1v1.Host;

// Un tirage par nom d'arme : tous les joueurs reçoivent exactement les mêmes accessoires.
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
            DuelPlugin.Log.LogInfo($"Accessoires {itemName} : {loadout}");
        }
        WeaponUpgrades.Apply(item, loadout);
    }
}

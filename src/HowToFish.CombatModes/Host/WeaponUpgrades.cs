using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HowToFish.CombatModes.Core;

namespace HowToFish.CombatModes.Host;

internal static class WeaponUpgrades
{
    private static readonly AccessTools.FieldRef<Attachments, List<Sight>> SightsOf =
        AccessTools.FieldRefAccess<Attachments, List<Sight>>("_sights");
    private static readonly AccessTools.FieldRef<Attachments, List<BarrelAttachment>> BarrelsOf =
        AccessTools.FieldRefAccess<Attachments, List<BarrelAttachment>>("_barrelAttachments");
    private static readonly AccessTools.FieldRef<Attachments, BulletUpgrade[]> BulletsOf =
        AccessTools.FieldRefAccess<Attachments, BulletUpgrade[]>("_bulletUpgrades");
    private static readonly AccessTools.FieldRef<Attachments, QuiverUpgrade[]> QuiversOf =
        AccessTools.FieldRefAccess<Attachments, QuiverUpgrade[]>("_quiverUpgrades");
    private static readonly AccessTools.FieldRef<Attachments, AttachmentInfo> ExtendedMagOf =
        AccessTools.FieldRefAccess<Attachments, AttachmentInfo>("_extendedMagInfo");
    private static readonly AccessTools.FieldRef<Attachments, LaserSight> LaserOf =
        AccessTools.FieldRefAccess<Attachments, LaserSight>("_laserSight");
    private static readonly AccessTools.FieldRef<Melee, SharpnessUpgrade[]> SharpnessOf =
        AccessTools.FieldRefAccess<Melee, SharpnessUpgrade[]>("_sharpnessUpgrades");

    public static WeaponOptions ReadOptions(Item item)
    {
        if (item is Melee melee) return WeaponOptions.None with { SharpnessLevels = SharpnessOf(melee)?.Length ?? 0 };
        if (item is not Weapon weapon || weapon.Attachments == null) return WeaponOptions.None;
        var a = weapon.Attachments;
        var laser = LaserOf(a);
        return new WeaponOptions(
            SightsOf(a)?.Count ?? 0,
            BarrelsOf(a)?.Count ?? 0,
            BulletsOf(a)?.Length ?? 0,
            QuiversOf(a)?.Length ?? 0,
            ExtendedMagOf(a) != null,
            laser != null && laser.Info != null,
            0);
    }

    // Appelé côté serveur après le Spawn : les SyncVar se synchronisent ensuite comme un achat d'accessoire.
    public static void Apply(Item item, Loadout loadout)
    {
        if (item is Melee melee)
        {
            melee._syncedSharpnessIndex.Value = (byte)loadout.Sharpness;
            return;
        }
        if (item is not Weapon weapon || weapon.Attachments == null) return;
        var a = weapon.Attachments;
        a._syncedSight.Value = (byte)loadout.Sight;
        a._syncedBarrelAttachment.Value = (byte)loadout.Barrel;
        a._syncedBulletIndex.Value = (byte)loadout.Bullet;
        a._syncedQuiverIndex.Value = (byte)loadout.Quiver;
        a._syncedExtendedMag.Value = loadout.ExtendedMag;
        a._syncedLaserSight.Value = loadout.Laser;
    }

    // Weapon remplit son chargeur à sa création, avant de recevoir le chargeur étendu.
    public static void RefillLocalAmmo(Player player)
    {
        foreach (var weapon in Items(player).OfType<Weapon>().Where(w => w.Attachments != null))
            Traverse.Create(weapon).Property("Ammo").SetValue(weapon.Attachments.AmmoPerMag);
    }

    public static string Describe(Player player) =>
        string.Join("  ·  ", Items(player).Select(DescribeItem));

    private static string DescribeItem(Item item)
    {
        var name = item.name.Replace("(Clone)", "").Trim();
        var parts = new List<string>();
        if (item is Melee melee && melee.SharpnessIndex > 0) parts.Add($"affûtage {melee.SharpnessIndex}");
        if (item is Weapon { Attachments: { } a }) parts.AddRange(DescribeAttachments(a));
        return parts.Count == 0 ? $"{name} (nu)" : $"{name} : {string.Join(", ", parts)}";
    }

    private static IEnumerable<string> DescribeAttachments(Attachments a)
    {
        var sight = a.Sight != 0 ? SightsOf(a)[a.Sight].Info : null;
        if (sight != null) yield return sight.NameLocalized;
        var barrel = a.BarrelAttachment != 0 ? BarrelsOf(a)[a.BarrelAttachment].Info : null;
        if (barrel != null) yield return barrel.NameLocalized;
        if (a.LaserSight) yield return "laser";
        if (a.ExtendedMag) yield return "chargeur étendu";
        if (a.AmmoType > 0) yield return $"munitions niv. {a.AmmoType}";
        if (a.QuiverIndex > 0) yield return $"carquois niv. {a.QuiverIndex}";
    }

    private static IEnumerable<Item> Items(Player player) =>
        player.Inventory._items.Values.Where(item => item != null);
}

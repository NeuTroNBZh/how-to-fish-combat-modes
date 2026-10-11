namespace HowToFish.CombatModes.Core;

public static class ItemNames
{
    // Same key as GameInfo._nameToSpawnable: prefab name without spaces, lowercase.
    public static string KeyOf(string objectName) =>
        objectName.Replace("(Clone)", "").Replace(" ", "").Trim().ToLowerInvariant();
}

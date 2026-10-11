namespace HowToFish.Duel1v1.Core;

public static class ItemNames
{
    // Même clé que GameInfo._nameToSpawnable : nom du prefab sans espaces, en minuscules.
    public static string KeyOf(string objectName) =>
        objectName.Replace("(Clone)", "").Replace(" ", "").Trim().ToLowerInvariant();
}

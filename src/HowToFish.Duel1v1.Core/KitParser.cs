using System.Collections.Generic;
using System.Linq;

namespace HowToFish.Duel1v1.Core;

public static class KitParser
{
    public static IReadOnlyList<Kit>? Parse(string text, out string error)
    {
        if (text.Contains('|'))
        {
            error = "le caractère '|' est interdit dans les kits";
            return null;
        }

        var kits = new List<Kit>();
        foreach (var rawKit in text.Split(';'))
        {
            if (string.IsNullOrWhiteSpace(rawKit)) continue;
            var items = rawKit.Split('+').Select(Normalize).ToList();
            if (items.Any(i => i.Length == 0))
            {
                error = $"objet vide dans le kit '{rawKit.Trim()}'";
                return null;
            }
            kits.Add(new Kit(string.Join("+", items), items));
        }

        if (kits.Count == 0)
        {
            error = "aucun kit défini";
            return null;
        }

        error = "";
        return kits;
    }

    private static string Normalize(string item) => item.Replace(" ", "").Trim().ToLowerInvariant();
}

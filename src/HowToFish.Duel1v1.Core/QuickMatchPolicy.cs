using System.Collections.Generic;
using System.Linq;

namespace HowToFish.Duel1v1.Core;

public static class QuickMatchPolicy
{
    // Deux joueurs qui cherchent en même temps créent chacun un lobby : seul celui qui a l'identifiant
    // le plus grand rejoint l'autre, sinon ils se croiseraient.
    public static ulong? MergeTarget(ulong ownLobby, IEnumerable<ulong> openLobbies)
    {
        var lower = openLobbies.Where(id => id < ownLobby).ToList();
        return lower.Count == 0 ? null : lower.Min();
    }
}

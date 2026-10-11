using System.Collections.Generic;
using System.Linq;

namespace HowToFish.Duel1v1.Core;

public sealed record OpenLobby(ulong Id, int Members);

public static class QuickMatchPolicy
{
    // Deux joueurs qui cherchent en même temps créent chacun un lobby : seul celui qui a l'identifiant
    // le plus grand rejoint l'autre, sinon ils se croiseraient.
    public static ulong? MergeTarget(ulong ownLobby, IEnumerable<ulong> openLobbies)
    {
        var lower = openLobbies.Where(id => id < ownLobby).ToList();
        return lower.Count == 0 ? null : lower.Min();
    }

    // En arène un salon déjà peuplé reste ouvert : un hôte seul le rejoint quel que soit son identifiant,
    // sinon la partie rapide se scinde en plusieurs salons.
    public static ulong? MergeTarget(ulong ownLobby, IEnumerable<OpenLobby> openLobbies, bool arena)
    {
        var others = openLobbies.Where(l => l.Id != ownLobby).ToList();
        if (arena)
        {
            var populated = others.Where(l => l.Members > 1)
                .OrderByDescending(l => l.Members).ThenBy(l => l.Id).FirstOrDefault();
            if (populated != null) return populated.Id;
        }
        return MergeTarget(ownLobby, others.Select(l => l.Id));
    }
}

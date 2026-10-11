using System.Collections.Generic;
using System.Linq;

namespace HowToFish.CombatModes.Core;

public sealed record OpenLobby(ulong Id, int Members);

public static class QuickMatchPolicy
{
    // Two players searching at the same time each create a lobby: only the one with the larger id
    // joins the other, otherwise they would miss each other.
    public static ulong? MergeTarget(ulong ownLobby, IEnumerable<ulong> openLobbies)
    {
        var lower = openLobbies.Where(id => id < ownLobby).ToList();
        return lower.Count == 0 ? null : lower.Min();
    }

    // In arena mode an already populated lobby stays open: a lone host joins it regardless of its id,
    // otherwise quick play splits into several lobbies.
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

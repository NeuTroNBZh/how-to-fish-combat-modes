using HowToFish.Duel1v1.Core;
using Xunit;

namespace HowToFish.Duel1v1.Tests;

public class QuickMatchPolicyTests
{
    [Fact]
    public void MergeTarget_NoOtherLobby_StaysHome()
    {
        Assert.Null(QuickMatchPolicy.MergeTarget(50, new ulong[] { 50 }));
    }

    [Fact]
    public void MergeTarget_OnlyHigherLobbies_StaysHome()
    {
        Assert.Null(QuickMatchPolicy.MergeTarget(50, new ulong[] { 70, 90 }));
    }

    [Fact]
    public void MergeTarget_LowerLobbies_JoinsLowest()
    {
        Assert.Equal(10UL, QuickMatchPolicy.MergeTarget(50, new ulong[] { 70, 30, 10, 50 }));
    }
}

public class QuickMatchArenaPolicyTests
{
    [Fact]
    public void Arena_JoinsPopulatedLobbyEvenIfHigherId()
    {
        var open = new[] { new OpenLobby(50, 1), new OpenLobby(200, 3), new OpenLobby(300, 2) };

        Assert.Equal(200UL, QuickMatchPolicy.MergeTarget(50, open, arena: true));
    }

    [Fact]
    public void Arena_NoPopulatedLobby_FallsBackToLowestId()
    {
        var open = new[] { new OpenLobby(50, 1), new OpenLobby(20, 1) };

        Assert.Equal(20UL, QuickMatchPolicy.MergeTarget(50, open, arena: true));
        Assert.Null(QuickMatchPolicy.MergeTarget(20, open, arena: true));
    }

    [Fact]
    public void Duel_IgnoresMemberCounts()
    {
        var open = new[] { new OpenLobby(50, 1), new OpenLobby(200, 3) };

        Assert.Null(QuickMatchPolicy.MergeTarget(50, open, arena: false));
    }
}

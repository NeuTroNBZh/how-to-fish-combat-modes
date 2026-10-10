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

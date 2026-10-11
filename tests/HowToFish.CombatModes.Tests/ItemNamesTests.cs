using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class ItemNamesTests
{
    [Theory]
    [InlineData("Knife(Clone)", "knife")]
    [InlineData("Assault Rifle(Clone)", "assaultrifle")]
    [InlineData("pistol", "pistol")]
    public void KeyOf_MatchesGameSpawnableKeys(string objectName, string expected)
    {
        Assert.Equal(expected, ItemNames.KeyOf(objectName));
    }
}

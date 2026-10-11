using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;
using Xunit;

namespace HowToFish.Duel1v1.Tests;

public class CombatUiRulesTests
{
    [Theory]
    [InlineData(null, CombatScreen.Lobby)]
    [InlineData(DuelPhase.Waiting, CombatScreen.Lobby)]
    [InlineData(DuelPhase.Preparing, CombatScreen.Countdown)]
    [InlineData(DuelPhase.Countdown, CombatScreen.Countdown)]
    [InlineData(DuelPhase.Fighting, CombatScreen.Fight)]
    [InlineData(DuelPhase.RoundEnd, CombatScreen.Fight)]
    [InlineData(DuelPhase.MatchEnd, CombatScreen.End)]
    public void ScreenFor_DuelPhase(DuelPhase? phase, CombatScreen expected)
    {
        Assert.Equal(expected, CombatUiRules.ScreenFor(phase));
    }

    [Theory]
    [InlineData(null, CombatScreen.Lobby)]
    [InlineData(ArenaPhase.Lobby, CombatScreen.Lobby)]
    [InlineData(ArenaPhase.Preparing, CombatScreen.Countdown)]
    [InlineData(ArenaPhase.Countdown, CombatScreen.Countdown)]
    [InlineData(ArenaPhase.Playing, CombatScreen.Fight)]
    [InlineData(ArenaPhase.Ended, CombatScreen.End)]
    public void ScreenFor_ArenaPhase(ArenaPhase? phase, CombatScreen expected)
    {
        Assert.Equal(expected, CombatUiRules.ScreenFor(phase));
    }

    [Fact]
    public void Journal_BlockedOnlyInCombatLobbies()
    {
        Assert.True(CombatUiRules.BlocksJournal(inCombatLobby: true));
        Assert.False(CombatUiRules.BlocksJournal(inCombatLobby: false));
    }

    [Theory]
    [InlineData(CombatScreen.None, false)]
    [InlineData(CombatScreen.Lobby, true)]
    [InlineData(CombatScreen.Countdown, false)]
    [InlineData(CombatScreen.Fight, false)]
    [InlineData(CombatScreen.End, true)]
    public void CursorAndInputs_FreedAndBlockedOnMenuScreens(CombatScreen screen, bool menuLike)
    {
        Assert.Equal(menuLike, CombatUiRules.FreesCursor(screen));
        Assert.Equal(menuLike, CombatUiRules.BlocksInputs(screen));
    }
}

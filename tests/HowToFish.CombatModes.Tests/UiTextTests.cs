using HowToFish.CombatModes.Core;
using Xunit;

namespace HowToFish.CombatModes.Tests;

public class UiTextTests
{
    [Theory]
    [InlineData("fr", Language.French)]
    [InlineData("fr-FR", Language.French)]
    [InlineData("FR-ca", Language.French)]
    [InlineData("en", Language.English)]
    [InlineData("de-DE", Language.English)]
    [InlineData("", Language.English)]
    [InlineData(null, Language.English)]
    public void LanguageFromLocale_FrenchOnlyForFrenchLocales(string? code, Language expected)
    {
        Assert.Equal(expected, UiText.LanguageFromLocale(code));
    }

    [Fact]
    public void For_ReturnsTheMatchingCatalog()
    {
        Assert.Equal("Combat Modes", UiText.For(Language.English).CombatModes);
        Assert.Equal("Mods de combat", UiText.For(Language.French).CombatModes);
    }

    [Fact]
    public void ModeLabels_AreTranslated()
    {
        Assert.Equal("1v1 Duel", UiText.English.ModeLabel(GameMode.Duel));
        Assert.Equal("Free-for-all", UiText.English.ModeLabel(GameMode.Deathmatch));
        Assert.Equal("Gun Game", UiText.English.ModeLabel(GameMode.GunGame));
        Assert.Equal("Course à l'armement", UiText.French.ModeLabel(GameMode.GunGame));
    }

    [Theory]
    [InlineData(0, "0 rounds", "0 manche")]
    [InlineData(1, "1 round", "1 manche")]
    [InlineData(3, "3 rounds", "3 manches")]
    public void Rounds_HandlesPlural(int count, string english, string french)
    {
        Assert.Equal(english, UiText.English.Rounds(count));
        Assert.Equal(french, UiText.French.Rounds(count));
    }

    [Fact]
    public void RoundClockKit_FormatsPerLanguage()
    {
        Assert.Equal("Round 2  ·  1:05  ·  Kit: smg+knife", UiText.English.RoundClockKit(2, "1:05", "smg+knife"));
        Assert.Equal("Manche 2  ·  1:05  ·  Kit : smg+knife", UiText.French.RoundClockKit(2, "1:05", "smg+knife"));
    }

    [Fact]
    public void WeaponUpgradeWords_AreTranslated()
    {
        Assert.Equal("extended mag", UiText.English.ExtendedMag);
        Assert.Equal("chargeur étendu", UiText.French.ExtendedMag);
        Assert.Equal("ammo lvl 2", UiText.English.AmmoLevel(2));
        Assert.Equal("munitions niv. 2", UiText.French.AmmoLevel(2));
        Assert.Equal("Steam refused to create the lobby", UiText.English.SteamRefusedLobby);
    }
}

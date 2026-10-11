using System;
using HowToFish.CombatModes.Core;
using UnityEngine.Localization.Settings;

namespace HowToFish.CombatModes.Ui;

// The mod follows the language selected in the game's settings: French for French locales, English otherwise.
internal static class Lang
{
    public static UiText Text
    {
        get
        {
            try
            {
                var locale = LocalizationSettings.SelectedLocale;
                return UiText.For(UiText.LanguageFromLocale(locale != null ? locale.Identifier.Code : null));
            }
            catch (Exception)
            {
                return UiText.English;
            }
        }
    }
}

using HowToFish.CombatModes.Arena;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Host;
using HowToFish.CombatModes.Net;

namespace HowToFish.CombatModes.Ui;

internal static class CombatActions
{
    // Start (arena lobby), Play again (arena end) and Rematch (duel end) all go through here.
    public static void Replay()
    {
        if (!CombatUiState.IsDuel)
        {
            ArenaHost.Instance?.RequestStart();
            return;
        }
        if (CombatSession.IsHost) DuelHost.Instance?.RequestRematch();
        else CombatChannel.Send(DuelMessage.Rematch());
    }

    public static void Quit()
    {
        SteamManager.LeaveLobby(DisconnectReason.None);
        MainMenuManager.ToggleMenu(enabled: true);
    }
}

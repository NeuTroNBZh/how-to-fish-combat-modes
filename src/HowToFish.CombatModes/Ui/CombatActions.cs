using HowToFish.CombatModes.Arena;
using HowToFish.CombatModes.Core;
using HowToFish.CombatModes.Host;
using HowToFish.CombatModes.Net;

namespace HowToFish.CombatModes.Ui;

internal static class CombatActions
{
    // Lancer (salon d'arène), Rejouer (fin d'arène) et Revanche (fin de duel) passent tous par ici.
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

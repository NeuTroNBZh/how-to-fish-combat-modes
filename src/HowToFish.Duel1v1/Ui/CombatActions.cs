using HowToFish.Duel1v1.Arena;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Host;
using HowToFish.Duel1v1.Net;

namespace HowToFish.Duel1v1.Ui;

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
        if (DuelSession.IsHost) DuelHost.Instance?.RequestRematch();
        else DuelChannel.Send(DuelMessage.Rematch());
    }

    public static void Quit()
    {
        SteamManager.LeaveLobby(DisconnectReason.None);
        MainMenuManager.ToggleMenu(enabled: true);
    }
}

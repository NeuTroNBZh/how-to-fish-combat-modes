namespace HowToFish.CombatModes.Core;

public static class MessagePolicy
{
    public static bool IsAccepted(MessageKind kind, bool senderIsLobbyOwner, bool localIsHost) => kind switch
    {
        MessageKind.State => senderIsLobbyOwner && !localIsHost,
        MessageKind.Hello or MessageKind.Rematch => !senderIsLobbyOwner && localIsHost,
        _ => false,
    };
}

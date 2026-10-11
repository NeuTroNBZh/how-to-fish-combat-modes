using System;
using System.Text;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;
using Steamworks;
using UnityEngine;

namespace HowToFish.Duel1v1.Net;

internal sealed class DuelChannel : MonoBehaviour
{
    public static event Action<CSteamID, DuelMessage>? MessageReceived;

    private readonly byte[] _buffer = new byte[4096];
    private Callback<LobbyChatMsg_t>? _chatMessage;

    private void Awake() => _chatMessage = Callback<LobbyChatMsg_t>.Create(OnChatMessage);

    private void OnDestroy() => _chatMessage?.Dispose();

    public static bool Send(DuelMessage message) => SendText(DuelCodec.Encode(message), message.Kind.ToString());

    public static bool SendArena(ArenaWire wire) => SendText(ArenaCodec.Encode(wire), "ARENA");

    private static bool SendText(string text, string label)
    {
        if (!DuelSession.IsDuelLobby) return false;
        var bytes = Encoding.UTF8.GetBytes(text);
        bool sent = SteamMatchmaking.SendLobbyChatMsg(DuelSession.LobbyId, bytes, bytes.Length);
        if (!sent) DuelPlugin.Log.LogWarning($"Envoi du message {label} impossible");
        return sent;
    }

    private void OnChatMessage(LobbyChatMsg_t msg)
    {
        if (!DuelSession.IsDuelLobby || msg.m_ulSteamIDLobby != DuelSession.LobbyId.m_SteamID) return;
        int length = SteamMatchmaking.GetLobbyChatEntry(
            DuelSession.LobbyId, (int)msg.m_iChatID, out var sender, _buffer, _buffer.Length, out var type);
        if (type != EChatEntryType.k_EChatEntryTypeChatMsg || length <= 0) return;

        var text = Encoding.UTF8.GetString(_buffer, 0, length).TrimEnd('\0');
        if (ArenaCodec.IsArena(text))
        {
            HandleArena(sender, text);
            return;
        }
        if (!text.StartsWith(DuelCodec.Prefix + "|", StringComparison.Ordinal)) return;
        if (!DuelCodec.TryDecode(text, out var message, out var error))
        {
            DuelPlugin.Log.LogWarning($"Message duel rejeté ({error}) : {text}");
            return;
        }
        Dispatch(sender, message!);
    }

    // Seul l'hôte (propriétaire du lobby) publie l'état de l'arène ; l'hôte n'écoute pas ses propres messages.
    private static void HandleArena(CSteamID sender, string text)
    {
        if (!ArenaCodec.TryDecode(text, out var wire, out var error))
        {
            DuelPlugin.Log.LogWarning($"Message arène rejeté ({error})");
            return;
        }
        if (DuelSession.IsHost || SteamMatchmaking.GetLobbyOwner(DuelSession.LobbyId) != sender) return;
        DuelSession.SetArenaDisplay(wire!, Time.time);
    }

    private static void Dispatch(CSteamID sender, DuelMessage message)
    {
        bool senderIsOwner = SteamMatchmaking.GetLobbyOwner(DuelSession.LobbyId) == sender;
        if (!MessagePolicy.IsAccepted(message.Kind, senderIsOwner, DuelSession.IsHost))
        {
            if (sender != SteamUser.GetSteamID())
                DuelPlugin.Log.LogWarning($"Message {message.Kind} refusé de {sender.m_SteamID}");
            return;
        }
        if (message.State is WireState state) DuelSession.SetDisplay(state, Time.time);
        MessageReceived?.Invoke(sender, message);
    }
}

using System;
using System.Globalization;

namespace HowToFish.CombatModes.Core;

public static class DuelCodec
{
    public const string Prefix = "HTFCM";
    public const int ProtocolVersion = 1;
    private const int StateFieldCount = 14;
    private static readonly int[] NumericFields = { 3, 4, 5, 6, 7, 9, 10, 11, 12, 13 };

    public static string Encode(DuelMessage m) => m.Kind switch
    {
        MessageKind.Hello => $"{Prefix}|{ProtocolVersion}|HELLO",
        MessageKind.Rematch => $"{Prefix}|{ProtocolVersion}|REQ|REMATCH",
        _ => EncodeState(m.State ?? throw new ArgumentException("STATE sans état", nameof(m))),
    };

    private static string EncodeState(WireState s) => string.Join("|",
        Prefix, ProtocolVersion, "STATE", (int)s.Phase, s.Round, s.HostScore, s.GuestScore, s.RemainingMs,
        s.KitName, SideCode(s.LastRoundWinner), Bit(s.LastRoundDraw), SideCode(s.MatchWinner), Bit(s.Forfeit),
        Bit(s.HostAtSpawnA));

    public static bool TryDecode(string text, out DuelMessage? message, out string error)
    {
        message = null;
        var p = text.Split('|');
        if (p.Length < 3 || p[0] != Prefix) return Fail("préfixe absent", out error);
        if (!int.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out var v) || v != ProtocolVersion)
            return Fail($"version inconnue '{p[1]}'", out error);

        if (p.Length == 3 && p[2] == "HELLO") message = DuelMessage.Hello();
        else if (p.Length == 4 && p[2] == "REQ" && p[3] == "REMATCH") message = DuelMessage.Rematch();
        else if (p[2] == "STATE") return TryDecodeState(p, out message, out error);
        else return Fail($"type de message inconnu '{p[2]}'", out error);

        error = "";
        return true;
    }

    private static bool TryDecodeState(string[] p, out DuelMessage? message, out string error)
    {
        message = null;
        if (p.Length != StateFieldCount) return Fail($"nombre de champs {p.Length} au lieu de {StateFieldCount}", out error);
        var n = new int[NumericFields.Length];
        for (int i = 0; i < NumericFields.Length; i++)
        {
            var field = p[NumericFields[i]];
            if (!int.TryParse(field, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out n[i]))
                return Fail($"nombre invalide '{field}'", out error);
        }
        if (!Enum.IsDefined(typeof(DuelPhase), n[0])) return Fail($"phase inconnue {n[0]}", out error);
        if (n[1] < 0 || n[2] < 0 || n[3] < 0 || n[4] < 0) return Fail("valeur négative", out error);
        if (!IsSide(n[5]) || !IsSide(n[7])) return Fail("côté invalide", out error);
        if (!IsBit(n[6]) || !IsBit(n[8]) || !IsBit(n[9])) return Fail("booléen invalide", out error);

        var state = new WireState((DuelPhase)n[0], n[1], n[2], n[3], n[4], p[8],
            ToSide(n[5]), n[6] == 1, ToSide(n[7]), n[8] == 1, n[9] == 1);
        message = DuelMessage.Of(state);
        error = "";
        return true;
    }

    private static bool Fail(string reason, out string error)
    {
        error = reason;
        return false;
    }

    private static int SideCode(Side? side) => side is null ? -1 : (int)side.Value;
    private static Side? ToSide(int code) => code < 0 ? null : (Side)code;
    private static bool IsSide(int code) => code >= -1 && code <= 1;
    private static bool IsBit(int value) => value == 0 || value == 1;
    private static int Bit(bool value) => value ? 1 : 0;
}

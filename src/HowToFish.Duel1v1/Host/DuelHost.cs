using System;
using System.Collections.Generic;
using System.Linq;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Net;
using Steamworks;
using UnityEngine;

namespace HowToFish.Duel1v1.Host;

internal sealed class DuelHost : MonoBehaviour
{
    private const int FullHealth = 100;
    private const float ZoneCheckInterval = 1f;
    private const float PermanentNoticeSeconds = 3600f;

    public static DuelHost? Instance { get; private set; }

    private readonly List<Item> _givenItems = new();
    private CSteamID _lobby = CSteamID.Nil;
    private DuelRules? _rules;
    private (SpawnPoint A, SpawnPoint B) _spawns;
    private Vector3 _center;
    private ulong _helloFrom;
    private float _guestSeenAt = -1f;
    private float _nextBroadcast;
    private float _nextZoneCheck;
    private bool _settingsChecked;

    private void Awake()
    {
        Instance = this;
        DuelChannel.MessageReceived += OnMessage;
    }

    private void OnDestroy() => DuelChannel.MessageReceived -= OnMessage;

    private void Update()
    {
        if (DuelSession.LobbyId != _lobby) ResetForLobby(DuelSession.LobbyId);
        if (!DuelSession.IsHostingDuel || Player.LocalPlayer == null || Server.Instance == null) return;
        if (!EnsureSettings()) return;
        var guest = Guest();
        CheckHandshake(guest);
        DetectReadinessAndLeaves(guest);
        Step(new Tick(Player.LocalPlayer.Vitals.Health, guest != null ? guest.Vitals.Health : FullHealth));
        KeepPlayersInZone();
        DuelSession.SetDisplay(WireState.From(DuelSession.Snapshot, Time.time), Time.time);
        if (Time.time >= _nextBroadcast) Broadcast();
    }

    public void OnPlayerDied(Player player) =>
        Step(new PlayerDied(player == Player.LocalPlayer ? Side.Host : Side.Guest));

    public void RequestRematch() => Step(new RematchRequested());

    public void SimulateGuestDeath() => Step(new PlayerDied(Side.Guest));

    private static Player? Guest() => PlayerManager.Players.FirstOrDefault(p => p != null && p != Player.LocalPlayer);

    // Le HELLO de l'invité peut arriver avant que le joueur de l'hôte existe : on ne l'efface qu'au changement de lobby.
    private void ResetForLobby(CSteamID lobby)
    {
        _lobby = lobby;
        _rules = null;
        _helloFrom = 0;
        _guestSeenAt = -1f;
        _settingsChecked = false;
        _givenItems.Clear();
    }

    private bool EnsureSettings()
    {
        if (_settingsChecked) return _rules != null;
        _settingsChecked = true;
        if (!DuelPreflight.TryValidate(DuelSession.Settings, out var settings, out var error))
        {
            SteamMatchmaking.SetLobbyJoinable(DuelSession.LobbyId, false);
            DuelSession.ShowNotice($"{error} — duel impossible, retourne au menu", Time.time, PermanentNoticeSeconds);
            return false;
        }
        DuelSession.UseSettings(settings);
        _rules = new DuelRules(settings, new SystemRandomSource(new System.Random()));
        return true;
    }

    private void CheckHandshake(Player? guest)
    {
        if (guest == null || DuelSession.Settings.DebugSolo)
        {
            _guestSeenAt = -1f;
            return;
        }
        if (_guestSeenAt < 0) _guestSeenAt = Time.time;
        bool helloMissing = _helloFrom != guest.SteamID;
        if (!helloMissing || Time.time - _guestSeenAt < DuelSettings.HelloTimeoutSeconds) return;
        DuelPlugin.Log.LogWarning($"{guest.SteamName} n'a pas le mod duel, expulsion");
        DuelSession.ShowNotice("Ton adversaire n'a pas le mod Duel 1v1", Time.time);
        ConnectionManager.Instance.KickSteamUserIfConnected(new CSteamID(guest.SteamID));
        _guestSeenAt = -1f;
    }

    private void DetectReadinessAndLeaves(Player? guest)
    {
        bool solo = DuelSession.Settings.DebugSolo;
        bool guestReady = solo || (guest != null && _helloFrom == guest.SteamID);
        var phase = DuelSession.Snapshot.Phase;
        if (phase == DuelPhase.Waiting && guestReady && PrepareArena()) Step(new PlayersReady());
        if (phase != DuelPhase.Waiting && !solo && guest == null) Step(new PlayerLeft(Side.Guest));
    }

    private bool PrepareArena()
    {
        if (!GameActions.EnsureIsland(DuelSession.Settings.IslandIndex)) return false;
        _center = GameActions.ArenaCenter();
        var s = DuelSession.Settings;
        _spawns = s.SpawnA is { } a && s.SpawnB is { } b
            ? ArenaGeometry.Facing(a, b)
            : GroundedAutoSpawns(s.ArenaRadius);
        return true;
    }

    private (SpawnPoint A, SpawnPoint B) GroundedAutoSpawns(float radius)
    {
        var auto = ArenaGeometry.AutoSpawns(GameActions.ToNumerics(_center), radius);
        var a = GameActions.ToNumerics(GameActions.Ground(GameActions.ToUnity(auto.A.Position)));
        var b = GameActions.ToNumerics(GameActions.Ground(GameActions.ToUnity(auto.B.Position)));
        return ArenaGeometry.Facing(a, b);
    }

    private void Step(DuelEvent duelEvent)
    {
        if (_rules == null) return;
        var before = DuelSession.Snapshot;
        var step = _rules.Apply(before, duelEvent, Time.time);
        DuelSession.Snapshot = step.Snapshot;
        Execute(step.Effect);
        // Re-placement au début du compte à rebours : la résurrection côté client peut ramener le joueur à sa position de mort.
        if (before.Phase == DuelPhase.Preparing && step.Snapshot.Phase == DuelPhase.Countdown) PlaceAll();
        if (step.Effect != DuelEffect.None || step.Snapshot.Phase != before.Phase) Broadcast();
    }

    private void Execute(DuelEffect effect)
    {
        switch (effect)
        {
            case DuelEffect.PrepareRound:
                PrepareRound();
                break;
            case DuelEffect.StartFight:
                Guarded("tir allié", () => ServerSettings.Instance.ToggleFriendlyFire(true));
                foreach (var (player, side) in Participants())
                    Guarded($"soin {side}", () => GameActions.HealToFull(player));
                break;
            case DuelEffect.ResetToWaiting:
                Guarded("nettoyage des armes", DestroyGivenItems);
                Guarded("réanimation hôte", () => GameActions.Revive(Player.LocalPlayer));
                break;
        }
    }

    private void PrepareRound()
    {
        Guarded("nettoyage des armes", DestroyGivenItems);
        var kit = DuelSession.Snapshot.Kit!;
        foreach (var (player, side) in Participants())
        {
            Guarded($"réanimation {side}", () => GameActions.Revive(player));
            Guarded($"vidage {side}", () => GameActions.ClearInventory(player));
            Guarded($"placement {side}", () => GameActions.Place(player, SpawnFor(side)));
            Guarded($"kit {side}", () => GiveKit(player, kit));
        }
    }

    private void GiveKit(Player player, Kit kit)
    {
        var (given, missing) = GameActions.GiveKit(player, kit);
        _givenItems.AddRange(given);
        foreach (var item in missing) DuelPlugin.Log.LogWarning($"Objet introuvable au moment du don : {item}");
    }

    private void DestroyGivenItems()
    {
        GameActions.Destroy(_givenItems);
        _givenItems.Clear();
    }

    private void PlaceAll()
    {
        foreach (var (player, side) in Participants())
            Guarded($"placement {side}", () => GameActions.Place(player, SpawnFor(side)));
    }

    private SpawnPoint SpawnFor(Side side) => ArenaGeometry.For(side, DuelSession.Snapshot.HostAtSpawnA, _spawns);

    private void KeepPlayersInZone()
    {
        var phase = DuelSession.Snapshot.Phase;
        if (phase != DuelPhase.Countdown && phase != DuelPhase.Fighting) return;
        if (Time.time < _nextZoneCheck) return;
        _nextZoneCheck = Time.time + ZoneCheckInterval;
        var center = GameActions.ToNumerics(_center);
        foreach (var (player, side) in Participants())
        {
            var position = GameActions.ToNumerics(player.Transform.position);
            if (!ArenaGeometry.IsOutside(position, center, DuelSession.Settings.ArenaRadius)) continue;
            Guarded($"retour en zone {side}", () => GameActions.Place(player, SpawnFor(side)));
        }
    }

    private static (Player Player, Side Side)[] Participants()
    {
        var guest = Guest();
        return guest == null
            ? new[] { (Player.LocalPlayer, Side.Host) }
            : new[] { (Player.LocalPlayer, Side.Host), (guest, Side.Guest) };
    }

    private void Broadcast()
    {
        _nextBroadcast = Time.time + (float)DuelSettings.StateBroadcastSeconds;
        DuelChannel.Send(DuelMessage.Of(WireState.From(DuelSession.Snapshot, Time.time)));
    }

    private void OnMessage(CSteamID sender, DuelMessage message)
    {
        if (message.Kind == MessageKind.Hello) _helloFrom = sender.m_SteamID;
        if (message.Kind == MessageKind.Rematch) RequestRematch();
    }

    private static void Guarded(string what, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            DuelPlugin.Log.LogError($"Échec ({what}) : {ex}");
        }
    }
}

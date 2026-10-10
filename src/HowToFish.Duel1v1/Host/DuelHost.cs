using System;
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

    public static DuelHost? Instance { get; private set; }

    private DuelRules? _rules;
    private (SpawnPoint A, SpawnPoint B) _spawns;
    private Vector3 _center;
    private ulong _helloFrom;
    private float _guestSeenAt = -1f;
    private float _nextBroadcast;
    private float _nextZoneCheck;
    private bool _kitsChecked;

    private void Awake()
    {
        Instance = this;
        DuelChannel.MessageReceived += OnMessage;
    }

    private void OnDestroy() => DuelChannel.MessageReceived -= OnMessage;

    private void Update()
    {
        if (!DuelSession.IsHostingDuel || Player.LocalPlayer == null || Server.Instance == null)
        {
            ResetLocalState();
            return;
        }
        if (!EnsureKits()) return;
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

    private void ResetLocalState()
    {
        _rules = null;
        _helloFrom = 0;
        _guestSeenAt = -1f;
        _kitsChecked = false;
    }

    private bool EnsureKits()
    {
        if (_kitsChecked) return _rules != null;
        _kitsChecked = true;
        var kits = KitFilter.KeepAvailable(DuelSession.Settings.Kits, GameActions.ItemExists, out var missing);
        foreach (var item in missing) DuelPlugin.Log.LogWarning($"Objet de kit inconnu, kit ignoré : {item}");
        if (kits.Count == 0)
        {
            DuelPlugin.Log.LogError("Aucun kit utilisable : vérifie 'Kits' dans htf.duel1v1.cfg");
            DuelSession.ShowNotice("Aucun kit d'armes valide — duel impossible (voir le log)", Time.time);
            return false;
        }
        var settings = DuelSession.Settings.WithKits(kits);
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
        }
    }

    private void PrepareRound()
    {
        var snapshot = DuelSession.Snapshot;
        foreach (var (player, side) in Participants())
        {
            Guarded($"réanimation {side}", () => GameActions.Revive(player));
            Guarded($"vidage {side}", () => GameActions.ClearInventory(player));
            Guarded($"placement {side}", () => GameActions.Place(player, ArenaGeometry.For(side, snapshot.HostAtSpawnA, _spawns)));
            Guarded($"kit {side}", () =>
            {
                foreach (var item in GameActions.GiveKit(player, snapshot.Kit!))
                    DuelPlugin.Log.LogWarning($"Objet introuvable au moment du don : {item}");
            });
        }
    }

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
            Guarded($"retour en zone {side}", () =>
                GameActions.Place(player, ArenaGeometry.For(side, DuelSession.Snapshot.HostAtSpawnA, _spawns)));
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

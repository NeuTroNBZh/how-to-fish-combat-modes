using System;
using System.Collections.Generic;
using System.Linq;
using HowToFish.Duel1v1.Core;
using HowToFish.Duel1v1.Core.Arena;
using HowToFish.Duel1v1.Host;
using HowToFish.Duel1v1.Net;
using Steamworks;
using UnityEngine;

namespace HowToFish.Duel1v1.Arena;

internal sealed class ArenaHost : MonoBehaviour
{
    public const ulong BotId = 1;
    private const float BroadcastSeconds = 2f;
    private const float WaitLogInterval = 5f;
    private const float PermanentNoticeSeconds = 3600f;

    public static ArenaHost? Instance { get; private set; }

    private readonly HashSet<ulong> _hello = new();
    private readonly Dictionary<ulong, float> _firstSeen = new();
    private ArenaRules? _rules;
    private ArenaExecutor? _executor;
    private CSteamID _lobby = CSteamID.Nil;
    private bool _settingsChecked;
    private int _pendingIsland = -1;
    private float _nextBroadcast;
    private float _nextWaitLog;

    private void Awake()
    {
        Instance = this;
        DuelChannel.MessageReceived += OnMessage;
    }

    private void OnDestroy() => DuelChannel.MessageReceived -= OnMessage;

    private void Update()
    {
        if (DuelSession.LobbyId != _lobby) ResetForLobby(DuelSession.LobbyId);
        if (!DuelSession.IsHostingArena || Player.LocalPlayer == null || Server.Instance == null) return;
        if (!EnsureSettings()) return;
        SyncRoster();
        // Le temps de la préparation est gelé tant que l'île de la partie charge.
        if (_pendingIsland < 0 || TryFinishIsland()) Step(new ArenaTick());
        _executor!.Update(Time.time);
        Publish();
    }

    public bool AllowsDamage(Player victim) =>
        victim != null && ArenaRules.AllowsDamage(DuelSession.ArenaSnapshot, victim.SteamID, Time.time);

    public void RequestStart() => Step(new ArenaStart());

    public void OnHitDealt(ulong attacker) => Step(new ArenaHit(attacker));

    public void OnPlayerDied(Player victim)
    {
        if (victim == null) return;
        var credit = HitTracker.TakeCredit(victim.SteamID, Time.time);
        Step(credit is { } hit ? new ArenaKill(hit.Attacker, victim.SteamID, hit.Weapon) : new ArenaDeath(victim.SteamID));
    }

    public void AddBot()
    {
        if (DuelSession.Settings.DebugSolo) Step(new ArenaJoin(BotId));
    }

    public void KillBot()
    {
        var local = Player.LocalPlayer;
        if (!DuelSession.Settings.DebugSolo || local == null) return;
        var held = local.Holding.HeldItem;
        Step(new ArenaKill(local.SteamID, BotId, held != null ? ItemNames.KeyOf(held.name) : ""));
    }

    private void ResetForLobby(CSteamID lobby)
    {
        _lobby = lobby;
        _executor?.ResetForGame();
        _executor = null;
        _rules = null;
        _hello.Clear();
        _firstSeen.Clear();
        _settingsChecked = false;
        _pendingIsland = -1;
        HitTracker.Clear();
    }

    private bool EnsureSettings()
    {
        if (_settingsChecked) return _rules != null;
        _settingsChecked = true;
        if (!DuelPreflight.TryValidateArena(DuelSession.ArenaSettings, DuelSession.Settings, DuelSession.Mode, out var settings, out var error))
        {
            DuelSession.LockLobby();
            SteamMatchmaking.SetLobbyJoinable(DuelSession.LobbyId, false);
            DuelSession.ShowNotice($"{error} — partie impossible, retourne au menu", Time.time, PermanentNoticeSeconds);
            return false;
        }
        DuelSession.UseArenaSettings(settings);
        _rules = new ArenaRules(settings, new SystemRandomSource(new System.Random()));
        _executor = new ArenaExecutor(settings.RandomUpgrades);
        DuelSession.ArenaSnapshot = ArenaSnapshot.Lobby(DuelSession.Mode);
        return true;
    }

    private void SyncRoster()
    {
        var local = Player.LocalPlayer!.SteamID;
        if (DuelSession.ArenaSnapshot.Find(local) == null) Step(new ArenaJoin(local));
        var present = new HashSet<ulong> { local, BotId };
        foreach (var player in PlayerManager.Players.Where(p => p != null && p != Player.LocalPlayer))
        {
            present.Add(player.SteamID);
            if (!_hello.Contains(player.SteamID)) KickIfSilent(player);
            else if (DuelSession.ArenaSnapshot.Find(player.SteamID) == null) Step(new ArenaJoin(player.SteamID));
        }
        foreach (var gone in DuelSession.ArenaSnapshot.Players.Select(p => p.SteamId).Where(id => !present.Contains(id)).ToList())
            Step(new ArenaLeave(gone));
    }

    private void KickIfSilent(Player player)
    {
        if (DuelSession.Settings.DebugSolo) return;
        if (!_firstSeen.TryGetValue(player.SteamID, out var since))
        {
            _firstSeen[player.SteamID] = Time.time;
            return;
        }
        if (Time.time - since < DuelSettings.HelloTimeoutSeconds) return;
        DuelPlugin.Log.LogWarning($"{player.SteamName} n'a pas le mod, expulsion");
        DuelSession.ShowNotice($"{player.SteamName} n'a pas le mod de combat", Time.time);
        ConnectionManager.Instance.KickSteamUserIfConnected(new CSteamID(player.SteamID));
        _firstSeen.Remove(player.SteamID);
    }

    private bool TryFinishIsland()
    {
        if (!GameActions.EnsureIsland(_pendingIsland))
        {
            if (Time.time >= _nextWaitLog)
            {
                _nextWaitLog = Time.time + WaitLogInterval;
                DuelPlugin.Log.LogInfo($"En attente de l'île {_pendingIsland} : {GameActions.IslandStatus()}");
            }
            return false;
        }
        _executor!.UseSpawns(ArenaSpawns.Compute(GameActions.ArenaCenter(), DuelSession.Settings.ArenaRadius));
        DuelPlugin.Log.LogInfo($"Île {_pendingIsland} prête");
        _pendingIsland = -1;
        return true;
    }

    private void Step(ArenaEvent e)
    {
        if (_rules == null) return;
        var before = DuelSession.ArenaSnapshot;
        var step = _rules.Apply(before, e, Time.time);
        DuelSession.ArenaSnapshot = step.Snapshot;
        if (step.Snapshot.Phase != before.Phase)
            DuelPlugin.Log.LogInfo($"Arène {before.Phase} -> {step.Snapshot.Phase} ({step.Snapshot.Players.Count} joueurs, île {step.Snapshot.Island})");
        foreach (var action in step.Actions) Guarded(action.ToString(), () => Execute(action));
        if (step.Actions.Count > 0 || step.Snapshot.Phase != before.Phase || step.Snapshot.Players.Count != before.Players.Count)
            _nextBroadcast = 0f;
    }

    private void Execute(ArenaAction action)
    {
        var executor = _executor!;
        switch (action)
        {
            case PrepareIsland prepare:
                executor.ResetForGame();
                _pendingIsland = prepare.Island;
                break;
            case Respawn respawn:
                WithPlayer(respawn.Id, p => executor.Respawn(p, respawn.Kit, Time.time));
                break;
            case Equip equip:
                WithPlayer(equip.Id, p => executor.Equip(p, equip.Kit));
                break;
            case RotateKit rotate:
                executor.ClearLoadouts();
                foreach (var alive in DuelSession.ArenaSnapshot.Players.Where(p => p.Alive))
                    WithPlayer(alive.SteamId, p => executor.Equip(p, rotate.Kit));
                break;
            case Strip strip:
                executor.Strip(strip.Id);
                break;
            case StartFight:
                ServerSettings.Instance.ToggleFriendlyFire(true);
                break;
            case EndGame end:
                DuelPlugin.Log.LogInfo($"Partie terminée, gagnant {end.Winner}");
                break;
        }
    }

    private static void WithPlayer(ulong id, Action<Player> act)
    {
        var player = PlayerManager.Players.FirstOrDefault(p => p != null && p.SteamID == id);
        if (player != null) act(player);
    }

    private void Publish()
    {
        var ladder = DuelSession.ArenaSettings.Ladder.Weapons;
        var wire = ArenaWire.From(DuelSession.ArenaSnapshot, Time.time, ladder);
        DuelSession.SetArenaDisplay(wire, Time.time);
        if (Time.time < _nextBroadcast) return;
        _nextBroadcast = Time.time + BroadcastSeconds;
        DuelChannel.SendArena(wire);
    }

    private void OnMessage(CSteamID sender, DuelMessage message)
    {
        if (DuelSession.IsHostingArena && message.Kind == MessageKind.Hello) _hello.Add(sender.m_SteamID);
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

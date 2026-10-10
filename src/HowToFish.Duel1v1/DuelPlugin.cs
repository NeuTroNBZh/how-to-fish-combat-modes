using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HowToFish.Duel1v1;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class DuelPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "htf.duel1v1";
    public const string PluginName = "Duel 1v1";
    public const string PluginVersion = "1.0.0";

    internal static ManualLogSource Log { get; private set; } = null!;
    internal static GameObject Root { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        var (settings, debugKey) = DuelConfigBinder.Load(Config, Log);
        var compatible = CompatibilityCheck.Run(Log);
        DuelSession.Initialize(settings, debugKey, compatible);

        Root = new GameObject("HowToFish.Duel1v1");
        DontDestroyOnLoad(Root);
        Root.hideFlags = HideFlags.HideAndDontSave;

        if (compatible) new Harmony(PluginGuid).PatchAll(typeof(DuelPlugin).Assembly);
        if (compatible) Root.AddComponent<Net.DuelLobby>();
        Log.LogInfo($"{PluginName} {PluginVersion} chargé (compatible={compatible}, kits={settings.Kits.Count})");
    }
}

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
    public const string PluginVersion = "1.3.0";

    internal static ManualLogSource Log { get; private set; } = null!;
    internal static GameObject Root { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        var (settings, arena, debugKey) = DuelConfigBinder.Load(Config, Log);
        var compatible = CompatibilityCheck.Run(Log);
        Ui.Native.GameUiKit.ForceFallback =
            Config.Bind("Debug", "ForceImguiFallback", false, "Affiche l'ancienne interface IMGUI (diagnostic)").Value;
        DuelSession.Initialize(settings, arena, debugKey, compatible);

        Root = new GameObject("HowToFish.Duel1v1");
        DontDestroyOnLoad(Root);
        Root.hideFlags = HideFlags.HideAndDontSave;

        var menu = Root.AddComponent<Ui.DuelMenu>();
        Root.AddComponent<Ui.Native.CombatMenuScreen>();
        if (compatible)
        {
            new Harmony(PluginGuid).PatchAll(typeof(DuelPlugin).Assembly);
            Root.AddComponent<Net.DuelLobby>();
            Root.AddComponent<Net.DuelChannel>();
            Root.AddComponent<Host.DuelHost>();
            Root.AddComponent<Arena.ArenaHost>();
            Root.AddComponent<Ui.DuelHud>();
            Root.AddComponent<Ui.ArenaHud>();
            Root.AddComponent<Ui.CombatControllers>();
            Root.AddComponent<Ui.Native.LobbyScreen>();
            Root.AddComponent<Ui.Native.EndScreen>();
            Root.AddComponent<Ui.DebugTools>();
        }
        else
        {
            menu.InjectButton();
        }
        Log.LogInfo($"{PluginName} {PluginVersion} chargé (compatible={compatible}, kits={settings.Kits.Count})");
    }
}

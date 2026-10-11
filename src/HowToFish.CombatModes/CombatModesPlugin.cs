using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HowToFish.CombatModes;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class CombatModesPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "htf.combatmodes";
    public const string PluginName = "Combat Modes";
    public const string PluginVersion = "1.4.0";

    internal static ManualLogSource Log { get; private set; } = null!;
    internal static GameObject Root { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        var (settings, arena, debugKey) = ConfigBinder.Load(Config, Log);
        var compatible = CompatibilityCheck.Run(Log);
        Ui.Native.GameUiKit.ForceFallback =
            Config.Bind("Debug", "ForceImguiFallback", false, "Affiche l'ancienne interface IMGUI (diagnostic)").Value;
        CombatSession.Initialize(settings, arena, debugKey, compatible);

        Root = new GameObject("HowToFish.CombatModes");
        DontDestroyOnLoad(Root);
        Root.hideFlags = HideFlags.HideAndDontSave;

        var menu = Root.AddComponent<Ui.MainMenuButton>();
        Root.AddComponent<Ui.Native.CombatMenuScreen>();
        if (compatible)
        {
            new Harmony(PluginGuid).PatchAll(typeof(CombatModesPlugin).Assembly);
            Root.AddComponent<Net.CombatLobby>();
            Root.AddComponent<Net.CombatChannel>();
            Root.AddComponent<Host.DuelHost>();
            Root.AddComponent<Arena.ArenaHost>();
            Root.AddComponent<Ui.DuelHud>();
            Root.AddComponent<Ui.ArenaHud>();
            Root.AddComponent<Ui.CombatControllers>();
            Root.AddComponent<Ui.Native.LobbyScreen>();
            Root.AddComponent<Ui.Native.EndScreen>();
            Root.AddComponent<Ui.Native.CombatHud>();
            Root.AddComponent<Ui.Native.ScoreboardScreen>();
            Root.AddComponent<Ui.Native.NoticeScreen>();
            Root.AddComponent<Ui.DebugTools>();
        }
        else
        {
            menu.InjectButton();
        }
        Log.LogInfo($"{PluginName} {PluginVersion} chargé (compatible={compatible}, kits={settings.Kits.Count})");
    }
}

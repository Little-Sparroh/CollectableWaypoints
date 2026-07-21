using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class CollectableWaypointsPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.collectablewaypoints";
    public const string PluginName = "CollectableWaypoints";
    public const string PluginVersion = "1.0.4";

    internal static new ManualLogSource Logger;

    internal static ConfigEntry<bool> dataLogWaypoints;
    internal static ConfigEntry<bool> pumpkinWaypoints;
    internal static ConfigEntry<bool> bearWaypoints;
    internal static ConfigEntry<bool> otherPunchCollectableWaypoints;

    internal static ConfigEntry<string> dataLogWaypointColor;
    internal static ConfigEntry<string> bearWaypointColor;
    internal static ConfigEntry<string> pumpkinWaypointColor;
    internal static ConfigEntry<string> otherWaypointColor;

    // Defaults requested: data logs green, bears blue, pumpkins orange.
    private static readonly Color DefaultDataLogColor = new Color(0.20f, 0.90f, 0.30f, 1f);
    private static readonly Color DefaultBearColor = new Color(0.25f, 0.55f, 1.00f, 1f);
    private static readonly Color DefaultPumpkinColor = new Color(1.00f, 0.55f, 0.10f, 1f);
    private static readonly Color DefaultOtherColor = new Color(0.85f, 0.45f, 1.00f, 1f);

    internal static Color DataLogWaypointColor => WaypointUtil.ParseColor(dataLogWaypointColor.Value, DefaultDataLogColor);
    internal static Color BearWaypointColor => WaypointUtil.ParseColor(bearWaypointColor.Value, DefaultBearColor);
    internal static Color PumpkinWaypointColor => WaypointUtil.ParseColor(pumpkinWaypointColor.Value, DefaultPumpkinColor);
    internal static Color OtherWaypointColor => WaypointUtil.ParseColor(otherWaypointColor.Value, DefaultOtherColor);

    internal static CollectableWaypointsPlugin Instance { get; set; }

    private FileSystemWatcher _configWatcher;
    private volatile bool _configFileChanged;
    private float _reloadCooldown = -1f;
    private const float ReloadDebounceSeconds = 0.25f;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        dataLogWaypoints = Config.Bind("General", "Data Log Waypoints", true, "If true, shows waypoints for undiscovered data logs.");
        pumpkinWaypoints = Config.Bind("General", "Pumpkin Waypoints", true, "If true, shows waypoints for undiscovered pumpkins.");
        bearWaypoints = Config.Bind("General", "Bear Waypoints", true, "If true, shows waypoints for undiscovered bears.");
        otherPunchCollectableWaypoints = Config.Bind(
            "General",
            "Other Punch Collectable Waypoints",
            true,
            "If true, shows waypoints for any other punch collectables (future event sets, etc.).");

        dataLogWaypointColor = Config.Bind(
            "Colors",
            "Data Log Waypoint Color",
            "#33E64D",
            "Waypoint color for data logs. Accepts #RRGGBB / #RRGGBBAA or R,G,B[,A].");
        bearWaypointColor = Config.Bind(
            "Colors",
            "Bear Waypoint Color",
            "#408CFF",
            "Waypoint color for bears. Accepts #RRGGBB / #RRGGBBAA or R,G,B[,A].");
        pumpkinWaypointColor = Config.Bind(
            "Colors",
            "Pumpkin Waypoint Color",
            "#FF8C1A",
            "Waypoint color for pumpkins. Accepts #RRGGBB / #RRGGBBAA or R,G,B[,A].");
        otherWaypointColor = Config.Bind(
            "Colors",
            "Other Punch Collectable Waypoint Color",
            "#D973FF",
            "Waypoint color for other/future punch collectables. Accepts #RRGGBB / #RRGGBBAA or R,G,B[,A].");

        dataLogWaypoints.SettingChanged += (_, __) => DataLogWaypointPatches.ApplyConfig();
        pumpkinWaypoints.SettingChanged += (_, __) => PunchCollectableWaypointPatches.ApplyAll();
        bearWaypoints.SettingChanged += (_, __) => PunchCollectableWaypointPatches.ApplyAll();
        otherPunchCollectableWaypoints.SettingChanged += (_, __) => PunchCollectableWaypointPatches.ApplyAll();

        dataLogWaypointColor.SettingChanged += (_, __) => DataLogWaypointPatches.ApplyConfig();
        bearWaypointColor.SettingChanged += (_, __) => PunchCollectableWaypointPatches.ApplyAll();
        pumpkinWaypointColor.SettingChanged += (_, __) => PunchCollectableWaypointPatches.ApplyAll();
        otherWaypointColor.SettingChanged += (_, __) => PunchCollectableWaypointPatches.ApplyAll();

        SetupConfigWatcher();

        try
        {
            var harmony = new Harmony(PluginGUID);

            DataLogWaypointPatches.Initialize(harmony);
            PunchCollectableWaypointPatches.Initialize(harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error applying patches: {ex.Message}");
        }

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void Update()
    {
        if (_configFileChanged)
        {
            _configFileChanged = false;
            _reloadCooldown = ReloadDebounceSeconds;
        }

        if (_reloadCooldown < 0f)
            return;

        _reloadCooldown -= Time.unscaledDeltaTime;
        if (_reloadCooldown > 0f)
            return;

        _reloadCooldown = -1f;
        try
        {
            Config.Reload();
            // Rebuild waypoints after disk reload so color/toggle changes apply immediately.
            DataLogWaypointPatches.ApplyConfig();
            PunchCollectableWaypointPatches.ApplyAll();
            Logger.LogInfo("Configuration reloaded from disk.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to reload configuration: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        if (_configWatcher == null)
            return;

        _configWatcher.EnableRaisingEvents = false;
        _configWatcher.Changed -= OnConfigFileChanged;
        _configWatcher.Created -= OnConfigFileChanged;
        _configWatcher.Renamed -= OnConfigFileRenamed;
        _configWatcher.Dispose();
        _configWatcher = null;
    }

    private void SetupConfigWatcher()
    {
        try
        {
            var configPath = Config.ConfigFilePath;
            var directory = Path.GetDirectoryName(configPath);
            var fileName = Path.GetFileName(configPath);

            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
            {
                Logger.LogWarning("Could not set up config file watcher: invalid config path.");
                return;
            }

            _configWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                IncludeSubdirectories = false
            };
            _configWatcher.Changed += OnConfigFileChanged;
            _configWatcher.Created += OnConfigFileChanged;
            _configWatcher.Renamed += OnConfigFileRenamed;
            _configWatcher.EnableRaisingEvents = true;

            Logger.LogInfo($"Watching config file for changes: {configPath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to set up config file watcher: {ex.Message}");
        }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        _configFileChanged = true;
    }

    private void OnConfigFileRenamed(object sender, RenamedEventArgs e)
    {
        _configFileChanged = true;
    }
}

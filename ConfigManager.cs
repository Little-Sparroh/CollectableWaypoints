using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

public static class ConfigManager
{
    private const float DebounceSeconds = 0.25f;

    private static readonly Color DefaultDataLogColor = new(0.20f, 0.90f, 0.30f, 1f);
    private static readonly Color DefaultBearColor = new(0.25f, 0.55f, 1.00f, 1f);
    private static readonly Color DefaultPumpkinColor = new(1.00f, 0.55f, 0.10f, 1f);
    private static readonly Color DefaultOtherColor = new(0.85f, 0.45f, 1.00f, 1f);

    private static ConfigFile config;
    private static ManualLogSource logger;
    private static FileSystemWatcher configWatcher;
    private static volatile bool pendingRefresh;
    private static volatile bool reloadPending;
    private static float lastReloadTime;

    public static ConfigEntry<bool> DataLogWaypoints { get; private set; }
    public static ConfigEntry<bool> PumpkinWaypoints { get; private set; }
    public static ConfigEntry<bool> BearWaypoints { get; private set; }
    public static ConfigEntry<bool> OtherPunchCollectableWaypoints { get; private set; }

    public static ConfigEntry<string> DataLogWaypointColorEntry { get; private set; }
    public static ConfigEntry<string> BearWaypointColorEntry { get; private set; }
    public static ConfigEntry<string> PumpkinWaypointColorEntry { get; private set; }
    public static ConfigEntry<string> OtherWaypointColorEntry { get; private set; }

    public static Color DataLogWaypointColor =>
        WaypointUtil.ParseColor(DataLogWaypointColorEntry.Value, DefaultDataLogColor);

    public static Color BearWaypointColor =>
        WaypointUtil.ParseColor(BearWaypointColorEntry.Value, DefaultBearColor);

    public static Color PumpkinWaypointColor =>
        WaypointUtil.ParseColor(PumpkinWaypointColorEntry.Value, DefaultPumpkinColor);

    public static Color OtherWaypointColor =>
        WaypointUtil.ParseColor(OtherWaypointColorEntry.Value, DefaultOtherColor);

    public static void Initialize(ConfigFile configFile, ManualLogSource log)
    {
        config = configFile;
        logger = log;

        DataLogWaypoints = config.Bind(
            "General",
            "Data Log Waypoints",
            true,
            "If true, shows waypoints for undiscovered data logs.");
        PumpkinWaypoints = config.Bind(
            "General",
            "Pumpkin Waypoints",
            true,
            "If true, shows waypoints for undiscovered pumpkins.");
        BearWaypoints = config.Bind(
            "General",
            "Bear Waypoints",
            true,
            "If true, shows waypoints for undiscovered bears.");
        OtherPunchCollectableWaypoints = config.Bind(
            "General",
            "Other Waypoints",
            true,
            "If true, shows waypoints for any other punch collectables (future event sets, etc.).");

        DataLogWaypointColorEntry = config.Bind(
            "Colors",
            "Data Log Waypoint Color",
            "#33E64D",
            "Waypoint color for data logs. Accepts #RRGGBB / #RRGGBBAA or R,G,B[,A].");
        BearWaypointColorEntry = config.Bind(
            "Colors",
            "Bear Waypoint Color",
            "#408CFF",
            "Waypoint color for bears. Accepts #RRGGBB / #RRGGBBAA or R,G,B[,A].");
        PumpkinWaypointColorEntry = config.Bind(
            "Colors",
            "Pumpkin Waypoint Color",
            "#FF8C1A",
            "Waypoint color for pumpkins. Accepts #RRGGBB / #RRGGBBAA or R,G,B[,A].");
        OtherWaypointColorEntry = config.Bind(
            "Colors",
            "Other Waypoint Color",
            "#D973FF",
            "Waypoint color for other/future punch collectables. Accepts #RRGGBB / #RRGGBBAA or R,G,B[,A].");

        DataLogWaypoints.SettingChanged += OnSettingChanged;
        PumpkinWaypoints.SettingChanged += OnSettingChanged;
        BearWaypoints.SettingChanged += OnSettingChanged;
        OtherPunchCollectableWaypoints.SettingChanged += OnSettingChanged;
        DataLogWaypointColorEntry.SettingChanged += OnSettingChanged;
        BearWaypointColorEntry.SettingChanged += OnSettingChanged;
        PumpkinWaypointColorEntry.SettingChanged += OnSettingChanged;
        OtherWaypointColorEntry.SettingChanged += OnSettingChanged;

        try
        {
            SetupFileWatcher();
        }
        catch (Exception ex)
        {
            logger.LogError($"Error setting up config file watcher: {ex.Message}");
        }
    }


    public static void Tick()
    {
        if (!reloadPending)
            return;

        if (Time.unscaledTime - lastReloadTime < DebounceSeconds)
            return;

        reloadPending = false;
        lastReloadTime = Time.unscaledTime;

        try
        {
            config.Reload();
            pendingRefresh = true;
            logger.LogInfo("Configuration reloaded from disk.");
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to reload configuration: {ex.Message}");
        }
    }

    public static bool ConsumePendingRefresh()
    {
        if (!pendingRefresh)
            return false;

        pendingRefresh = false;
        return true;
    }

    public static void Dispose()
    {
        if (DataLogWaypoints != null)
            DataLogWaypoints.SettingChanged -= OnSettingChanged;
        if (PumpkinWaypoints != null)
            PumpkinWaypoints.SettingChanged -= OnSettingChanged;
        if (BearWaypoints != null)
            BearWaypoints.SettingChanged -= OnSettingChanged;
        if (OtherPunchCollectableWaypoints != null)
            OtherPunchCollectableWaypoints.SettingChanged -= OnSettingChanged;
        if (DataLogWaypointColorEntry != null)
            DataLogWaypointColorEntry.SettingChanged -= OnSettingChanged;
        if (BearWaypointColorEntry != null)
            BearWaypointColorEntry.SettingChanged -= OnSettingChanged;
        if (PumpkinWaypointColorEntry != null)
            PumpkinWaypointColorEntry.SettingChanged -= OnSettingChanged;
        if (OtherWaypointColorEntry != null)
            OtherWaypointColorEntry.SettingChanged -= OnSettingChanged;

        if (configWatcher != null)
        {
            configWatcher.EnableRaisingEvents = false;
            configWatcher.Changed -= OnConfigFileChanged;
            configWatcher.Created -= OnConfigFileChanged;
            configWatcher.Renamed -= OnConfigFileChanged;
            configWatcher.Dispose();
            configWatcher = null;
        }
    }

    private static void SetupFileWatcher()
    {
        configWatcher = new FileSystemWatcher(Paths.ConfigPath, $"{CollectableWaypointsPlugin.PluginGUID}.cfg")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            IncludeSubdirectories = false
        };
        configWatcher.Changed += OnConfigFileChanged;
        configWatcher.Created += OnConfigFileChanged;
        configWatcher.Renamed += OnConfigFileChanged;
        configWatcher.EnableRaisingEvents = true;

        logger.LogInfo(
            $"Watching config file for changes: {Path.Combine(Paths.ConfigPath, CollectableWaypointsPlugin.PluginGUID + ".cfg")}");
    }

    private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        reloadPending = true;
    }

    private static void OnSettingChanged(object sender, EventArgs e)
    {
        pendingRefresh = true;
    }
}
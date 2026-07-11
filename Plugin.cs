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
    public const string PluginVersion = "1.0.1";

    internal static new ManualLogSource Logger;

    internal static ConfigEntry<bool> dataLogWaypoints;
    internal static ConfigEntry<bool> pumpkinWaypoints;
    internal static ConfigEntry<bool> bearWaypoints;

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

        dataLogWaypoints.SettingChanged += (_, __) => DataLogWaypointPatches.ApplyConfig();
        pumpkinWaypoints.SettingChanged += (_, __) => PumpkinWaypointPatches.ApplyConfig();
        bearWaypoints.SettingChanged += (_, __) => BearWaypointPatches.ApplyConfig();

        SetupConfigWatcher();

        try
        {
            var harmony = new Harmony(PluginGUID);

            DataLogWaypointPatches.Initialize(harmony);
            PumpkinWaypointPatches.Initialize();
            BearWaypointPatches.Initialize();
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

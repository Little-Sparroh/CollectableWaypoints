using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class CollectableWaypointsPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.collectablewaypoints";
    public const string PluginName = "CollectableWaypoints";
    public const string PluginVersion = "1.0.5";

    internal new static ManualLogSource Logger;

    private Harmony harmony;

    private void Awake()
    {
        Logger = base.Logger;

        ConfigManager.Initialize(Config, Logger);

        try
        {
            harmony = new Harmony(PluginGUID);

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
        ConfigManager.Tick();

        if (ConfigManager.ConsumePendingRefresh())
        {
            DataLogWaypointPatches.ApplyConfig();
            PunchCollectableWaypointPatches.ApplyAll();
        }
    }

    private void OnDestroy()
    {
        ConfigManager.Dispose();
        harmony?.UnpatchSelf();
    }
}
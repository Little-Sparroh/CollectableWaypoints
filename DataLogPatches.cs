using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

public static class DataLogWaypointPatches
{
    private static Dictionary<string, Transform> datalogPings = new Dictionary<string, Transform>();
    private static FieldInfo logIDField;

    public static void Initialize(Harmony harmony)
    {
        try
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            logIDField = typeof(TextLogInteractable).GetField("logID", BindingFlags.NonPublic | BindingFlags.Instance);
            var playerDataOnDataLogOpened = typeof(PlayerData).GetMethod("OnDataLogOpened", BindingFlags.Public | BindingFlags.Instance);
            var postfix = typeof(DataLogWaypointPatches).GetMethod("PlayerDataOnDataLogOpened_Postfix", BindingFlags.Public | BindingFlags.Static);
            if (playerDataOnDataLogOpened != null && postfix != null)
            {
                harmony.Patch(playerDataOnDataLogOpened, postfix: new HarmonyMethod(postfix));
            }
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError($"Error initializing DataLogWaypointPatches: {ex.Message}");
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try
        {
            var harmony = new Harmony(CollectableWaypointsPlugin.PluginGUID);
            var textSetup = typeof(TextLogWindow).GetMethod("Setup", new System.Type[] { typeof(string) });
            var imageSetup = typeof(ImageLogWindow).GetMethod("Setup", new System.Type[] { typeof(string) });
            var textPostfix = typeof(DataLogWaypointPatches).GetMethod("TextLogWindowSetup_Postfix", BindingFlags.Public | BindingFlags.Static);
            var imagePostfix = typeof(DataLogWaypointPatches).GetMethod("ImageLogWindowSetup_Postfix", BindingFlags.Public | BindingFlags.Static);
            if (textSetup != null && imageSetup != null && textPostfix != null && imagePostfix != null)
            {
                harmony.Patch(textSetup, postfix: new HarmonyMethod(textPostfix));
                harmony.Patch(imageSetup, postfix: new HarmonyMethod(imagePostfix));
            }

            // Scene changed; previous transforms are no longer valid.
            datalogPings.Clear();
            ApplyConfig();
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError($"Error in OnSceneLoaded: {ex.Message}");
        }
    }

    public static void ApplyConfig()
    {
        try
        {
            if (!CollectableWaypointsPlugin.dataLogWaypoints.Value)
            {
                ClearAllPings();
                return;
            }

            if (logIDField == null || Highlighter.Instance == null)
                return;

            var interactables = Object.FindObjectsOfType<TextLogInteractable>();
            foreach (var tli in interactables)
            {
                if (tli == null)
                    continue;

                string logID = logIDField.GetValue(tli) as string;
                if (logID == null || datalogPings.ContainsKey(logID))
                    continue;

                if (PlayerData.Instance != null
                    && PlayerData.Instance.discoveredDataLogs != null
                    && !PlayerData.Instance.discoveredDataLogs.Contains(logID))
                {
                    Highlighter.Instance.AddWaypointPing(tli.transform);
                    datalogPings[logID] = tli.transform;
                }
            }
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError($"Error applying data log waypoint config: {ex.Message}");
        }
    }

    private static void ClearAllPings()
    {
        foreach (var kvp in datalogPings)
        {
            if (kvp.Value != null)
                Highlighter.Instance?.RemovePing(kvp.Value);
        }
        datalogPings.Clear();
    }

    public static void TextLogWindowSetup_Postfix(string id)
    {
        try
        {
            if (!CollectableWaypointsPlugin.dataLogWaypoints.Value) return;
            if (datalogPings.TryGetValue(id, out var target))
            {
                Highlighter.Instance?.RemovePing(target);
                datalogPings.Remove(id);
            }
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError($"Error in TextLogWindowSetup_Postfix: {ex.Message}");
        }
    }

    public static void ImageLogWindowSetup_Postfix(string id)
    {
        try
        {
            if (!CollectableWaypointsPlugin.dataLogWaypoints.Value) return;
            if (datalogPings.TryGetValue(id, out var target))
            {
                Highlighter.Instance?.RemovePing(target);
                datalogPings.Remove(id);
            }
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError($"Error in ImageLogWindowSetup_Postfix: {ex.Message}");
        }
    }

    public static void PlayerDataOnDataLogOpened_Postfix(string id)
    {
        try
        {
            if (!CollectableWaypointsPlugin.dataLogWaypoints.Value) return;
            if (datalogPings.TryGetValue(id, out var target))
            {
                Highlighter.Instance?.RemovePing(target);
                datalogPings.Remove(id);
            }
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError($"Error in PlayerDataOnDataLogOpened_Postfix: {ex.Message}");
        }
    }
}

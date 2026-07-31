using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class DataLogWaypointPatches
{
    private static readonly Dictionary<string, Transform> trackedPings = new();

    private static FieldInfo logIDField;
    private static bool patchedLogWindows;

    public static void Initialize(Harmony harmony)
    {
        try
        {
            logIDField = typeof(TextLogInteractable).GetField("logID", BindingFlags.NonPublic | BindingFlags.Instance);

            PatchMethod(harmony, typeof(PlayerData), "OnDataLogOpened", nameof(OnDataLogOpened_Postfix));

            TryPatchLogWindows(harmony);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error initializing DataLogWaypointPatches: " + ex.Message);
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try
        {
            TryPatchLogWindows(new Harmony(CollectableWaypointsPlugin.PluginGUID));


            trackedPings.Clear();
            ApplyConfig();
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error in OnSceneLoaded for data logs: " + ex.Message);
        }
    }

    private static void TryPatchLogWindows(Harmony harmony)
    {
        if (patchedLogWindows)
            return;

        try
        {
            var textOk = PatchMethod(harmony, typeof(TextLogWindow), "Setup", nameof(OnLogWindowSetup_Postfix),
                typeof(string));
            var imageOk = PatchMethod(harmony, typeof(ImageLogWindow), "Setup", nameof(OnLogWindowSetup_Postfix),
                typeof(string));
            patchedLogWindows = textOk && imageOk;
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error patching log windows: " + ex.Message);
        }
    }

    private static bool PatchMethod(Harmony harmony, Type type, string methodName, string postfixName,
        params Type[] parameters)
    {
        var target = parameters == null || parameters.Length == 0
            ? type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            : type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                parameters, null);

        var postfix = typeof(DataLogWaypointPatches).GetMethod(postfixName, BindingFlags.Public | BindingFlags.Static);
        if (target == null || postfix == null)
            return false;

        harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        return true;
    }

    public static void ApplyConfig()
    {
        try
        {
            if (!ConfigManager.DataLogWaypoints.Value)
            {
                ClearAllPings();
                return;
            }

            if (logIDField == null || Highlighter.Instance == null)
                return;


            ClearAllPings();

            var interactables = Object.FindObjectsOfType<TextLogInteractable>();
            foreach (var tli in interactables)
            {
                if (tli == null)
                    continue;

                var logID = logIDField.GetValue(tli) as string;
                if (string.IsNullOrEmpty(logID) || trackedPings.ContainsKey(logID))
                    continue;

                if (PlayerData.Instance == null
                    || PlayerData.Instance.discoveredDataLogs == null
                    || PlayerData.Instance.discoveredDataLogs.Contains(logID))
                    continue;

                if (WaypointUtil.TryAddWaypoint(tli.transform, ConfigManager.DataLogWaypointColor))
                    trackedPings[logID] = tli.transform;
            }
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error applying data log waypoint config: " + ex.Message);
        }
    }

    private static void ClearAllPings()
    {
        foreach (var kvp in trackedPings)
            WaypointUtil.RemoveWaypoint(kvp.Value);

        trackedPings.Clear();
    }

    private static void RemovePing(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        if (!trackedPings.TryGetValue(id, out var target))
            return;

        WaypointUtil.RemoveWaypoint(target);
        trackedPings.Remove(id);
    }

    public static void OnLogWindowSetup_Postfix(string id)
    {
        try
        {
            RemovePing(id);
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error in OnLogWindowSetup_Postfix: " + ex.Message);
        }
    }

    public static void OnDataLogOpened_Postfix(string id)
    {
        try
        {
            RemovePing(id);
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error in OnDataLogOpened_Postfix: " + ex.Message);
        }
    }
}
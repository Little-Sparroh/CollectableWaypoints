using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

public static class PumpkinWaypointPatches
{
    private static readonly List<Transform> pumpkinPings = new List<Transform>();

    public static void Initialize()
    {
        try
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error initializing PumpkinWaypointPatches: " + ex.Message);
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try
        {
            // Scene changed; previous transforms are no longer valid.
            pumpkinPings.Clear();
            ApplyConfig();
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error in OnSceneLoaded for pumpkins: " + ex.Message);
        }
    }

    public static void ApplyConfig()
    {
        try
        {
            if (!CollectableWaypointsPlugin.pumpkinWaypoints.Value || LevelData.IsHub)
            {
                ClearAllPings();
                return;
            }

            if (Highlighter.Instance == null)
                return;

            GameObject[] pumpkins = Object.FindObjectsOfType<GameObject>()
                .Where(go => go != null && go.name.Contains("Pumpkin"))
                .ToArray();

            foreach (GameObject pumpkin in pumpkins)
            {
                if (pumpkinPings.Contains(pumpkin.transform))
                    continue;

                Highlighter.Instance.AddWaypointPing(pumpkin.transform);
                pumpkinPings.Add(pumpkin.transform);
            }
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error applying pumpkin waypoint config: " + ex.Message);
        }
    }

    private static void ClearAllPings()
    {
        foreach (var target in pumpkinPings)
        {
            if (target != null)
                Highlighter.Instance?.RemovePing(target);
        }
        pumpkinPings.Clear();
    }
}

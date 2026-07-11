using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

public static class BearWaypointPatches
{
    private static readonly List<Transform> bearPings = new List<Transform>();
    private static readonly string[] BearNameTokens = { "Bear", "bear", "Teddy", "teddy", "Bruce", "bruce", "Bruiser", "bruiser" };

    public static void Initialize()
    {
        try
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error initializing BearWaypointPatches: " + ex.Message);
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try
        {
            // Scene changed; previous transforms are no longer valid.
            bearPings.Clear();
            ApplyConfig();
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error in OnSceneLoaded for bears: " + ex.Message);
        }
    }

    public static void ApplyConfig()
    {
        try
        {
            if (!CollectableWaypointsPlugin.bearWaypoints.Value || LevelData.IsHub)
            {
                ClearAllPings();
                return;
            }

            if (Highlighter.Instance == null)
                return;

            GameObject[] bears = Object.FindObjectsOfType<GameObject>()
                .Where(go => go != null && BearNameTokens.Any(n => go.name.Contains(n)))
                .ToArray();

            foreach (GameObject bear in bears)
            {
                if (bearPings.Contains(bear.transform))
                    continue;

                Highlighter.Instance.AddWaypointPing(bear.transform);
                bearPings.Add(bear.transform);
            }
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error applying bear waypoint config: " + ex.Message);
        }
    }

    private static void ClearAllPings()
    {
        foreach (var target in bearPings)
        {
            if (target != null)
                Highlighter.Instance?.RemovePing(target);
        }
        bearPings.Clear();
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PunchCollectableWaypointPatches
{
    private const string PumpkinApiPrefix = "col_pump";

    private static FieldInfo profileField;
    private static FieldInfo indexField;
    private static readonly List<TrackedPing> trackedPings = new();
    private static bool loggedProfilesThisScene;


    private static readonly string[] BearTokens = { "bear", "teddy", "bruce" };
    private static readonly string[] PumpkinTokens = { "pumpkin", "pump", "jack", "lantern", "gourd", "halloween" };

    public static void Initialize(Harmony harmony)
    {
        try
        {
            profileField = typeof(PunchCollectable).GetField("profile", BindingFlags.NonPublic | BindingFlags.Instance);
            indexField = typeof(PunchCollectable).GetField("index", BindingFlags.NonPublic | BindingFlags.Instance);

            var addMeleeForce =
                typeof(PunchCollectable).GetMethod("AddMeleeForce", BindingFlags.Public | BindingFlags.Instance);
            var postfix = typeof(PunchCollectableWaypointPatches).GetMethod(
                nameof(PunchCollectable_AddMeleeForce_Postfix), BindingFlags.Public | BindingFlags.Static);
            if (addMeleeForce != null && postfix != null)
                harmony.Patch(addMeleeForce, postfix: new HarmonyMethod(postfix));

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error initializing PunchCollectableWaypointPatches: " +
                                                       ex.Message);
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try
        {
            trackedPings.Clear();
            loggedProfilesThisScene = false;
            ApplyAll();
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error in OnSceneLoaded for punch collectables: " + ex.Message);
        }
    }

    public static void ApplyAll()
    {
        try
        {
            if (LevelData.IsHub)
            {
                ClearAllPings();
                return;
            }

            var bearsEnabled = ConfigManager.BearWaypoints.Value;
            var pumpkinsEnabled = ConfigManager.PumpkinWaypoints.Value;
            var othersEnabled = ConfigManager.OtherPunchCollectableWaypoints.Value;


            ClearAllPings();

            if (!bearsEnabled && !pumpkinsEnabled && !othersEnabled)
                return;

            if (profileField == null || Highlighter.Instance == null)
                return;

            var collectables = Object.FindObjectsOfType<PunchCollectable>();
            MaybeLogProfiles(collectables);

            foreach (var collectable in collectables)
            {
                if (collectable == null)
                    continue;

                if (!TryGetProfile(collectable, out var profile) || profile == null)
                    continue;

                var kind = Classify(collectable, profile);
                if (!IsKindEnabled(kind, bearsEnabled, pumpkinsEnabled, othersEnabled))
                    continue;

                if (IsTracked(collectable.transform))
                    continue;

                var color = GetColor(kind);

                if (!WaypointUtil.TryAddWaypoint(collectable.transform, color))
                    continue;

                trackedPings.Add(new TrackedPing
                {
                    Transform = collectable.transform,
                    Kind = kind,
                    ApiName = profile.APIName ?? string.Empty,
                    Index = GetIndex(collectable)
                });
            }
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError(
                "Error applying punch collectable waypoint config: " + ex.Message);
        }
    }

    private static Color GetColor(CollectableKind kind)
    {
        switch (kind)
        {
            case CollectableKind.Bear:
                return ConfigManager.BearWaypointColor;
            case CollectableKind.Pumpkin:
                return ConfigManager.PumpkinWaypointColor;
            default:
                return ConfigManager.OtherWaypointColor;
        }
    }


    private static bool IsKindEnabled(CollectableKind kind, bool bears, bool pumpkins, bool others)
    {
        switch (kind)
        {
            case CollectableKind.Bear:
                return bears;
            case CollectableKind.Pumpkin:
                return pumpkins;
            default:
                return others;
        }
    }

    private static CollectableKind Classify(PunchCollectable collectable, CollectableProfile profile)
    {
        var apiName = profile.APIName ?? string.Empty;


        if (!string.IsNullOrEmpty(apiName) &&
            apiName.StartsWith(PumpkinApiPrefix, StringComparison.OrdinalIgnoreCase))
            return CollectableKind.Pumpkin;

        if (MatchesTokens(apiName, BearTokens) ||
            MatchesTokens(profile.Name, BearTokens) ||
            MatchesTokens(collectable.gameObject.name, BearTokens))
            return CollectableKind.Bear;

        if (MatchesTokens(apiName, PumpkinTokens) ||
            MatchesTokens(profile.Name, PumpkinTokens) ||
            MatchesTokens(collectable.gameObject.name, PumpkinTokens))
            return CollectableKind.Pumpkin;


        return CollectableKind.Other;
    }

    private static void MaybeLogProfiles(PunchCollectable[] collectables)
    {
        if (loggedProfilesThisScene || collectables == null || collectables.Length == 0)
            return;

        loggedProfilesThisScene = true;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var collectable in collectables)
        {
            if (collectable == null || !TryGetProfile(collectable, out var profile) || profile == null)
                continue;

            var apiName = profile.APIName ?? string.Empty;
            if (!seen.Add(apiName))
                continue;

            var kind = Classify(collectable, profile);
            CollectableWaypointsPlugin.Logger.LogInfo(
                $"[PunchCollectable] kind={kind} apiName='{apiName}' displayName='{profile.Name}' object='{collectable.name}' count={profile.Count}");
        }
    }

    private static bool TryGetProfile(PunchCollectable collectable, out CollectableProfile profile)
    {
        profile = null;
        try
        {
            profile = profileField?.GetValue(collectable) as CollectableProfile;
            return profile != null;
        }
        catch
        {
            return false;
        }
    }

    private static byte GetIndex(PunchCollectable collectable)
    {
        try
        {
            if (indexField != null)
                return (byte)indexField.GetValue(collectable);
        }
        catch
        {
        }

        return 0;
    }

    private static bool MatchesTokens(string value, string[] tokens)
    {
        if (string.IsNullOrEmpty(value) || tokens == null || tokens.Length == 0)
            return false;

        for (var i = 0; i < tokens.Length; i++)
            if (value.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

        return false;
    }

    private static bool IsTracked(Transform transform)
    {
        for (var i = 0; i < trackedPings.Count; i++)
            if (trackedPings[i].Transform == transform)
                return true;

        return false;
    }

    private static void ClearAllPings()
    {
        for (var i = 0; i < trackedPings.Count; i++)
            WaypointUtil.RemoveWaypoint(trackedPings[i].Transform);

        trackedPings.Clear();
    }

    public static void PunchCollectable_AddMeleeForce_Postfix(PunchCollectable __instance)
    {
        try
        {
            if (__instance == null)
                return;

            var target = __instance.transform;
            for (var i = trackedPings.Count - 1; i >= 0; i--)
            {
                if (trackedPings[i].Transform != target)
                    continue;

                WaypointUtil.RemoveWaypoint(target);
                trackedPings.RemoveAt(i);
            }
        }
        catch (Exception ex)
        {
            CollectableWaypointsPlugin.Logger.LogError("Error in PunchCollectable_AddMeleeForce_Postfix: " +
                                                       ex.Message);
        }
    }

    private enum CollectableKind
    {
        Bear,
        Pumpkin,
        Other
    }

    private struct TrackedPing
    {
        public Transform Transform;
        public CollectableKind Kind;
        public string ApiName;
        public byte Index;
    }
}
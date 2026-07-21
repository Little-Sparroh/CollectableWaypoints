using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Shared helpers for adding/removing colored waypoint pings.
/// </summary>
public static class WaypointUtil
{
    public static bool TryAddWaypoint(Transform target, Color color)
    {
        if (target == null || Highlighter.Instance == null)
            return false;

        Highlighter.Instance.AddWaypointPing(target, Highlighter.PingType.Waypoint, color);
        return true;
    }

    public static void RemoveWaypoint(Transform target)
    {
        if (target == null)
            return;

        Highlighter.Instance?.RemovePing(target);
    }

    /// <summary>
    /// Parses #RGB, #RRGGBB, #RRGGBBAA, or R,G,B[,A] (0-255 or 0-1).
    /// Falls back to <paramref name="fallback"/> on failure.
    /// </summary>
    public static Color ParseColor(string value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        value = value.Trim();

        if (value.StartsWith("#", StringComparison.Ordinal))
        {
            string hex = value.Substring(1);
            if (hex.Length == 3)
            {
                // #RGB -> #RRGGBB
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
            }

            if (hex.Length == 6 || hex.Length == 8)
            {
                try
                {
                    byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    byte a = 255;
                    if (hex.Length == 8)
                        a = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                }
                catch
                {
                    return fallback;
                }
            }
        }

        // R,G,B or R,G,B,A
        string[] parts = value.Split(',');
        if (parts.Length >= 3)
        {
            try
            {
                float r = ParseColorComponent(parts[0]);
                float g = ParseColorComponent(parts[1]);
                float b = ParseColorComponent(parts[2]);
                float a = parts.Length >= 4 ? ParseColorComponent(parts[3]) : 1f;
                return new Color(r, g, b, a);
            }
            catch
            {
                return fallback;
            }
        }

        return fallback;
    }

    private static float ParseColorComponent(string raw)
    {
        raw = raw.Trim();
        float value = float.Parse(raw, CultureInfo.InvariantCulture);
        // Allow 0-255 style values.
        if (value > 1f)
            value /= 255f;
        return Mathf.Clamp01(value);
    }
}

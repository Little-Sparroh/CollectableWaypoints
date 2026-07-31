using System;
using System.Globalization;
using UnityEngine;

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


    public static Color ParseColor(string value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        value = value.Trim();

        if (value.StartsWith("#", StringComparison.Ordinal))
        {
            var hex = value.Substring(1);
            if (hex.Length == 3) hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);

            if (hex.Length == 6 || hex.Length == 8)
                try
                {
                    var r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    var g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    var b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
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


        var parts = value.Split(',');
        if (parts.Length >= 3)
            try
            {
                var r = ParseColorComponent(parts[0]);
                var g = ParseColorComponent(parts[1]);
                var b = ParseColorComponent(parts[2]);
                var a = parts.Length >= 4 ? ParseColorComponent(parts[3]) : 1f;
                return new Color(r, g, b, a);
            }
            catch
            {
                return fallback;
            }

        return fallback;
    }

    private static float ParseColorComponent(string raw)
    {
        raw = raw.Trim();
        var value = float.Parse(raw, CultureInfo.InvariantCulture);

        if (value > 1f)
            value /= 255f;
        return Mathf.Clamp01(value);
    }
}
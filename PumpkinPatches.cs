/// <summary>
/// Thin facade kept for config wiring. Pumpkin waypoints are handled by
/// <see cref="PunchCollectableWaypointPatches"/>.
/// </summary>
public static class PumpkinWaypointPatches
{
    public static void ApplyConfig()
    {
        PunchCollectableWaypointPatches.ApplyPumpkinConfig();
    }
}

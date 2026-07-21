/// <summary>
/// Thin facade kept for config wiring. Bear waypoints are handled by
/// <see cref="PunchCollectableWaypointPatches"/>.
/// </summary>
public static class BearWaypointPatches
{
    public static void ApplyConfig()
    {
        PunchCollectableWaypointPatches.ApplyBearConfig();
    }
}

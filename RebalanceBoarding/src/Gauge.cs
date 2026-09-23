namespace Restitutor.RebalanceBoarding;

// Pure display rules for the boarding progress gauge (0.3.0), separated from the FairyGUI objects
// so they can be unit-tested. "total" is this ship's ShootProgress plus its target's - the exact
// sum BoatEntityBoardShoot.CheckProgressFull compares against the threshold.
internal static class Gauge
{
    internal const float FadeSeconds = 0.5f;

    internal static bool ShouldShow(int total, bool meleeBattling) => total > 0 && !meleeBattling;

    // Full bar == melee starts. The threshold is whatever the running game compares against:
    // 40 when the native byte is applied, 100 when it is not.
    internal static double Percent(int total, int threshold)
    {
        if (total <= 0 || threshold <= 0) return 0d;
        double pct = total * 100d / threshold;
        return pct > 100d ? 100d : pct;
    }
}

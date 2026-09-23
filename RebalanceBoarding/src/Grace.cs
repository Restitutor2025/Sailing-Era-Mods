namespace Restitutor.RebalanceBoarding;

// Pure rules for the boarding grace period (0.2.0). Original game: when the last boarding target leaves,
// BoatEntityBoardShoot.StopShooting sets ShootProgress = 0 immediately. With the grace period, the progress
// a ship had when contact broke is given back if it boards the SAME ship again within GraceSeconds.
public static class Grace
{
    public const float GraceSeconds = 3f;
    public const int MaxRecords = 64;   // safety bound; records are removed on use or when expired

    public readonly record struct Record(IntPtr Target, int Progress, float Time);

    /// Record only a real contact break: one target left, it is the one leaving, not a melee lock,
    /// and there is something to keep.
    public static bool ShouldRecord(int enemyCount, bool leavingIsOnlyTarget, bool meleeBattling, int progress) =>
        enemyCount == 1 && leavingIsOnlyTarget && !meleeBattling && progress > 0;

    /// Give the progress back only for the same target within the grace period (and never negative time).
    public static bool ShouldRestore(Record r, IntPtr newTarget, float now) =>
        r.Target != IntPtr.Zero && r.Target == newTarget && r.Progress > 0
        && now >= r.Time && now - r.Time <= GraceSeconds;

    public static bool Expired(Record r, float now) => now < r.Time || now - r.Time > GraceSeconds;
}

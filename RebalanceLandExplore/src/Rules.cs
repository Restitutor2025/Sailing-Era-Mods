namespace Restitutor.RebalanceLandExplore;

/// <summary>
/// Pure rules, no game types (unit-tested in tests/).
///
/// Original (GameAssembly R11, verified 2026-09-23):
///  - A land-explore option with a property check resolves by <c>ENConditionPropertyCheck.CheckResult</c>
///    (0x2599600): success = <c>GetRolePropByID(propertyId) &gt;= propertyValue</c>. No randomness.
///  - On failure the player may spend a Fate Coin. The chance shown on the coin button is
///    <c>CalculateProbability</c> (0x2599700), inlined into <c>UILandExploreEventView.ListRoleRender</c>
///    (0xB1DB20, at 0xB1E046..0xB1E0A9):
///        p = clamp(prop / required - 0.2, 0, 1);  ratio = (int)(p * 100)   // truncation
///    and <c>UILandExploreEventView.ListRoleRenderer</c> keeps the MAXIMUM over the listed navigators in
///    <c>UILandExploreEventModel.Ratio</c> (0xB1E70B), which the coin button then shows (0xB1E839).
///  - The roll is inlined in <c>UILandExploreEventCtrl.ShowRatioResult(int)</c> (0xB19AB0, at 0xB19BC8):
///        success = Random.Next(0, 100) &lt;= ratio
///    Random.Next(0,100) yields 0..99, so the real chance is ratio + 1 percentage points: the number on
///    the button is one point lower than the truth (shown 0% is really 1%). That is the bug fixed here.
///
/// This mod (user decisions 2026-09-23):
///  1. Cap: hard clamp of the coin chance at <see cref="MaxRatio"/> (anything above becomes exactly that).
///  2. Bug fix: the number shown is the real chance - the roll threshold is lowered by one.
///     0% therefore means a certain failure (the coin is still spent); left that way on purpose.
/// Nothing else changes: the deterministic check, which navigator the coin uses (the best one) and the
/// coin's own cost stay as they are.
/// </summary>
public static class Rules
{
    /// <summary>Highest coin chance in percent after this mod.</summary>
    public const int MaxRatio = 50;

    /// <summary>Original clamp of the formula (p &lt;= 1.0).</summary>
    public const int OriginalMax = 100;

    /// <summary>Chance shown on the coin button, and used for the roll.</summary>
    public static int Cap(int ratio) => ratio > MaxRatio ? MaxRatio : ratio;

    /// <summary>Threshold handed to the original roll <c>Random.Next(0,100) &lt;= t</c> so that the real
    /// chance equals the capped percentage.</summary>
    public static int RollThreshold(int ratio) => Cap(ratio) - 1;

    /// <summary>Real success chance (%) of the original roll for a threshold t.</summary>
    public static int ChanceOf(int threshold) =>
        threshold < 0 ? 0 : threshold >= OriginalMax - 1 ? 100 : threshold + 1;

    /// <summary>Real success chance (%) after this mod for a computed ratio.</summary>
    public static int Chance(int ratio) => ChanceOf(RollThreshold(ratio));

    /// <summary>Real success chance (%) of the original game for a computed ratio.</summary>
    public static int OriginalChance(int ratio) => ChanceOf(ratio);

    /// <summary>True when the stored value still has to be lowered (keeps the log quiet otherwise).</summary>
    public static bool NeedsCap(int ratio) => ratio > MaxRatio;
}

namespace Restitutor.RebalanceSaveSlots;

// Pure rules, no game types (unit-tested in tests/).
// Original: StorageHistoryManager keeps 10 entries (index 0,1 = auto save, 2..9 = manual).
// This mod: 2 auto + 99 manual = 101 entries (indexes 0..100). Save files are GameData\PlayerData\<index>.
public static class Rules
{
    public const int OriginalCount = 10;
    public const int AutoCount = 2;
    public const int ManualCount = 99;
    public const int Total = AutoCount + ManualCount;   // 101

    /// <summary>Indexes to append so the list becomes 0..Total-1 (list is assumed to hold 0..count-1).</summary>
    public static IEnumerable<int> Missing(int count)
    {
        for (int i = Math.Max(0, count); i < Total; i++) yield return i;
    }

    /// <summary>From entries parsed out of the saved history JSON (position = index), the positions to
    /// restore after the game loaded only the first <paramref name="loaded"/> of them.
    /// The game's own loader stops at 10; the JSON it writes back holds the whole list, so
    /// entries 10..Total-1 are still in the file and are appended in order.</summary>
    public static IEnumerable<int> ToRestore(int parsedCount, int loaded)
    {
        for (int i = Math.Max(0, loaded); i < Math.Min(parsedCount, Total); i++) yield return i;
    }

    public static bool IsAuto(int index) => index >= 0 && index < AutoCount;

    /// <summary>Rows the game never had. RenderStorageItem gives row i an enter animation delayed by i*0.1 s
    /// (aniReset then aniruchang); the game's Refresh shrinks the list to 10 and this mod grows it back,
    /// which removes rows 10+ from the stage mid-delay and FairyGUI stops their transitions in the hidden
    /// state. These rows are therefore shown at their end state right away.</summary>
    public static bool ShowInstantly(int index) => index >= OriginalCount && index < Total;

    /// <summary>Selection the game's Refresh drops: GList.selectedIndex = model index while the list holds
    /// only 10 rows clears the selection for index 10+. Returns the index to re-apply, or -1.</summary>
    public static int ReselectAfterGrow(int selected, int listCount) =>
        selected >= OriginalCount && selected < listCount ? selected : -1;
}

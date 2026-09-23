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

    /// <summary>Rows per page (user: page-turn feel; 101 rows = 21 pages, the last one holds row 100 only).
    /// Pages start at row 0: page 1 = auto 0,1 + manual 1..3, page 2 = manual 4..8, …</summary>
    public const int Page = 5;

    public static int PageCount(int count) => count <= 0 ? 0 : (count + Page - 1) / Page;

    public static int PageOf(int row) => row <= 0 ? 0 : row / Page;

    /// <summary>First row of a page (clamped to the list).</summary>
    public static int PageStart(int page, int count)
    {
        if (count <= 0) return -1;
        int pages = PageCount(count);
        if (page < 0) page = 0;
        if (page > pages - 1) page = pages - 1;
        return page * Page;
    }

    /// <summary>Q (dir -1) / E (dir +1): first row of the previous / next page. -1 = already at the end.</summary>
    public static int PageStep(int current, int dir, int count)
    {
        if (count <= 0) return -1;
        int p = PageOf(Math.Min(current, count - 1)), np = p + dir;
        if (np < 0 || np > PageCount(count) - 1) return -1;
        return np * Page;
    }

    /// <summary>Row to focus when the save/load screen opens: the slot used in this run (save or load),
    /// else the game's own latest-save slot, else the first row.</summary>
    public static int FocusRow(int lastUsed, int latest, int count)
    {
        if (count <= 0) return -1;
        if (lastUsed >= 0 && lastUsed < count) return lastUsed;
        if (latest >= 0 && latest < count) return latest;
        return 0;
    }
}

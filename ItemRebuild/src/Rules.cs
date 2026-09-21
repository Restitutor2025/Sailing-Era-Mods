namespace Restitutor.ItemRebuild;

internal static class Rules
{
    internal static bool Target(int id) => id is >= 36000 and <= 36007 or >= 36009 and <= 36013;
    // Exact installed Item types 4 (souvenirs), 10, 35, 1000. Expedition rules remain separate.
    internal static bool Consumable(int id) => id is >= 100000 and <= 100041 or >= 42001 and <= 42008
        or 10158 or >= 10216 and <= 10221 or >= 12003 and <= 12006
        or 10059 or 10077 or 10108 or >= 40001 and <= 40038;
    internal static bool Stack(int id) => Target(id) || Consumable(id) || Book(id);
    // Installed type 72 only: skill books, excluding language books, maps and other books.
    internal static bool SkillBook(int id) => id is 10034 or 10098 or >= 71001 and <= 71100 or >= 71146 and <= 71151;
    // 0.1.18: installed Item type 73 (book_language, 언어 서적) rows whose SkillBook-table kind is 2
    // (language). 71131-71145 are also type 73 but teach skills 116-120; user decision: excluded.
    // Native OnClickBtnBook learns languages via InnerAddLanguage and then deletes the book by GUID
    // (same InnerRemoveItemByGuid call as skill books), so the 0.1.16 real-quantity stack applies unchanged.
    internal static bool LanguageBook(int id) => id is >= 71101 and <= 71130;
    internal static bool Book(int id) => SkillBook(id) || LanguageBook(id);
    internal static bool GroupedRecord(int id) => Equipment(id) || Book(id);
    internal static bool Equipment(int id) => equipment.Contains(id) || clothes.Contains(id);
    // 0.1.17: installed Item type 33 (의상-장비), 68 rows, except the nine trade statistics
    // notebooks 33062-33070 (user decision: their function is unknown). Includes 10111 (구귀법).
    // Same display-only policy as type 31/32/34: unworn equal-attribute records share a bag slot;
    // GUIDs, equipment records and saved rows are unchanged.
    static readonly HashSet<int> clothes=new() {
        10111,33001,33002,33005,33006,33007,33008,33009,33010,33013,33014,33015,33023,33024,33025,33026,33027,33029,33030,33031,33032,33033,33035,33043,33044,
        33045,33046,33047,33048,33049,33050,33051,33052,33053,33054,33055,33056,33057,33058,33059,33060,33061,33071,33072,33073,33074,34001,34002,34006,34007,
        34009,34010,34011,34014,34026,34033,34038,34039,34042
    };
    static readonly HashSet<int> equipment=new() {
        10120,10142,10143,10157,10175,12028,1314029,31000,31001,31002,31003,31004,31005,31006,31007,31008,31009,31010,31011,31012,31013,31014,31015,31016,31017,31018,31019,31020,31021,31022,31023,34000,34018,34019,34020,34021,34022,34023,34024,34027,34031,34032,34034,34044,34046,
        32000,32001,32002,32003,32004,32005,32006,32007,32008,32009,32010,32011,32012,32013,32014,32015,32016,34025,
        10215,33000,33003,33004,33011,33012,33016,33017,33018,33019,33020,33021,33022,33028,33034,33036,33037,33038,33039,33040,33041,33042,34003,34004,34005,34008,34012,34015,34017,34028,34029,34030,34035,34036,34037,34040,34041,34043,34045
    };
    // 0.1.19: unlock items (user decision 2026-09-21).
    // Cabin blueprints 11001-11010/11036/11037 and 11000 (cabin expansion) unlock by being held
    // (GameConditionTpl ShipCabin_Condition_* "보유: 선실 설계도"). Rows stay saved; the bag hides them,
    // they take no logical capacity, and shops hide/refuse them while one is held.
    internal static bool CabinUnlock(int id) => id is >= 11000 and <= 11010 or 11036 or 11037;
    // Route charts (Item type 13, PrefabLane.lineMap). Native bag use (UIBagCtrl.UseSailLineDrawing)
    // deletes the item and records PlayerLaneDB.UnlockPrefabLane. Purchases are auto-used; shops
    // hide/refuse a chart whose lane is unlocked. 12007/12039-12042 excluded (possible quest items).
    // Purchasable route charts. 0.1.21: no longer auto-used; only hidden/refused in shops once held or unlocked.
    internal static bool AutoRoute(int id) => id is >= 130001 and <= 130056;
    internal static bool UnlockItem(int id) => CabinUnlock(id) || AutoRoute(id);
    internal static int Add(int current, int added)
    {
        if (current < 0 || added < 0) throw new ArgumentOutOfRangeException();
        return checked(current + added);
    }
    internal static int Select(int requested, int stock, float weight, float available)
    {
        int n = Math.Clamp(requested, 0, Math.Clamp(stock,0,3));
        if (weight > 0) n = Math.Min(n, (int)Math.Clamp(Math.Floor((available + 0.0001) / weight), 0, int.MaxValue));
        return n;
    }
}

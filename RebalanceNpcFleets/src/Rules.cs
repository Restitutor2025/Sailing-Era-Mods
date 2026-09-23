namespace Restitutor.RebalanceNpcFleets;

// Pure rules, no game types (unit-tested in tests/). User decisions 2026-09-23 (handoff/BIG_FUCHUAN_REMOVAL.md):
// - 대형 복선 (300) leaves the NPC shipyard purchase list (GameConst NPC_BUY_SHIP_LIST).
// - NPC fleets with a large Fu ship: pirates get Chinese warships (flagship 간증선, escorts 해창선 / 개랑선
//   alternating), trade fleets get 복선. NPC template copies (3xxx) are used, like the original fleets.
// - No difficulty compensation (user).
public static class Rules
{
    public const string BuyListKey = "NPC_BUY_SHIP_LIST";
    public const int BannedBuy = 300;

    // NPC template copies (same stats as 290/291/293/294, hidden from the knowledge book).
    public const int Fu = 3290, Haicang = 3291, Ganzeng = 3293, Kailang = 3294;

    public sealed record Team(int Tid, string Name, bool Pirate, int Flag, int[] Others, int NewFlag, int[] NewOthers);

    // Every NpcShipTeam that some Npc uses and that has 300 / 2006 / 3300 / 306 / 307 (table census).
    // Changed only when the loaded row still equals the original (another mod or the City Editor may have edited it).
    public static readonly Team[] Teams =
    {
        new(9405011, "연안 해적 함대",   true,  3300, new[]{3290,3290,3290,3290}, Ganzeng, new[]{3290,3290,3290,3290}),
        new(9504031, "연안 해적 선단",   true,  3300, new[]{3300,3300},           Ganzeng, new[]{Haicang,Kailang}),
        new(9505011, "남해 반군 함대",   true,  3300, new[]{3300,3300,3310,3310}, Ganzeng, new[]{Haicang,Kailang,3310,3310}),
        new(109979,  "해적 두목의 함대", true,  2006, new[]{2006,3290,3290,3280}, Ganzeng, new[]{Haicang,3290,3290,3280}),
        new(1300501, "떠돌이 해적",      true,  3300, new[]{3290},                Ganzeng, new[]{3290}),
        new(1010089, "진보회 함대",      false, 3300, new[]{3300,3290,3290,3290}, Fu,      new[]{Fu,3290,3290,3290}),
        new(1010098, "서공 선단",        false, 306,  new[]{307,3290,3290,3290},  Fu,      new[]{Fu,3290,3290,3290}),
    };

    public enum Result { Changed, AlreadyChanged, Differs }

    public static Result Check(Team t, int flag, IReadOnlyList<int> others)
    {
        if (flag == t.NewFlag && others.SequenceEqual(t.NewOthers)) return Result.AlreadyChanged;
        if (flag == t.Flag && others.SequenceEqual(t.Others)) return Result.Changed;
        return Result.Differs;
    }

    /// <summary>The purchase list without 300 ('|'-separated ids). Null when nothing changes.
    /// Tokens are kept as they are (order, spacing); only exact "300" tokens are dropped.
    /// Null as well when removing would empty the list (the game picks a random index from it).</summary>
    public static string? BuyListWithout(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var parts = value.Split('|');
        var keep = parts.Where(p => p.Trim() != BannedBuy.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        if (keep.Length == parts.Length || keep.Length == 0) return null;
        return string.Join("|", keep);
    }
}

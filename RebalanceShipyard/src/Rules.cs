namespace Restitutor.RebalanceShipyard;

// Pure rule, no game types (unit-tested in tests/).
// The shipyard "Buy" stock is refilled over time from WorldPortBuildShipData.GetBuyShipList,
// which keeps ships whose Technical <= the port's technology. We keep only Technical <= MaxTechnical,
// so city development never unlocks better ships for purchase. Player-ordered builds are a
// different path and are not touched.
public static class Rules
{
    public const int MaxTechnical = 0;

    /// <summary>Ships the Buy stock may contain. <paramref name="technical"/> returns null when the
    /// ship template is unknown; such ids are kept (we never remove what we cannot judge).
    /// Returns null when nothing changes, and also when filtering would empty the list: the game
    /// picks a random index from this list and throws on an empty one.</summary>
    public static List<int>? Filter(IReadOnlyList<int> ids, Func<int, int?> technical)
    {
        if (ids.Count == 0) return null;
        var keep = new List<int>(ids.Count);
        foreach (var id in ids)
        {
            var t = technical(id);
            if (t == null || t.Value <= MaxTechnical) keep.Add(id);
        }
        if (keep.Count == ids.Count || keep.Count == 0) return null;
        return keep;
    }
}

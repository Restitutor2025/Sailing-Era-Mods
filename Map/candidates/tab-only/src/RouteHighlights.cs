namespace Restitutor.Map;

// Presentation only: project the native BestLineList onto existing map segments.
// UIMapLineCtrl.SelectSailLine (0x5EB160) matches unordered node pairs and sets
// Order on the first selection; repeated/reversed traversal keeps that direction.
internal sealed class RouteHighlights
{
    private readonly Dictionary<(int lo, int hi), int> firstFrom = new();

    internal void AddEdge(int from, int to)
    {
        if (from == to) return;
        firstFrom.TryAdd((Math.Min(from, to), Math.Max(from, to)), from);
    }

    internal bool TryDirection(int start, int end, out bool forward)
    {
        bool selected = firstFrom.TryGetValue((Math.Min(start, end), Math.Max(start, end)), out int from);
        forward = selected && from == start;
        return selected;
    }
}

namespace Restitutor.TabCharacters;

// Page position has no dependency on the selected character.
internal sealed class PageWindow
{
    internal const int Size = 10;
    internal int Start { get; private set; }
    internal int Count { get; private set; }
    internal int VisibleCount => Math.Min(Size, Count - Start);

    internal void Reconcile(int count)
    {
        Count = Math.Max(0, count);
        int last = Count == 0 ? 0 : ((Count - 1) / Size) * Size;
        Start = Math.Min(Start, last);
    }

    internal bool Move(int direction)
    {
        int old = Start;
        if (direction < 0) Start = Math.Max(0, Start - Size);
        else if (direction > 0 && (long)Start + Size < Count) Start += Size;
        return Start != old;
    }

    internal int GlobalIndex(int slot) => slot >= 0 && slot < VisibleCount ? Start + slot : -1;
}

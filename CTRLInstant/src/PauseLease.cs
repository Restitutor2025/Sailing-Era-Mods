namespace Restitutor.CTRLInstant;

// A pause belongs to the exact native list that issued it, not merely an integer ID.
internal sealed class PauseLease
{
    internal int Token { get; private set; }
    internal nint List { get; private set; }
    internal bool Active => Token > 0;
    internal void Take(int token, nint list)
    {
        if (Active || token <= 0 || list == 0) throw new InvalidOperationException("Invalid pause ownership");
        Token = token;
        List = list;
    }
    internal void Forget() { Token = 0; List = 0; }
    internal void Release(nint currentList, Func<int, bool> contains, Action<int> resume)
    {
        int token = Token;
        bool same = Active && List == currentList;
        Forget(); // Native notifications can reenter cleanup.
        if (same && contains(token)) resume(token);
    }
}

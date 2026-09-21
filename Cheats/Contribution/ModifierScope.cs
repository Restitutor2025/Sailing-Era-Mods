namespace Restitutor.Cheats.Contribution;
// Independent of IL2CPP: each modifier substitution may be consumed only once.
internal sealed class ModifierScope : IDisposable {
    [ThreadStatic] internal static ModifierScope? Current;
    internal readonly IntPtr Owner;
    private bool point = true, rate = true;
    internal bool Complete => !point && !rate;
    internal ModifierScope(IntPtr owner) {
        if (Current != null || owner == IntPtr.Zero) throw new InvalidOperationException("Invalid or reentrant modifier scope");
        Owner=owner; Current=this;
    }
    internal bool Take(IntPtr owner, int property, bool isPoint) {
        if (owner != Owner || Current != this) return false;
        if (isPoint && property==133 && point) { point=false; return true; }
        if (!isPoint && property==21 && rate) { rate=false; return true; }
        return false;
    }
    public void Dispose() { if (Current == this) Current=null; }
}

namespace Restitutor.Cheats.Battle;

internal sealed class Selection {
    internal int Melee { get; private set; } = 1;
    internal int Cannon { get; private set; } = 1;
    internal void Reset() { Melee=Cannon=1; }
    internal void Select(bool melee,int value,bool active) {
        if(!active || value<1 || value>5)return;
        if(melee)Melee=value;else Cannon=value;
    }
}

// 1.1.0: checkboxes. Like the X1..X5 selection (also kept since 1.1.0) they survive the end of a sea battle and
// are cleared only by a session reset (save load / title / O off / panel error) — user decision 2026-09-23.
internal sealed class Toggles {
    internal bool Board { get; private set; } // instant boarding (flagship)
    internal bool Hull { get; private set; }  // hull HP lock (controlled ship = flagship)
    internal void Reset() { Board=Hull=false; }
    internal void Set(bool board,bool on,bool active) {
        if(!active)return;
        if(board)Board=on;else Hull=on;
    }
}

// Never compound our previous multiplier or overwrite a later owner's change.
internal sealed class StatLease {
    private readonly int original;
    private int applied;
    private bool lostOwnership;
    internal StatLease(int value) { original=applied=value; }
    internal int Apply(int current,int multiplier) {
        if(current!=applied)lostOwnership=true;
        if(lostOwnership)return current;
        applied=(int)Math.Clamp((long)original*multiplier,int.MinValue,int.MaxValue);
        return applied;
    }
    internal int Restore(int current)=>!lostOwnership && current==applied ? original : current;
}

// 1.1.0: progress to add so that own + enemy boarding progress passes the original threshold (> 100).
internal static class BoardRule {
    internal const int Threshold=100;
    internal static int Needed(int own,int enemy) {
        long need=(long)Threshold+1-own-enemy;
        return need<=0 ? 0 : (int)Math.Min(need,Threshold+1);
    }
}

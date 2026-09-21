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

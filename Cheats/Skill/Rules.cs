namespace Restitutor.Cheats.Skill;
internal static class Rules {
    // Levels per skill point. 1.1.0 (user): choices 1/2/5, default 5 (Rebalance Growth sets the game value to 5).
    // A saved 10 or 15 from 1.0.x is no longer a choice and falls back to the default.
    internal static readonly int[] Choices={1,2,5};
    internal const int Default=5;
    internal static bool Valid(int value)=>Array.IndexOf(Choices,value)>=0;
    internal static int Normalize(int value)=>Valid(value)?value:Default;
    // Original rule: leaving level x grants a point when x % interval == 0.
    // The multi-level button checks x = from .. from+count-1.
    internal static int PointsInRange(int from,int count,int interval) {
        if(interval<=0 || count<=0) return 0;
        int n=0;
        for(int x=from;x<from+count;x++) if(x%interval==0) n++;
        return n;
    }
}

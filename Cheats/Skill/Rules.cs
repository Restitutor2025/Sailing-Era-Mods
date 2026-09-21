namespace Restitutor.Cheats.Skill;
internal static class Rules {
    // Levels per skill point. 15 is the default the user specified for the original game.
    internal static readonly int[] Choices={5,10,15};
    internal const int Default=15;
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

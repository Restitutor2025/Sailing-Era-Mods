namespace Restitutor.Cheats.Bargirls;
internal static class Rules {
    internal static bool Valid(int value,int first,int second,int third)=>value>=0 && first>0 && second>first && third>second && value<=third;
    internal static int Stage(int value,int first,int second,int third)=>value>=third?3:value>=second?2:value>=first?1:0;
    internal static bool QuestBlocks(int role,int owner,bool started,bool finished)=>role>0 && owner==role && started && !finished;
    internal static bool TryRaise(int current,int completed,int first,int second,int third,out int target) {
        target=current;
        if(!Valid(current,first,second,third) || completed<0)return false;
        int stage=Stage(current,first,second,third);
        if(stage==3 || completed<stage)return false;
        target=stage==0?first:stage==1?second:third;
        return target>current;
    }
}

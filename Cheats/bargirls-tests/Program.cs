using Restitutor.Cheats.Bargirls;
int count=0;
void Check(bool condition){count++;if(!condition)throw new Exception("Failed check "+count);}
foreach(int current in new[]{0,1,49,50,51,249,250,251,649,650}) {
    int stage=current>=650?3:current>=250?2:current>=50?1:0;
    for(int completed=0;completed<=3;completed++) {
        bool allowed=Rules.TryRaise(current,completed,50,250,650,out int target);
        Check(allowed==(stage<3 && completed>=stage));
        if(allowed){Check(target>current);Check(Rules.Stage(target,50,250,650)==stage+1);}
        else Check(target==current);
    }
}
foreach(int value in new[]{-1,651,int.MaxValue})Check(!Rules.TryRaise(value,2,50,250,650,out _));
Check(!Rules.TryRaise(0,-1,50,250,650,out _));
Check(!Rules.TryRaise(0,0,0,250,650,out _));Check(!Rules.TryRaise(0,0,50,50,650,out _));
// A's accepted/unreported quest blocks A, but not B; completion restores eligibility.
foreach(int owner in new[]{301,302})foreach(bool started in new[]{false,true})foreach(bool finished in new[]{false,true}) {
    Check(Rules.QuestBlocks(301,owner,started,finished)==(owner==301 && started && !finished));
    Check(Rules.QuestBlocks(302,owner,started,finished)==(owner==302 && started && !finished));
}
Check(!Rules.QuestBlocks(0,0,true,false));
int valueNow=0;
foreach(int progress in new[]{0,1,2}){Check(Rules.TryRaise(valueNow,progress,50,250,650,out int next));valueNow=next;}
Check(valueNow==650);Check(!Rules.TryRaise(valueNow,2,50,250,650,out _));
Console.WriteLine($"PASS {count} step-up, cap, no-decrease, max-stage and per-NPC quest checks.");

using Restitutor.RebalanceGrowth;
int checks=0;
void Check(bool c,string n){checks++;if(!c)throw new Exception($"Check {checks}: {n}");}
// User values.
Check(Rules.SkillInterval==5&&Rules.MaxLevel==200&&Rules.StatMax==500,"user values");
Check(Rules.OriginalSkillInterval==15&&Rules.OriginalMaxLevel==99&&Rules.OriginalStatMax==99,"original values");
// Original RoleLevel rows (R11 chunk 138) reproduced by the rule: spot values from the dump.
var r2=Rules.RowFor(2); Check(r2==new Rules.Row(2,2,200,10,0,4f,2f),"row 2");
var r3=Rules.RowFor(3); Check(r3.SkillPoint==1&&r3.Exp==300,"row 3");
var r99=Rules.RowFor(99); Check(r99==new Rules.Row(99,99,9900,10,1,4f,2f),"row 99");
var r200=Rules.RowFor(200); Check(r200.Exp==20000&&r200.SkillPoint==0&&r200.HpRatio==4f&&r200.AttackRatio==2f,"row 200");
Check(Enumerable.Range(100,101).All(l=>Rules.RowFor(l).Tid==l&&Rules.RowFor(l).Level==l),"tid = level 100..200");
// 10-level button points (x = from .. from+count-1, x % interval == 0).
Check(Rules.PointsInRange(1,10,5)==2,"1..10 /5");
Check(Rules.PointsInRange(5,10,5)==2,"5..14 /5");
Check(Rules.PointsInRange(6,10,5)==2,"6..15 /5");
Check(Rules.PointsInRange(1,10,15)==0&&Rules.PointsInRange(10,10,15)==1,"interval 15");
Check(Rules.PointsInRange(1,10,1)==10&&Rules.PointsInRange(1,10,2)==5,"cheat 1 and 2");
Check(Rules.PointsInRange(195,5,5)==1&&Rules.PointsInRange(1,0,5)==0&&Rules.PointsInRange(1,10,0)==0,"edges");
// Next-point label: identical to the original wherever the original found a level.
int same=0;
foreach(int iv in new[]{1,2,5,10,15}) for(int lv=1;lv<=200;lv++) {
    int o=Rules.OriginalNextDiff(lv,iv),n=Rules.NextDiff(lv,200,iv);
    if(o>0){ Check(o==n,$"same where original found lv={lv} iv={iv}"); same++; }
    Check(Rules.FixNextLabel(lv,200,iv)==(o<0&&n>0),"fix condition");
}
Check(same>0,"compared");
Check(Rules.OriginalNextDiff(95,5)==1&&Rules.OriginalNextDiff(96,5)==-1&&Rules.NextDiff(96,200,5)==5,"96 -> 100 (5 levels)");
Check(Rules.NextDiff(199,200,5)==-1&&Rules.NextDiff(195,200,5)==1&&Rules.NextDiff(200,200,5)==-1,"top end");
Check(Rules.OriginalNextDiff(98,1)==1&&Rules.OriginalNextDiff(99,1)==-1&&Rules.FixNextLabel(99,200,1),"level 99 interval 1");
Check(!Rules.FixNextLabel(99,99,5)&&!Rules.FixNextLabel(96,99,5),"no change at the original cap");
// Tab tip.
Check(!Rules.FixTip(98)&&Rules.FixTip(99)&&Rules.FixTip(200),"tip range");
Check(Rules.TipMaxIndex(99,200)==0&&Rules.TipMaxIndex(200,200)==1&&Rules.TipShowsExp(199,200)&&!Rules.TipShowsExp(200,200),"tip values");
// 0.2.0 luck: growth-included, capped at 99 (all heroes: base 20, upValue 1).
Check(Rules.Lucky(20,1,1)==20&&Rules.Lucky(20,50,1)==69&&Rules.Lucky(20,80,1)==99&&Rules.Lucky(20,200,1)==99,"luck growth and cap");
Check(Rules.Lucky(50,1,1)==50&&Rules.Lucky(120,1,1)==99&&Rules.CapLucky(150)==99&&Rules.CapLucky(30)==30,"stored above 99 capped");
Check(Rules.Lucky(20,0,1)==20,"level 0 guard");
// 0.2.0 HP/attack gain: original (x/100) vs new (x/250), float steps as in the native code.
Check(Rules.Gain(99,4f,100f)==8&&Rules.Gain(500,4f,100f)==24&&Rules.Gain(99,2f,100f)==4&&Rules.Gain(500,2f,100f)==12,"original gains");
Check(Rules.Gain(99,4f,250f)==6&&Rules.Gain(500,4f,250f)==12&&Rules.Gain(99,2f,250f)==3&&Rules.Gain(500,2f,250f)==6,"new gains");
Check(Rules.GainCorrection(99,4f)==-2&&Rules.GainCorrection(500,4f)==-12&&Rules.GainCorrection(0,4f)==0&&Rules.GainCorrection(10,0f)==0,"corrections");
Check(Rules.Gain(25,2f,100f)==2&&Rules.Gain(75,2f,100f)==4,"banker rounding 2.5->2, 3.5->4");
// 0.2.0 weight cap.
Check(Rules.CapWeight(33f)==30f&&Rules.CapWeight(166.7f)==30f&&Rules.CapWeight(12.3f)==12.3f&&Rules.CapWeight(30f)==30f,"weight cap");
// 0.3.0 cumulative growth (per-mille, user rates S 100 / A 77.5 / B 55 / C 32.5 / D 10 %).
Check(Rules.RatePerMille("S")==1000&&Rules.RatePerMille("A")==775&&Rules.RatePerMille("B")==550&&Rules.RatePerMille("C")==325&&Rules.RatePerMille("D")==100&&Rules.RatePerMille("?")==-1,"rates");
Check(Rules.Letter("role_growth__s",9)=="S"&&Rules.Letter("role_growth__d",1)=="D"&&Rules.Letter(null,3)=="B"&&Rules.Letter("",7)=="?","letters");
{int p;
 Check(Rules.Grow(10,0,1000,1,500,out p)==1&&p==0,"S one level +1");
 Check(Rules.Grow(10,0,1000,10,500,out p)==10&&p==0,"S ten levels +10");
 Check(Rules.Grow(10,0,100,9,500,out p)==0&&p==900,"D 9 levels 90%");
 Check(Rules.Grow(10,900,100,1,500,out p)==1&&p==0,"D 10th level +1");
 Check(Rules.Grow(10,0,775,4,500,out p)==3&&p==100,"A 4 levels 310% -> +3, 10%");
 Check(Rules.Grow(10,0,325,10,500,out p)==3&&p==250,"C 10 levels 325% -> +3, 25%");
 Check(Rules.Grow(498,500,1000,10,500,out p)==2&&p==0,"cap: stop at 500, progress dropped");
 Check(Rules.Grow(500,700,1000,1,500,out p)==0&&p==0,"at cap: nothing, progress dropped");
 Check(Rules.Grow(499,0,550,1,500,out p)==0&&p==550,"below cap keeps progress");
 Check(Rules.Grow(10,5000,100,1,500,out p)==1&&p==99,"stored progress clamped to 999 first");
 // Same total whether levelled one at a time or all at once.
 foreach(int rate in new[]{1000,775,550,325,100}) {
   int cur=20,pr=0,g1=0; for(int i=0;i<180;i++){int q; int g=Rules.Grow(cur+g1,pr,rate,1,500,out q); g1+=g; pr=q;}
   int pa; int ga=Rules.Grow(20,0,rate,180,500,out pa);
   Check(ga==g1&&pa==pr,$"stepwise == batch rate {rate}");
 }
}
// 0.3.0 slider plan. Steps: level 10 (partial 300 of 1000 already stored -> raw 700), 11, 12; cost = raw * 0.8.
var steps=new List<Rules.Step>{new(700,560),new(1100,880),new(1200,960)};
{var s=Rules.Plan(0,steps); Check(s==new Rules.Spend(0,0,0,0),"nothing");
 s=Rules.Plan(560,steps); Check(s.Levels==1&&s.RawToRole==700&&s.Pay==560&&s.PartialRaw==0,"exactly one level");
 s=Rules.Plan(1000,steps); Check(s.Levels==1&&s.PartialRaw==550&&s.RawToRole==700+550&&s.Pay==1000,"one level + 440 -> 550 raw");
 s=Rules.Plan(300,steps); Check(s.Levels==0&&s.PartialRaw==375&&s.RawToRole==375&&s.Pay==300,"partial only");
 s=Rules.Plan(559,steps); Check(s.Levels==0&&s.PartialRaw==698&&s.PartialRaw<700,"partial stays below the level");
 s=Rules.Plan(1,new List<Rules.Step>{new(10,1000)}); Check(s.Levels==0&&s.PartialRaw==0&&s.Pay==0,"buys nothing -> pays nothing");
 s=Rules.Plan(560+880+960,steps); Check(s.Levels==3&&s.RawToRole==3000&&s.Pay==2400,"all steps");
 s=Rules.Plan(99999,steps); Check(s.Levels==3&&s.Pay==2400&&s.PartialRaw==0,"no partial past the last level");
 // Role side (PlayerRoleData.InnerAddExp 0x679B60): stored 300 + RawToRole walks the rows 1000,1100,1200.
 int stored=300,lv=10; long give=Rules.Plan(1000,steps).RawToRole; long e=stored+give; int[] need={1000,1100,1200};
 while(lv-10<3 && e>=need[lv-10]){e-=need[lv-10];lv++;}
 Check(lv==11&&e==550,"InnerAddExp walk: level 11, 550 stored");
}
Check(Rules.ToMax(steps,true)==2400&&Rules.ToMax(steps,false)==-1,"to max");
Check(Rules.ClampChoice(5000,9000,2400)==2400&&Rules.ClampChoice(2000,9000,2400)==2000&&Rules.ClampChoice(5000,3000,-1)==3000&&Rules.ClampChoice(-5,10,-1)==0,"clamp only above max usable");
// 0.3.0 2nd round: slider end, preview text.
Check(Rules.SliderMax(9000,2400)==2400&&Rules.SliderMax(1000,2400)==1000&&Rules.SliderMax(1000,-1)==1000&&Rules.SliderMax(-5,-1)==0,"slider max");
Check(Rules.Preview(10,0,775,1,500)=="0%→77.5%","A one level");
Check(Rules.Preview(10,325,775,1,500)=="+1 · 32.5%→10%","A carry +1");
Check(Rules.Preview(10,0,1000,10,500)=="+10 · 0%→0%","S ten levels");
Check(Rules.Preview(10,550,550,0,500)=="누적 55%","no level chosen");
Check(Rules.Preview(500,300,1000,1,500)=="최대"&&Rules.Preview(499,0,1000,5,500)=="+1 · 최대","cap");
Console.WriteLine($"PASS {checks} rebalance growth checks: values, level rows, 10-level points, next-point label, tip, luck, hp/attack gain, weight, growth %, slider plan, preview.");

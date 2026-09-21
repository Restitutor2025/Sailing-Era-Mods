using Restitutor.Cheats.Character;
int checks=0;
void Check(bool condition,string name){checks++;if(!condition)throw new Exception($"Check {checks}: {name}");}
// Digits only.
foreach(var t in new[]{"","-1","+1","1.2","1,000"," 1","1 ","abc",null})Check(!Rules.TryTarget(t,0,100,out _,out _),"reject "+(t??"null"));
Check(Rules.TryTarget("0",0,100,out int v,out bool c)&&v==0&&!c,"0");
Check(Rules.TryTarget("007",0,100,out v,out c)&&v==7&&!c,"leading zeros");
Check(Rules.TryTarget("100",0,100,out v,out c)&&v==100&&!c,"max kept");
Check(Rules.TryTarget("101",0,100,out v,out c)&&v==100&&c,"above max clamps");
Check(Rules.TryTarget("99999999999999999999999",0,Rules.ValueMax,out v,out c)&&v==Rules.ValueMax&&c,"beyond int64 clamps");
Check(Rules.TryTarget("0",Rules.HpMaxMin,Rules.ValueMax,out v,out c)&&v==1&&c,"hp max floor 1");
Check(!Rules.TryTarget("5",10,5,out _,out _),"empty range refused");
Check(Rules.TryTarget("3",0,0,out v,out c)&&v==0&&c,"hp current with max 0");
// Headroom: value ceiling + another ceiling-sized addition stays inside Int32.
Check(int.MaxValue-(long)Rules.ValueMax>1_000_000_000,"ceiling leaves >1e9 headroom below int32");
Check(Rules.ValueMax.ToString().Length==Rules.Digits,"digits match ceiling");
// Input correction.
Check(Rules.ClampInput("1234567890",Rules.ValueMax)==Rules.ValueMax.ToString(),"input clamp");
Check(Rules.ClampInput("12",10)=="10","input clamp small");
Check(Rules.ClampInput("-5",10)=="5","minus removed");
Check(Rules.ClampInput("",10)=="","empty");
// Skill plan.
Check(Rules.SkillPlan(3,7)==(true,4),"raise via native");
Check(Rules.SkillPlan(7,3)==(false,-4),"lower direct");
Check(Rules.SkillPlan(5,5)==(false,0),"same");
// Layout grows with skills, empty list keeps one line.
Check(Rules.Height(0)==Rules.Height(1)&&Rules.Height(5)>Rules.Height(1),"height");
// Two-column layout (1.1.0).
Check(Rules.ColumnWidth*2+Rules.ColumnGap==Rules.PanelWidth,"columns fill panel");
Check(Rules.Entries(21)==31&&Rules.RowsPerColumn(21)==16,"31 entries -> 16 rows");
Check(Rules.Slot(0,21)==(0f,0f)&&Rules.Slot(15,21)==(0f,15*Rules.RowHeight),"left column");
Check(Rules.Slot(16,21)==(Rules.ColumnWidth+Rules.ColumnGap,0f)&&Rules.Slot(30,21)==(Rules.ColumnWidth+Rules.ColumnGap,14*Rules.RowHeight),"right column");
Check(Rules.RowsPerColumn(0)==6&&Rules.Slot(11,0).x>0,"no skills: 11 entries, 6 left");
Check(Rules.Height(21)==Rules.RowsTop+16*Rules.RowHeight+Rules.Footer,"height uses rows per column");
// Apply: absolute, one write, verify, no retry.
int value=10,writes=0;
Check(Rules.Apply(25,()=>value,x=>{writes++;value=x;})==25&&writes==1,"set");
Rules.Apply(25,()=>value,x=>{writes++;value=x;});Check(writes==1,"unchanged no write");
bool failed=false;try{Rules.Apply(40,()=>value,x=>{writes++;value=x+1;});}catch(InvalidOperationException){failed=true;}Check(failed&&writes==2,"mismatch no retry");
Console.WriteLine($"PASS {checks} character checks: digits-only input, per-row clamp, 999,999,999 ceiling, skill raise/lower plan, absolute set without retry, two-column slots.");

using Restitutor.Cheats.Exp;
int checks=0;
void Check(bool condition,string name){checks++;if(!condition)throw new Exception($"Check {checks}: {name}");}
// Rejected: empty, sign, decimal, separators, spaces.
foreach(var t in new[]{"","-1","+1","1.2","1,000"," 1","1 ","abc","-0"})Check(!Rules.TryTarget(t,out _),"reject "+t);
// Accepted and clamped.
Check(Rules.TryTarget("0",out int v)&&v==0,"0");
Check(Rules.TryTarget("0000000001",out v)&&v==1,"leading zeros");
Check(Rules.TryTarget("2147483647",out v)&&v==int.MaxValue,"max");
Check(Rules.TryTarget("2147483648",out v)&&v==int.MaxValue,"max+1 clamps");
Check(Rules.TryTarget("9999999999",out v)&&v==int.MaxValue,"10 digits clamps");
Check(Rules.TryTarget("99999999999999999999999",out v)&&v==int.MaxValue,"beyond int64 clamps");
Check(Rules.TryTarget("00002147483648",out v)&&v==int.MaxValue,"padded max+1 clamps");
// Input box correction.
Check(Rules.ClampInput("2147483648")=="2147483647","input clamp");
Check(Rules.ClampInput("99999999999")=="2147483647","input clamp long");
Check(Rules.ClampInput("2147483647")=="2147483647","input max kept");
Check(Rules.ClampInput("123")=="123","input kept");
Check(Rules.ClampInput("-5")=="5","minus removed");
Check(Rules.ClampInput("1,000")=="1000","separator removed");
Check(Rules.ClampInput("")=="","empty");
// Apply: absolute set, one write, verify, no retry.
long amount=5000000000;int calls=0;
bool Write(int x){calls++;amount=x;return true;}
Check(Rules.Apply(int.MaxValue,()=>amount,Write)==int.MaxValue&&calls==1,"lower int64 to max");
Rules.Apply(int.MaxValue,()=>amount,Write);Check(calls==1,"unchanged no write");
Check(Rules.Apply(0,()=>amount,Write)==0&&calls==2,"set 0");
bool failed=false;try{Rules.Apply(12,()=>amount,x=>{calls++;return false;});}catch(InvalidOperationException){failed=true;}Check(failed&&calls==3,"rejected no retry");
failed=false;try{Rules.Apply(12,()=>amount,x=>{calls++;amount=13;return true;});}catch(InvalidOperationException){failed=true;}Check(failed&&calls==4,"mismatch no retry");
failed=false;try{Rules.Apply(-1,()=>amount,Write);}catch(ArgumentOutOfRangeException){failed=true;}Check(failed&&calls==4,"negative target refused");
Console.WriteLine($"PASS {checks} exp checks: digits-only input, clamp to 2,147,483,647, absolute set, no retry.");

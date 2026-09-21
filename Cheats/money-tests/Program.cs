using Restitutor.Cheats.Money;
int checks=0;
void Check(bool condition){checks++;if(!condition)throw new Exception("Check "+checks);}
foreach(var text in new[]{"","-1","2147483648","99999999999999999999","1.2","1,000"," 1","+1"})Check(!Rules.TryTarget(text,out _));
foreach(var text in new[]{"0","1","2147483647","0000000001"})Check(Rules.TryTarget(text,out _));
Check(Rules.ClampInput("2147483648")=="2147483647");Check(Rules.ClampInput("999999999999999999999999")=="2147483647");Check(Rules.ClampInput("0000000001")=="0000000001");Check(Rules.ClampInput("")=="");
long balance=5000000000;int calls=0;
bool Write(int value){calls++;balance=value;return true;}
Check(Rules.Apply(0,()=>balance,Write)==0);Check(calls==1);
Check(Rules.Apply(int.MaxValue,()=>balance,Write)==int.MaxValue);Check(calls==2);
Rules.Apply(int.MaxValue,()=>balance,Write);Check(calls==2);
bool failed=false;try{Rules.Apply(12,()=>balance,v=>{calls++;return false;});}catch(InvalidOperationException){failed=true;}Check(failed&&calls==3);
failed=false;try{Rules.Apply(12,()=>balance,v=>{calls++;balance=13;return true;});}catch(InvalidOperationException){failed=true;}Check(failed&&calls==4&&balance==13);
failed=false;try{Rules.Apply(12,()=>balance,v=>{calls++;throw new Exception();});}catch(Exception){failed=true;}Check(failed&&calls==5);
Console.WriteLine($"PASS {checks} money checks: bounds, overflow, absolute decrease/increase, unchanged value, rejection/mismatch/exception without retry.");

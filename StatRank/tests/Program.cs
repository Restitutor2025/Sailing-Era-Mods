using Restitutor.StatRank;
int checks=0;
void Check(bool c,string n){checks++;if(!c)throw new Exception($"Check {checks}: {n}");}
// Table rows (RoleGrowthType success/perfect from r11 table chunk 136).
Check(string.Join("|",Rules.Row("S",80,30))=="S|31%|55.9%|13.1%|1.18","S");
Check(string.Join("|",Rules.Row("A",60,20))=="A|21%|48.2%|30.8%|0.90","A");
Check(string.Join("|",Rules.Row("B",40,10))=="B|11%|36.5%|52.5%|0.58","B");
Check(string.Join("|",Rules.Row("C",20,5))=="C|6%|19.7%|74.3%|0.32","C");
Check(string.Join("|",Rules.Row("D",1,1))=="D|2%|2%|96%|0.06","D");
Check(Rules.Chance(99)==1&&Rules.Chance(150)==1&&Rules.Chance(-5)==0,"chance clamp");
var o=Rules.Odds(80,30);Check(Math.Abs(o.plus0+o.plus1+o.plus2-1)<1e-12,"sums to 1");
Check(Rules.Letter("role_growth__s",1)=="S"&&Rules.Letter("role_growth__d",5)=="D","letter from code");
Check(Rules.Letter("",2)=="A"&&Rules.Letter(null,9)=="?","letter fallback");
// 0.2.0 Rebalance Growth rows.
Check(string.Join("|",Rules.GrowthRow("S",0,false))=="S|100%|0%","S growth");
Check(string.Join("|",Rules.GrowthRow("A",350,false))=="A|77.5%|35%","A growth");
Check(string.Join("|",Rules.GrowthRow("C",999,false))=="C|32.5%|99.9%","C growth");
Check(string.Join("|",Rules.GrowthRow("D",0,true))=="D|10%|최대"&&Rules.GrowthRow("B",5000,false)[2]=="99.9%"&&Rules.GrowthRow("?",0,false)[1]=="?","cap, clamp, unknown");
Check(Rules.GrowthHeader.Length==3,"header");
Console.WriteLine($"PASS {checks} stat rank checks: (rate+1)% rolls, perfect-then-success order, letters, growth rows.");

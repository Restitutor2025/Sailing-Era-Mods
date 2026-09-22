using Restitutor.Cheats.Skill;
int checks=0;
void Check(bool c,string n){checks++;if(!c)throw new Exception($"Check {checks}: {n}");}
// 1.1.0 (user): choices 1/2/5, default 5.
Check(Rules.Choices.SequenceEqual(new[]{1,2,5})&&Rules.Default==5,"choices/default");
Check(Rules.Valid(1)&&Rules.Valid(2)&&Rules.Valid(5)&&!Rules.Valid(10)&&!Rules.Valid(15)&&!Rules.Valid(0),"valid");
Check(Rules.Normalize(10)==5&&Rules.Normalize(15)==5&&Rules.Normalize(2)==2,"saved 1.0.x value falls back");
Check(Rules.PointsInRange(1,10,1)==10&&Rules.PointsInRange(1,10,2)==5&&Rules.PointsInRange(1,10,5)==2,"10-level points");
Console.WriteLine($"PASS {checks} cheats skill checks: choices, default, normalize, 10-level points.");

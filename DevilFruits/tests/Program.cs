using Restitutor.DevilFruits;
int checks=0;
void Check(bool c,string n){checks++;if(!c)throw new Exception($"Check {checks}: {n}");}
// Item ids / names (user: "(스탯명) 악마의 열매", ids 990001~990005, stat order = Stat Rank rows).
Check(Rules.ItemTid(0)==990001&&Rules.ItemTid(4)==990005,"ids");
Check(Rules.StatOf(990003)==2&&Rules.StatOf(990000)==-1&&Rules.StatOf(990006)==-1&&Rules.StatOf(10034)==-1,"stat of id");
Check(Rules.ItemName(0)=="신체 악마의 열매"&&Rules.ItemName(4)=="매력 악마의 열매","names");
Check(Rules.ItemDesc(3)=="먹은 항해사의 학식 성장 등급이 한 단계 오른다.","desc");
Check(Rules.Texts().Count()==Rules.Count*3+2&&Rules.Texts().Select(t=>t.key).Distinct().Count()==Rules.Count*3+2,"text keys unique");
// Save record -> steps. Real skill-book ids and other values are ignored.
var s=Rules.Steps(new[]{10034,990001,990001,990005,71101,990002});
Check(s.SequenceEqual(new[]{2,1,0,0,1}),"steps");
Check(Rules.Steps(Array.Empty<int>()).All(x=>x==0),"no record");
// Grade: 1=S..5=D, one step per fruit, never above S.
Check(Rules.Applied(4,1)==3&&Rules.Applied(5,4)==1&&Rules.Applied(2,3)==1&&Rules.Applied(3,0)==3&&Rules.Applied(3,-2)==3,"applied");
Check(Rules.IsTop(1)&&!Rules.IsTop(2),"top");
Check(Rules.Letter("role_growth__b",3)=="B"&&Rules.Letter(null,1)=="S"&&Rules.Letter("",5)=="D"&&Rules.Letter(null,7)=="?","letters");
Console.WriteLine($"PASS {checks} devil fruit checks: ids, texts, save record steps, grade clamp, letters.");

using Restitutor.Contribution;
using System.Text.Json;
int checks=0;
void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
var benefits=new[]{new Benefit(1,0,100,"trade"),new Benefit(2,0,200,"guild")};
for(int before=0;before<=300;before+=25)for(int after=0;after<=300;after+=25){
 var j=new Journal();j.Changed(218,before,after,benefits,new HashSet<int>());
 var expected=benefits.Count(b=>after>before&&after>=b.Required);
 Check((j.Ports.GetValueOrDefault(218)?.Benefits.Count??0)==expected,$"cross {before}->{after}");
}
var journal=new Journal();journal.Changed(218,50,200,benefits,new HashSet<int>{1});
Check(journal.Ports[218].Benefits.Keys.SequenceEqual(new[]{2}),"already open skipped");
journal.Changed(218,200,0,benefits,new HashSet<int>());
Check(journal.Ports[218].Benefits.Count==1,"fall retains earned queued grant");
journal.Changed(218,0,200,benefits,new HashSet<int>());
Check(journal.Ports[218].Benefits.Count==2,"repeated rise deduplicated");
var old=JsonSerializer.Deserialize<PendingPort>("""
{"Port":218,"Contribution":50,"Benefits":{"1":{"Id":1,"Original":true,"Target":false},"2":{"Id":2,"Original":false,"Target":true},"3":{"Id":3,"Original":true,"Target":true},"4":{"Id":99,"Original":false,"Target":true}},"Revoked":[1]}
""")!;
var migrated=Rules.GrantsOnly(old)!;
Check(migrated.Benefits.Keys.SequenceEqual(new[]{2}),"legacy only new grants migrate");
Check(old.Benefits.Count==4,"migration preserves source");
migrated.Benefits[2].Target=false;
Check(old.Benefits[2].Target,"migration cloned entries");
old.Benefits.Remove(2);Check(Rules.GrantsOnly(old)==null,"decline-only legacy discarded");
var repair=new Journal();
repair.Changed(214,500,501,benefits,new HashSet<int>());
Check(repair.Ports[214].Benefits.Count==2,"next increase repairs both missed thresholds");
var jump=new Journal();jump.Changed(214,100,500,benefits,new HashSet<int>());
Check(jump.Ports[214].Benefits.Count==2,"100 to 500 also repairs missing threshold at initial value");
var stable=new Journal();stable.Changed(214,500,500,benefits,new HashSet<int>());
stable.Changed(214,500,400,benefits,new HashSet<int>());
Check(stable.Ports.Count==0,"same and decrease do not repair");
var discovery=new Journal();
discovery.ObserveHundred(214,0,99);Check(discovery.Ports.Count==0,"below 100 does not discover");
discovery.ObserveHundred(214,99,100);Check(discovery.Ports[214].RevealGoods,"100 inclusive queues discovery without benefits");
discovery.Ports.Clear();discovery.ObserveHundred(214,100,20);discovery.ObserveHundred(214,20,100);
Check(discovery.Ports.Count==0,"decline then recross never repeats");
discovery.ObserveHundred(215,0,500);Check(discovery.Ports[215].RevealGoods,"jump over 100 queues once per city");
var existing=new Journal();existing.ObserveHundred(214,500,10);existing.ObserveHundred(214,10,100);
Check(existing.Ports.Count==0,"existing >=100 before decline is remembered");
var pendingDiscovery=new PendingPort {Port=214,RevealGoods=true,Lines=new(){"grant"},UnknownGoods=new(){{101,"cargo"}}};
var cloned=Rules.GrantsOnly(pendingDiscovery)!;
Check(cloned.RevealGoods && cloned.Benefits.Count==0,"discovery-only survives filtering");
cloned.Lines.Clear();cloned.UnknownGoods.Clear();
Check(pendingDiscovery.Lines.Count==1 && pendingDiscovery.UnknownGoods.Count==1,"deferred notice and snapshot are deep copied");
var restored=JsonSerializer.Deserialize<PendingPort>(JsonSerializer.Serialize(pendingDiscovery))!;
Check(restored.RevealGoods && restored.Lines.Single()=="grant" && restored.UnknownGoods[101]=="cargo","pending discovery round trip");
var history=JsonSerializer.Deserialize<HashSet<int>>(JsonSerializer.Serialize(discovery.ReachedHundred))!;
var loaded=new Journal();loaded.ReachedHundred.UnionWith(history);loaded.ObserveHundred(214,50,100);
Check(loaded.Ports.Count==0,"persisted threshold prevents repeat after reload");
discovery.Clear();Check(discovery.ReachedHundred.Count==0,"new player resets threshold history");
var noticeSource = new List<Notice> {
    new() {City="리스본", Lines=new(){"해금 · 세금 면제 자격", "교역품 확인 · 후추", "실패 안내"}},
    new() {City="런던", Lines=new(){"교역품 확인 · 상아", "해금 · 추가 투자 가능", "교역품 확인 · 후추"}}
};
string beforeNotices = JsonSerializer.Serialize(noticeSource);
var displayRows = NoticePresentation.Rows(noticeSource);
Check(displayRows.Select(r=>r.Kind).SequenceEqual(new[]{0,0,0,1,1,2}),"all cargo precedes all unlocks across cities");
Check(displayRows.Take(3).Select(r=>r.Line).SequenceEqual(new[]{"교역품 확인 · 후추","교역품 확인 · 상아","교역품 확인 · 후추"}),"stable cargo order including duplicates");
Check(displayRows[0].City=="리스본" && displayRows[1].City=="런던" && displayRows[3].City=="리스본","city ownership retained after grouping");
Check(displayRows[3].Line=="해금 · 세금 면제 자격" && displayRows[4].Line=="해금 · 추가 투자 가능","unlock order retained");
Check(displayRows.Last().Line=="실패 안내","other notices retained");
Check(beforeNotices==JsonSerializer.Serialize(noticeSource),"presentation does not change saved notices");
Check(NoticePresentation.Rows(Array.Empty<Notice>()).Length==0,"empty notices render empty");
Console.WriteLine($"PASS {checks} grant/discovery/presentation assertions");

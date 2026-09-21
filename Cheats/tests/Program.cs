using Restitutor.Cheats.Contribution;
using Restitutor.Cheats.Speed;
int checks=0;
void Check(bool value) { checks++; if (!value) throw new Exception("Failed check " + checks); }
foreach (var v in new[]{"0","1","500","999","1000"}) Check(Rules.TryTarget(v,out _));
foreach (var v in new[]{""," ","-1","1001","2147483648","9999999999","5.0","1e2","+5","５００"," 500","500 "}) Check(!Rules.TryTarget(v,out _));
// The actual constant assigned to GTextInput.restrict is a Regex, not a Flash range.
var restriction = new System.Text.RegularExpressions.Regex(Rules.InputRestriction);
foreach (char c in "0123456789") Check(restriction.IsMatch(c.ToString()));
foreach (char c in "abc한글５-+. ") Check(!restriction.IsMatch(c.ToString()));
foreach (string digits in new[]{"500","0","1000","1234567890"}) {
 string accepted=string.Concat(restriction.Matches(digits).Select(m=>m.Value));
 Check(accepted==digits);
}
Check(!new System.Text.RegularExpressions.Regex("0-9").IsMatch("500"));
var owner = new IntPtr(123);
using (var s=new ModifierScope(owner)) {
 Check(!s.Take(new IntPtr(124),133,true)); Check(!s.Take(owner,20,true));
 Check(s.Take(owner,133,true)); Check(!s.Take(owner,133,true)); Check(!s.Complete);
 Check(!s.Take(owner,21,true)); Check(s.Take(owner,21,false)); Check(s.Complete); Check(!s.Take(owner,21,false));
 Check(!Task.Run(()=>ModifierScope.Current != null).Result);
 bool rejected=false;try{using var nested=new ModifierScope(owner);}catch(InvalidOperationException){rejected=true;} Check(rejected);
}
Check(ModifierScope.Current==null);
try { using var scope=new ModifierScope(owner); throw new Exception("test"); }catch{}
Check(ModifierScope.Current==null);
foreach(var value in new[]{"", "0", "999", "1000", "0000000999", "00001000"}) Check(Rules.ClampInput(value)==value);
foreach(var value in new[]{"1001", "10000", "9999999999", "2147483648", "00001001"}) Check(Rules.ClampInput(value)=="1000");
Check(Rules.ClampInput(Rules.ClampInput("9999999999"))=="1000");
SailingChecks.Run(Check);
var menu = new Il2CppFairyGUI.GObject();
var map = new Il2CppFairyGUI.GObject { parent=menu };
EntryPoint.MapContent=map;
Check(UiPresence.IsDisplayed(true,map));
Check(SailingSpeed.CanUse());
Check(SailingSpeed.Unavailable=="");
// Cached map remains open, but its menu is hidden after closing Tab.
menu.internalVisible2=false;
Check(!UiPresence.IsDisplayed(true,map)); Check(SailingSpeed.CanUse());
SailingSpeed.Select(3); Check(SailingSpeed.Multiplier==3);
for(int n=0;n<3;n++) {
    menu.internalVisible2=true; SailingSpeed.Update(); Check(SailingSpeed.CanUse()); Check(SailingSpeed.Multiplier==3);
    menu.internalVisible2=false; SailingSpeed.Update(); Check(SailingSpeed.CanUse()); Check(SailingSpeed.Multiplier==3);
}
menu.internalVisible2=true; map.onStage=false;
Check(!UiPresence.IsDisplayed(true,map)); Check(SailingSpeed.CanUse());
map.onStage=true; menu.internalVisible=false;
Check(!UiPresence.IsDisplayed(true,map));
menu.internalVisible=true; map.isDisposed=true; Check(!UiPresence.IsDisplayed(true,map));
map.isDisposed=false; Check(!UiPresence.IsDisplayed(false,map)); Check(!UiPresence.IsDisplayed(true,null));
EntryPoint.MapContent=null;
Console.WriteLine($"PASS {checks} checks: contribution regression; sailing gates, player-team isolation, X1..X5, exception restoration, foreign-write preservation, reset.");


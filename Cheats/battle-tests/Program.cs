using Restitutor.Cheats.Battle;
using Restitutor.Cheats.Interface;
using Il2CppClient.Manager;
using Il2CppCore.SceneSystem;
using Il2CppClient.Battle;
using Il2CppClient.WorldLogic.Entity.Component.Boat.BoatAttack;
using Il2Cpp;
int checks=0;
void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
void Ready(){BattleRuntime.ResetSession();Host.Enabled=EntryPoint.Loaded=true;Host.Player=new();PlayerDataManager.Instance.Data=Host.Player;SceneManager.Instance=new();BattleRuntime.Update();}
void ResetCheck(string why){Check(BattleRuntime.Choice.Melee==1 && BattleRuntime.Choice.Cannon==1,why);}
Ready();Check(BattleRuntime.Active,"battle active");
var ctrl=new MeleeBattleController();var hero=new CombatUnit();var enemy=new CombatUnit{IsPlayerUnit=false};var pawn=new CombatUnit{IsPawn=true};ctrl._Battle.PlayerUnits.AddRange(new[]{hero,enemy,pawn});
BattleRuntime.Select(true,3);BattleRuntime.Select(false,4);BattleRuntime.Bind(ctrl);
Check(hero.Attack==30 && hero.Craft==60 && hero.Perception==90 && hero.Physical==120,"all four combat stats");
Check(enemy.Attack==10 && pawn.Attack==10,"enemy and pawn excluded");
Check(hero.Hp==70 && hero.MaxHp==100 && hero.HpBeforeMelee==70,"HP/settlement untouched");
for(int i=0;i<30;i++)BattleRuntime.BeforeMelee(ctrl);
Check(hero.Attack==30,"no frame compounding");
BattleRuntime.Select(true,5);Check(hero.Attack==50,"X3 to X5 uses baseline");
BattleRuntime.Select(true,1);Check(hero.Attack==10,"X1 restores stats");
BattleRuntime.Select(true,2);hero.Craft=99;BattleRuntime.BeforeMelee(ctrl);BattleRuntime.Select(true,4);
Check(hero.Craft==99 && hero.Attack==40,"preserve other owner's change");
var gun=new BoatEntityGun();var shot=new AttackInfo{gunDamageFactor=1.5f};BattleRuntime.Cannon(gun,shot);
Check(shot.gunDamageFactor==6,"cannon composes original factor");
for(int i=0;i<30;i++)BattleRuntime.Cannon(gun,shot);
Check(shot.gunDamageFactor==6,"one multiplier per salvo");
BattleRuntime.Select(false,2);BattleRuntime.Cannon(gun,shot);Check(shot.gunDamageFactor==6,"shot snapshots selection");
var other=new AttackInfo{attackGuid=2,sourceEntityGuid=124};BattleRuntime.Cannon(new BoatEntityGun{boatData=new(){Guid=124}},other);Check(other.gunDamageFactor==1,"other allied ship excluded");
var siege=new AttackInfo{attackGuid=3,isSiege=true};BattleRuntime.Cannon(gun,siege);Check(siege.gunDamageFactor==1,"siege excluded");
BattleRuntime.EndMelee(ctrl);Check(BattleRuntime.Choice.Melee==4 && BattleRuntime.Choice.Cannon==2,"boarding end keeps selections");
Check(hero.Attack==10 && hero.Craft==99 && shot.gunDamageFactor==6,"boarding end restores units, retains sea shots");
BattleRuntime.Reset();Check(BattleRuntime.Choice.Melee==4 && BattleRuntime.Choice.Cannon==2,"battle end keeps both selections (1.1.0)");Check(shot.gunDamageFactor==1.5f,"full end restores shot factor");Check(hero.Attack==10,"full end restores units");BattleRuntime.ResetSession();ResetCheck("session reset resets both");
Ready();BattleRuntime.Select(false,5);var pooled=new AttackInfo{attackGuid=10};BattleRuntime.Cannon(gun,pooled);
pooled.attackGuid=11;pooled.gunDamageFactor=2;BattleRuntime.Reset();Check(pooled.gunDamageFactor==2,"do not restore a reused attack");
Ready();BattleRuntime.Select(false,2);var p=new AttackInfo{attackGuid=20};BattleRuntime.Cannon(gun,p);p.attackGuid=21;p.gunDamageFactor=3;BattleRuntime.Cannon(gun,p);Check(p.gunDamageFactor==6,"pooled reuse fresh baseline");
BattleRuntime.Reset();Check(p.gunDamageFactor==3,"pooled new attack restores correctly");
Ready();var unrelated=new MeleeBattleController();unrelated._Battle.PlayerShip.ShipGuid=999;var u=new CombatUnit();unrelated._Battle.PlayerUnits.Add(u);BattleRuntime.Select(true,5);BattleRuntime.Bind(unrelated);Check(u.Attack==10,"non-target battle excluded");
foreach(var transition in new Action[]{()=>SceneManager.Instance.Scene.IsInBattle=false,()=>SceneManager.Instance.IsInOceanScene=false}) {
 Ready();BattleRuntime.Select(true,5);BattleRuntime.Select(false,5);transition();BattleRuntime.Update();Check(BattleRuntime.Choice.Melee==5 && BattleRuntime.Choice.Cannon==5,"transition keeps selection");Check(!BattleRuntime.Active,"transition hidden");
}
foreach(var suspend in new Action[]{()=>SceneManager.Instance.IsInLoadingOrStarting=true,()=>Host.Player=null,()=>PlayerDataManager.Instance.Data=new(),()=>EntryPoint.Loaded=false,()=>Host.Enabled=false,()=>SceneManager.Instance.Scene._focusTeam.isPlayer=false}) {
 Ready();BattleRuntime.Select(true,4);BattleRuntime.Select(false,5);suspend();
 for(int i=0;i<100;i++)BattleRuntime.Update();
 Check(BattleRuntime.Choice.Melee==4 && BattleRuntime.Choice.Cannon==5,"suspension never changes selections");Check(!BattleRuntime.Active,"suspended effects hidden");
}
Ready();BattleRuntime.Select(true,3);BattleRuntime.Select(false,3);SceneManager.Instance.Scene.Focus.Data.Guid=999;BattleRuntime.Update();Check(BattleRuntime.Choice.Cannon==3 && BattleRuntime.Choice.Melee==3,"focus switch retains selection");
BattleRuntime.Select(true,1);BattleRuntime.Select(false,1);
foreach(int n in new[]{0,-1,6,int.MaxValue}){BattleRuntime.Select(true,n);BattleRuntime.Select(false,n);ResetCheck("reject invalid selection");}
Ready();BattleRuntime.Select(false,1);var x1=new AttackInfo();BattleRuntime.Cannon(gun,x1);BattleRuntime.Select(false,5);BattleRuntime.Cannon(gun,x1);Check(x1.gunDamageFactor==1,"X1 shot remains X1");
var lease=new StatLease(int.MaxValue);Check(lease.Apply(int.MaxValue,5)==int.MaxValue,"overflow clamp");
Ready();BattleRuntime.Select(true,2);BattleRuntime.Select(false,4);
SceneManager.Instance.IsInLoadingOrStarting=true;BattleRuntime.Update();SceneManager.Instance.IsInLoadingOrStarting=false;BattleRuntime.Update();
Check(BattleRuntime.Active && BattleRuntime.Choice.Cannon==4,"transient loading resumes at selected factor");
var next=new MeleeBattleController();var nextHero=new CombatUnit();next._Battle.PlayerUnits.Add(nextHero);BattleRuntime.Bind(next);BattleRuntime.EndMelee(next);
Check(BattleRuntime.Choice.Cannon==4 && BattleRuntime.Choice.Melee==2,"sea-to-boarding-to-sea keeps both");
var nextBoarding=new MeleeBattleController();var laterHero=new CombatUnit();nextBoarding._Battle.PlayerUnits.Add(laterHero);BattleRuntime.Bind(nextBoarding);
Check(laterHero.Attack==20,"second boarding uses same choice");
SceneManager.Instance.Scene.IsInBattle=false;BattleRuntime.Update();Check(BattleRuntime.Choice.Melee==2 && BattleRuntime.Choice.Cannon==4 && laterHero.Attack==10,"confirmed sea end restores units, keeps both");
SceneManager.Instance.Scene.IsInBattle=true;BattleRuntime.Update();Check(BattleRuntime.Active && BattleRuntime.Choice.Melee==2 && BattleRuntime.Choice.Cannon==4,"next sea battle starts at the kept selection");
var again=new MeleeBattleController();var againHero=new CombatUnit();again._Battle.PlayerUnits.Add(againHero);BattleRuntime.Bind(again);Check(againHero.Attack==20,"kept melee multiplier applies in the next battle");
var againShot=new AttackInfo{attackGuid=77};BattleRuntime.Cannon(gun,againShot);Check(againShot.gunDamageFactor==4,"kept cannon multiplier applies in the next battle");
SceneManager.Instance.Scene.IsInBattle=false;BattleRuntime.Update();SceneManager.Instance.IsInOceanScene=false;BattleRuntime.Update();
int cur=SceneManager.Instance.CurCalls;for(int i=0;i<100;i++)BattleRuntime.Update();Check(SceneManager.Instance.CurCalls==cur,"kept X2/X4 outside the ocean: no per-frame lookups");
SceneManager.Instance.IsInOceanScene=true;
// ---- 1.1.0 checkboxes ----
Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityHitHandler hh;Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityBoardShoot bs,foe;
void Ship(){SceneManager.Instance.Scene.Focus=new();hh=new();bs=new();foe=new(){ShootProgress=30};bs.ShootProgress=20;bs.targetEnemy=foe;SceneManager.Instance.Scene.Focus._comps.AddRange(new object[]{new object(),hh,bs});GameLinks.Throw=false;}
void Fresh(){BattleRuntime.ResetSession();Ready();Ship();}
Fresh();Check(!BattleRuntime.Toggle.Board && !BattleRuntime.Toggle.Hull,"checkboxes start off");
Check(!hh.lockHealth && bs.BoardShootingBegin==null,"nothing touched while off");
// hull lock
BattleRuntime.SetToggle(false,true);Check(BattleRuntime.Toggle.Hull && hh.lockHealth,"hull lock applied immediately on click");
int searches=GameLinks.Searches;for(int i=0;i<100;i++)BattleRuntime.Update();Check(GameLinks.Searches==searches,"no per-frame component search once applied");
BattleRuntime.SetToggle(false,false);Check(!hh.lockHealth,"hull lock released on uncheck");
Fresh();hh.lockHealth=true;BattleRuntime.SetToggle(false,true);BattleRuntime.SetToggle(false,false);Check(hh.lockHealth,"pre-existing lock (game/debug) left as it was");
Fresh();BattleRuntime.SetToggle(false,true);hh.lockHealth=false;BattleRuntime.SetToggle(false,false);Check(!hh.lockHealth,"someone else's clear respected");
// battle end keeps the checkbox but releases the lock; next battle re-applies
Fresh();BattleRuntime.SetToggle(false,true);BattleRuntime.Select(false,3);
SceneManager.Instance.Scene.IsInBattle=false;BattleRuntime.Update();
Check(!hh.lockHealth,"battle end releases lock (no HP lock while sailing)");Check(BattleRuntime.Toggle.Hull,"checkbox kept after battle");Check(BattleRuntime.Choice.Cannon==3,"multiplier kept at battle end too");
SceneManager.Instance.Scene.IsInBattle=true;BattleRuntime.Update();Check(hh.lockHealth,"next battle re-applies lock automatically");
SceneManager.Instance.IsInOceanScene=false;BattleRuntime.Update();Check(!hh.lockHealth && BattleRuntime.Toggle.Hull,"leaving the ocean scene releases, keeps checkbox");
SceneManager.Instance.IsInOceanScene=true;
// transient suspension keeps the lock (not a battle boundary)
Fresh();BattleRuntime.SetToggle(false,true);SceneManager.Instance.IsInLoadingOrStarting=true;for(int i=0;i<10;i++)BattleRuntime.Update();Check(hh.lockHealth,"transient loading keeps lock");SceneManager.Instance.IsInLoadingOrStarting=false;
// session reset clears everything
BattleRuntime.SetToggle(true,true);BattleRuntime.ResetSession();Check(!hh.lockHealth && bs.BoardShootingBegin==null && !BattleRuntime.Toggle.Hull && !BattleRuntime.Toggle.Board,"session reset clears checkboxes and releases");
// inactive: clicks ignored
Fresh();SceneManager.Instance.Scene.IsInBattle=false;BattleRuntime.Update();BattleRuntime.SetToggle(false,true);BattleRuntime.SetToggle(true,true);
Check(!BattleRuntime.Toggle.Hull && !BattleRuntime.Toggle.Board && !hh.lockHealth,"clicks outside battle ignored");SceneManager.Instance.Scene.IsInBattle=true;
// missing handler: searched once, not every frame
Fresh();SceneManager.Instance.Scene.Focus._comps.Clear();BattleRuntime.SetToggle(false,true);searches=GameLinks.Searches;for(int i=0;i<100;i++)BattleRuntime.Update();Check(GameLinks.Searches==searches,"missing component searched once");
// instant boarding
Fresh();BattleRuntime.SetToggle(true,true);Check(bs.BoardShootingBegin!=null,"flagship delegate subscribed");
bs.BoardShootingBegin!();Check(bs.Calls==0,"delegate only records (no game call inside physics callback)");
BattleRuntime.Update();Check(bs.Added==51 && bs.ShootProgress+foe.ShootProgress==101,"adds exactly enough to pass 100 via original AddProgress");
BattleRuntime.Update();Check(bs.Calls==1,"one entry per event");
bs.ShootProgress=0;foe.ShootProgress=0;
var mc=new MeleeBattleController();BattleRuntime.Bind(mc);bs.BoardShootingBegin!();BattleRuntime.Update();Check(bs.Calls==1,"no entry while a boarding battle is bound");
BattleRuntime.EndMelee(mc);bs.BoardShootingBegin!();BattleRuntime.Update();Check(bs.Calls==2 && bs.Added==51+101,"re-entry after boarding ends (user decision: allowed)");
foe.ShootProgress=200;bs.BoardShootingBegin!();BattleRuntime.Update();Check(bs.Calls==2,"already past threshold: nothing added");
BattleRuntime.SetToggle(true,false);Check(bs.BoardShootingBegin==null,"uncheck unsubscribes");
bs.BoardShootingBegin=null;
Fresh();Action gameOwn=()=>{};bs.BoardShootingBegin=gameOwn;BattleRuntime.SetToggle(true,true);BattleRuntime.SetToggle(true,false);Check(bs.BoardShootingBegin==gameOwn,"other subscribers preserved");
Fresh();SceneManager.Instance.Scene.Focus.Data.IsPlayerFlagship=false;BattleRuntime.SetToggle(true,true);Check(bs.BoardShootingBegin==null,"non-flagship not subscribed");
searches=GameLinks.Searches;for(int i=0;i<50;i++)BattleRuntime.Update();Check(GameLinks.Searches==searches,"non-flagship checked once");
Fresh();BattleRuntime.SetToggle(true,true);SceneManager.Instance.Scene.IsInBattle=false;BattleRuntime.Update();Check(bs.BoardShootingBegin==null && BattleRuntime.Toggle.Board,"battle end unsubscribes, keeps checkbox");
SceneManager.Instance.Scene.IsInBattle=true;BattleRuntime.Update();Check(bs.BoardShootingBegin!=null,"next battle re-subscribes");
// failure isolation
Fresh();BattleRuntime.Select(false,4);GameLinks.Throw=true;BattleRuntime.SetToggle(false,true);
Check(BattleRuntime.Active && BattleRuntime.Choice.Cannon==4,"link failure does not stop multipliers");
searches=GameLinks.Searches;for(int i=0;i<50;i++)BattleRuntime.Update();Check(GameLinks.Searches==searches,"after a failure no retry this battle");
GameLinks.Throw=false;SceneManager.Instance.Scene.IsInBattle=false;BattleRuntime.Update();SceneManager.Instance.Scene.IsInBattle=true;BattleRuntime.Update();Check(hh.lockHealth,"next battle retries");
BattleRuntime.ResetSession();
Check(BoardRule.Needed(0,0)==101 && BoardRule.Needed(100,1)==0 && BoardRule.Needed(-5,0)==101 && BoardRule.Needed(int.MinValue,int.MinValue)==101,"threshold arithmetic");
BattleRuntime.ResetSession();Console.WriteLine($"PASS {checks} battle scope, restoration, reset, pooling and multiplier checks (managed stubs; no game execution).");

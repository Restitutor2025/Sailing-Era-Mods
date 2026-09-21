using Restitutor.Cheats.Battle;
using Restitutor.Cheats.Interface;
using Il2CppClient.Manager;
using Il2CppCore.SceneSystem;
using Il2CppClient.Battle;
using Il2CppClient.WorldLogic.Entity.Component.Boat.BoatAttack;
using Il2Cpp;
int checks=0;
void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
void Ready(){BattleRuntime.Reset();Host.Enabled=EntryPoint.Loaded=true;Host.Player=new();PlayerDataManager.Instance.Data=Host.Player;SceneManager.Instance=new();BattleRuntime.Update();}
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
BattleRuntime.Reset();ResetCheck("full battle end resets both");Check(shot.gunDamageFactor==1.5f,"full end restores shot factor");
Ready();BattleRuntime.Select(false,5);var pooled=new AttackInfo{attackGuid=10};BattleRuntime.Cannon(gun,pooled);
pooled.attackGuid=11;pooled.gunDamageFactor=2;BattleRuntime.Reset();Check(pooled.gunDamageFactor==2,"do not restore a reused attack");
Ready();BattleRuntime.Select(false,2);var p=new AttackInfo{attackGuid=20};BattleRuntime.Cannon(gun,p);p.attackGuid=21;p.gunDamageFactor=3;BattleRuntime.Cannon(gun,p);Check(p.gunDamageFactor==6,"pooled reuse fresh baseline");
BattleRuntime.Reset();Check(p.gunDamageFactor==3,"pooled new attack restores correctly");
Ready();var unrelated=new MeleeBattleController();unrelated._Battle.PlayerShip.ShipGuid=999;var u=new CombatUnit();unrelated._Battle.PlayerUnits.Add(u);BattleRuntime.Select(true,5);BattleRuntime.Bind(unrelated);Check(u.Attack==10,"non-target battle excluded");
foreach(var transition in new Action[]{()=>SceneManager.Instance.Scene.IsInBattle=false,()=>SceneManager.Instance.IsInOceanScene=false}) {
 Ready();BattleRuntime.Select(true,5);BattleRuntime.Select(false,5);transition();BattleRuntime.Update();ResetCheck("transition reset");Check(!BattleRuntime.Active,"transition hidden");
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
SceneManager.Instance.Scene.IsInBattle=false;BattleRuntime.Update();ResetCheck("confirmed sea end resets both");
SceneManager.Instance.Scene.IsInBattle=true;BattleRuntime.Update();ResetCheck("next sea battle starts X1");
BattleRuntime.Reset();Console.WriteLine($"PASS {checks} battle scope, restoration, reset, pooling and multiplier checks (managed stubs; no game execution).");

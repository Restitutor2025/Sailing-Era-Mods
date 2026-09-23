namespace BattleTests {
 public class Object { private static int next;public IntPtr Pointer {get;set;}=new(++next);public T? TryCast<T>() where T:class=>this as T; }
 public class Player : Object { public Port PlayerPort=new(); }
 public class Port { public bool IsStayInPort; }
 public class Ship {public long ShipGuid;}
 public class Team {public bool isPlayer=true;}
 public class Entity:Object {public Il2CppClient.WorldLogic.Entity.Data.BoatEntityData Data=new();public List<object> _comps=new();}
 public class Log {public List<string> Lines=new();public void Error(string s){Lines.Add("E "+s);} public void Msg(string s){Lines.Add(s);} }
}
namespace Restitutor.Cheats.Interface {
 public static class Host {public static bool Enabled=true;public static BattleTests.Player? Player;}
}
namespace Il2CppClient.Manager {
 public class PlayerDataManager {public static PlayerDataManager Instance=new();public BattleTests.Player? Data;}
}
namespace Il2CppCore.SceneSystem {
 public class SceneManager {
  public static SceneManager Instance=new();public bool IsSceneEntered=true,IsInLoadingOrStarting,IsInOceanScene=true;
  public Il2CppClient.WorldLogic.Scenes.OceanScene Scene=new();public int CurCalls;public BattleTests.Object CurScene(){CurCalls++;return Scene;}
 }
}
namespace Il2CppClient.WorldLogic.Scenes {
 public class OceanScene:BattleTests.Object {
  public bool IsInBattle=true;public BattleTests.Team _focusTeam=new();public BattleTests.Entity Focus=new();public BattleTests.Entity GetFocusPlayer()=>Focus;
 }
}
namespace Il2CppClient.WorldLogic.Entity.Data {
 public class BoatEntityData:BattleTests.Object {public bool IsPlayer=true,IsPlayerFlagship=true;public long Guid=123;public BattleTests.Ship baseShipData=new(){ShipGuid=456};}
}
namespace Il2CppClient.Battle {
 public class ClientBattle:BattleTests.Object {public bool IsDone;public BattleTests.Ship PlayerShip=new(){ShipGuid=456};public List<CombatUnit> PlayerUnits=new();}
 public class CombatUnit:BattleTests.Object {
  public bool IsPlayerUnit=true,IsPawn;public int Attack=10,Craft=20,Perception=30,Physical=40;
  public int Hp=70,MaxHp=100,HpBeforeMelee=70;
 }
}
namespace Il2Cpp { public class MeleeBattleController {public Il2CppClient.Battle.ClientBattle _Battle=new();} }
namespace Il2CppClient.WorldLogic.Entity.Component.Boat.BoatAttack {
 public class BoatEntityGun {public Il2CppClient.WorldLogic.Entity.Data.BoatEntityData boatData=new();}
 public class AttackInfo:BattleTests.Object {public long attackGuid=1,sourceEntityGuid=123;public bool isSiege;public float gunDamageFactor=1;}
}
namespace Restitutor.Cheats.Battle { internal static class EntryPoint {internal static bool Loaded=true;internal static BattleTests.Log Log=new();} }

namespace Il2CppClient.WorldLogic.Entity.Component.Boat {
 public class BoatEntityHitHandler:BattleTests.Object {public bool lockHealth;}
 public class BoatEntityBoardShoot:BattleTests.Object {
  public int ShootProgress;public BoatEntityBoardShoot? targetEnemy;public Action? BoardShootingBegin;public int Added,Calls;
  public void AddProgress(int n){ShootProgress+=n;Added+=n;Calls++;}
 }
}
namespace Restitutor.Cheats.Battle {
 // Managed stand-in for the Il2Cpp-only GameLinks.cs (component list walk + delegate field).
 internal static class GameLinks {
  internal static int Searches;internal static bool Throw;
  private static T? Find<T>(BattleTests.Entity? e) where T:class { Searches++;if(Throw)throw new InvalidOperationException("stub link failure");
   if(e==null)return null;foreach(var c in e._comps)if(c is T t)return t;return null; }
  internal static Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityHitHandler? HitHandler(BattleTests.Entity? e)=>Find<Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityHitHandler>(e);
  internal static Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityBoardShoot? BoardShoot(BattleTests.Entity? e)=>Find<Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityBoardShoot>(e);
  private static Action? handler;
  internal static void Subscribe(Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityBoardShoot s,Action cb){handler??=cb;s.BoardShootingBegin=(Action?)Delegate.Combine(s.BoardShootingBegin,handler);}
  internal static void Unsubscribe(Il2CppClient.WorldLogic.Entity.Component.Boat.BoatEntityBoardShoot s){s.BoardShootingBegin=(Action?)Delegate.Remove(s.BoardShootingBegin,handler);}
 }
}

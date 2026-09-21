namespace BattleTests {
 public class Object { private static int next;public IntPtr Pointer {get;set;}=new(++next);public T? TryCast<T>() where T:class=>this as T; }
 public class Player : Object { public Port PlayerPort=new(); }
 public class Port { public bool IsStayInPort; }
 public class Ship {public long ShipGuid;}
 public class Team {public bool isPlayer=true;}
 public class Entity {public Il2CppClient.WorldLogic.Entity.Data.BoatEntityData Data=new();}
 public class Log {public void Error(string s){} }
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
  public Il2CppClient.WorldLogic.Scenes.OceanScene Scene=new();public BattleTests.Object CurScene()=>Scene;
 }
}
namespace Il2CppClient.WorldLogic.Scenes {
 public class OceanScene:BattleTests.Object {
  public bool IsInBattle=true;public BattleTests.Team _focusTeam=new();public BattleTests.Entity Focus=new();public BattleTests.Entity GetFocusPlayer()=>Focus;
 }
}
namespace Il2CppClient.WorldLogic.Entity.Data {
 public class BoatEntityData:BattleTests.Object {public bool IsPlayer=true;public long Guid=123;public BattleTests.Ship baseShipData=new(){ShipGuid=456};}
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

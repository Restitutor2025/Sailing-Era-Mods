namespace Il2CppClient.Const {
 public enum EPortFacilityType { Govhouse, Station, Market }
 public enum ECommanderPropertyType { PrivilegeCostReduce }
 public enum EGovHouseLicence { None }
}
namespace Il2CppClient.WorldLogic.GameEffect { public class Placeholder {} }
namespace Il2CppClient.Utils {
 public static class TemplateUtils {
  public static bool HasMarket=true;
  public static bool PortContainsFacility(int p,int t)=>t!=(int)Il2CppClient.Const.EPortFacilityType.Market || HasMarket;
  public static Il2CppGyyx.Template.Facility GetPortFacilityLineByType(int p,int t)=>new();
 }
 public static class TextLibUtils { public static string Text(string key,string fallback)=>key; }
 public static class EffectCalculator { public static int GetPropertyWithCommanderEffectPercentageChange(Il2CppClient.Const.ECommanderPropertyType p,int n)=>n; }
}
namespace Il2CppGyyx.Template {
 public class Facility { public int tid=1; }
 public class House { public int[] licences={1,2,3}; }
 public class Licence { public int type=2,needInfluence,param; public string name="benefit"; }
 public class Named { public string name="city"; }
 public static class TemplateManager {
  public static House GetGovHouseTpl(int id)=>new();
  public static Licence GetGovHouseLicence(int id)=>new(){needInfluence=id*100};
  public static Named GetItem(int id)=>new(); public static Named GetPort(int id)=>new();
  public static object GetMarket(int id)=>new();
 }
}
namespace Il2CppClient.PlayerStore {
 public class WorldPortData { public int PortID=214, Influence; public HashSet<int> OpenLicenceList=new(); public Dictionary<int,object> _dictNaturalResources=new(); }
 public class WorldPortHoldDB {
  public IntPtr Pointer=new(10);
  public WorldPortData Port=new(); public int Calls, FailId;
  public WorldPortData GetPortData(int p)=>Port;
  public bool AddGovHouseLicenceData(int p,int id,int value,int day,Il2CppClient.Const.EGovHouseLicence type) {
   Calls++; if(FailId==id)throw new InvalidOperationException("native grant failed"); return Port.OpenLicenceList.Add(id);
  }
 }
 public class TimeData { public int TotalGameDay; }
 public class KnowledgeDB {
  public IntPtr Pointer=new(1); public HashSet<(int,int)> Known=new();
  public bool GetCargoPortIsUnlock(int id,int port)=>Known.Contains((id,port));
 }
 public class MarketData {
  public List<int> ListGoodsMarket=new(); public Dictionary<int,object> DicGoods=new();
  public object? FindGoodsData(int id)=>DicGoods.GetValueOrDefault(id);
 }
 public class MarketDB {public MarketData? Data=new(); public MarketData? FindMarketMessage(int port)=>Data;}
 public class PlayerPortDB {public bool IsStayInPort=true; public int StayInPortId=214;}
 public class PlayerData {
  public WorldPortHoldDB WorldPort=new(); public TimeData WorldTimeDataDB=new();
  public KnowledgeDB KnowledgeData=new(); public MarketDB MarketData=new(); public PlayerPortDB PlayerPort=new();
 }
}
namespace Il2CppClient.Manager {
 public class Placeholder {}
}
namespace Il2CppClient.UILogic.UIKnowledge {
 public class KnowledgeManager {
  public static KnowledgeManager Instance=new();
  public Il2CppClient.PlayerStore.KnowledgeDB? _knowledgeData;
  public Action<int>? OnUnlock; public int Calls;
  public void UnlockCargoDataByMarketPortId(int port){Calls++;OnUnlock?.Invoke(port);}
 }
}
namespace Il2CppClient.UILogic.UIMarket {
 public class Content { public bool isDisposed; }
 public class View { public object? _state=new(); public Content? UIContent=new(); }
 public class Model { public int PortId=214,Dirty; public void MarkDirty()=>Dirty++; }
 public class UIMarketCtrl {
  public View? View=new(); public Model? Model=new(); public bool ThrowOnClose,ThrowOnBuild,Closed; public int CloseChecks,Builds;
  public bool IsClose() { CloseChecks++; if(ThrowOnClose)throw new NullReferenceException("IsClose");return Closed; }
  public void SetMarketGoods() { Builds++; if(ThrowOnBuild)throw new NullReferenceException("SetMarketGoods"); Restitutor.Contribution.LiveViews.MarketBuilt(this); }
 }
}
namespace Restitutor.Contribution {
 public class Logger { public void Msg(string text){} }
 public static class EntryPoint {
  internal static Journal Journal=new(); internal static Logger Log=new(); internal static int Errors; internal static double Now;
  internal static Il2CppClient.PlayerStore.PlayerData? Player;
  internal static void Error(string context,Exception ex)=>Errors++;
 }
}

// Offline test doubles. No game/Unity/MelonLoader assembly is loaded by this executable.
namespace HarmonyLib {
 public class HarmonyMethod { public HarmonyMethod(System.Reflection.MethodInfo x){} }
 public class Harmony { public static int Registrations; public void UnpatchSelf(){} public void Patch(System.Reflection.MethodInfo m,HarmonyMethod? p,HarmonyMethod? q,HarmonyMethod? finalizer){Registrations++;} }
}
namespace MelonLoader {
 public class MelonInfoAttribute:Attribute { public MelonInfoAttribute(Type t,string n,string v,string a){} }
 public class MelonGameAttribute:Attribute { public MelonGameAttribute(string d,string n){} }
 public class MelonLogger { public class Instance { public void Msg(string s){} public void Error(string s){} } }
 public class MelonMod { public MelonLogger.Instance LoggerInstance=new(); public HarmonyLib.Harmony HarmonyInstance=new(); public virtual void OnInitializeMelon(){} public virtual void OnUpdate(){} }
}
namespace MelonLoader.Utils { public static class MelonEnvironment { public static string GameRootDirectory="."; public static string UserDataDirectory="."; } }
namespace Il2CppSystem {
 public class Action<T> { readonly System.Action<T> f; public Action(System.Action<T> f){this.f=f;} public void Invoke(T x)=>f(x); public static explicit operator Action<T>(System.Action<T> f)=>new(f); }
}
namespace Il2CppSystem.Collections.Generic { public class List<T>:System.Collections.Generic.List<T> { readonly Doubles.Native identity=new(); public IntPtr Pointer=>identity.Pointer; } }
namespace Doubles {
 public class Native { static long serial; public IntPtr Pointer {get;}=new(++serial); public T? TryCast<T>() where T:class=>this as T; }
}
namespace Il2CppFairyGUI {
 public class InputEvent {public UnityEngine.Vector2 position;public int button,touchId;} public class EventContext {public Doubles.Native? sender,data;public InputEvent? inputEvent;public bool Stopped,Captured;public void StopPropagation()=>Stopped=true;public void CaptureTouch()=>Captured=true;}
 public class EventCallback1 {readonly System.Action<EventContext> f;public EventCallback1(System.Action<EventContext> a){f=a;}public void Invoke(EventContext e)=>f(e);public static explicit operator EventCallback1(System.Action<EventContext> a)=>new(a);}
 public class EventListener {public System.Collections.Generic.List<EventCallback1> Callbacks=new();public void Add(EventCallback1 cb)=>Callbacks.Add(cb);public void Remove(EventCallback1 cb)=>Callbacks.Remove(cb);public void Call(EventContext? e=null){foreach(var cb in Callbacks.ToArray()) cb.Invoke(e??new());}}
 public class GGraph:GObject {public void DrawRect(float w,float h,int line,UnityEngine.Color lc,UnityEngine.Color fill){}}
 public class GSlider:GComponent {public double min,max,value;public bool wholeNumbers,changeOnClick,canDrag;public GObject? _barObjectH,_gripObject;public float _barMaxWidth,_barMaxWidthDelta,_barStartX;public EventListener onChanged=new();public void __gripTouchBegin(EventContext e){}public void __gripTouchMove(EventContext e){}public void __gripTouchEnd(EventContext e){}public void __barTouchBegin(EventContext e){}}
 public enum AlignType { Left,Center,Right }
 public enum VertAlignType { Top,Middle,Bottom }
 public enum AutoSizeType { None,Both,Height,Shrink }
 public class TextFormat { public int size=40; public UnityEngine.Color color; public object? gradientColor; public AlignType align; public void CopyFrom(TextFormat f){size=f.size;color=f.color;gradientColor=f.gradientColor;align=f.align;} }
 public class GObject:Doubles.Native {
  public Doubles.Native? data; public EventListener onClick=new(),onTouchBegin=new(),onTouchMove=new(),onTouchEnd=new(),onRemovedFromStage=new();
  public int sortingOrder; public float xMin=>x; public float yMin=>y; public float actualWidth=>width*scaleX; public float actualHeight=>height*scaleY;
  public bool visible=true,touchable=true,onStage=true,isDisposed; public GComponent? parent; public float x,y,width=100,height=100,scaleX=1,scaleY=1;
  public void SetXY(float a,float b){x=a;y=b;} public void SetSize(float w,float h){width=w;height=h;}
  public static bool RejectGlobalTransforms;
  public UnityEngine.Vector2 LocalToGlobal(UnityEngine.Vector2 v){if(RejectGlobalTransforms) throw new Exception("Stage transform unavailable during renderer");var p=new UnityEngine.Vector2(x+v.x*scaleX,y+v.y*scaleY);return parent==null?p:parent.LocalToGlobal(p);}
  public UnityEngine.Vector2 GlobalToLocal(UnityEngine.Vector2 v){var p=parent==null?v:parent.GlobalToLocal(v);return new((p.x-x)/scaleX,(p.y-y)/scaleY);}
  public void Dispose(){isDisposed=true;parent?.Children.Remove(this);parent=null;}
 }
 public class GComponent:GObject {public GObject GetChildAt(int index)=>Children[index];public System.Collections.Generic.List<GObject> Children=new();public GObject AddChild(GObject o){o.parent=this;Children.Add(o);return o;}}
 public class GLoader:GObject {}
 public class Controller {public int selectedIndex;public void SetSelectedIndex(int i)=>selectedIndex=i;}
 public class GTextField:GObject { public string text=""; public TextFormat textFormat=new(); public AlignType align {get=>textFormat.align;set=>textFormat.align=value;} public float stroke=1; public UnityEngine.Color strokeColor; public AutoSizeType autoSize; public VertAlignType verticalAlign; public bool singleLine; }
 public class GButton:GComponent { public string title=""; }
}
namespace UnityEngine { public static class Input {public static bool MiddleHeld,MiddleUp;public static bool GetMouseButton(int n)=>n==2&&MiddleHeld;public static bool GetMouseButtonUp(int n)=>n==2&&MiddleUp;} public struct Color { public int value; public Color(float r,float g,float b,float a){value=0;} public static Color clear=>new(); public static Color black=>new(); public static Color white=>new(){value=1}; } public struct Vector2 { public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;} } }
namespace Il2CppClient.Const { public enum ECargoState { None=0, Other=1 } }
namespace Il2CppClient.PlayerStore {
 public class PlayerItemData:Doubles.Native { public long Guid; public int ItemId,Number,BuyPrice,Supply,Quality; public bool IsSuper,IsCommerce; public Il2CppClient.Const.ECargoState state; }
 public class ItemBagData:Doubles.Native { public Il2CppSystem.Collections.Generic.List<PlayerItemData> Items=new(); public int GetItemAmount(int id)=>Items.Where(x=>x.ItemId==id).Sum(x=>x.Number); }
 public class PlayerBagDB:Doubles.Native { public ItemBagData ItemBag=new(); public int Capacity=999,Dirty; public void MarkDBDirty()=>Dirty++; public bool RemoveItemByGuid(long guid){int i=ItemBag.Items.FindIndex(x=>x.Guid==guid);if(i<0)return false;ItemBag.Items.RemoveAt(i);Dirty++;return true;} }
 public class PlayerLaneDB { public HashSet<int> Unlocked=new(); public int Dirty; public bool CheckLaneIsUnlockByLaneId(int lane)=>Unlocked.Contains(lane); public bool UnlockPrefabLane(int lane){Dirty++;return Unlocked.Add(lane);} }
 public class EquipData {public PlayerItemData Equip=null!; public int WearRoleId;public long EquipEffectGuid,SpecialEffectGuid,ExclusiveEffectGuid;}
 public class PlayerEquipDB { public System.Collections.Generic.Dictionary<long,EquipData> Items=new();public EquipData? GetEquipByGuid(long guid)=>Items.GetValueOrDefault(guid); }
}
namespace Il2CppClient.Manager {
 public class GameManager{}
 public class UIManager { public static UIManager Instance=new(); public int Warnings; public void ShowConfirmationPanel(string text,System.Action? a,System.Action? b,string? c,string? d){Warnings++;} public void ShowInputNumPromptBox(){} }
 public class PlayerDataManager { public static PlayerDataManager Instance=new(); public PlayerData Data=new(); }
 public class PlayerData { public Il2CppClient.PlayerStore.PlayerBagDB PlayerBag=new(); public Il2CppClient.PlayerStore.PlayerEquipDB PlayerEquipData=new(); public Il2CppClient.PlayerStore.PlayerLaneDB PlayerLaneData=new(); }
}
namespace Il2CppClient.Utils { public static class TextLibUtils { public static string Text(string s)=>s; } }
namespace Il2CppClient.UILogic.UIBag {
 public class UIBagCtrl {public UIBagModel Model=new(); public int Refreshes; public void RefreshDataModel()=>Refreshes++;}
 public class UIBagView { public UIBagModel _model=new(); public Il2CppBag.UIcompBag UIContent=new(); }
 public class UIBagModel { public int GroupIndex,SlotIndex,Dirty; public Il2CppSystem.Collections.Generic.List<ItemGroup> BagGroupList=new();public Il2CppSystem.Collections.Generic.List<InventoryItem> ItemList=new();public void MarkDirty()=>Dirty++; }
 public class BagModel:UIBagModel {}
 public class ItemGroup { public Il2CppSystem.Collections.Generic.List<InventoryItem> ItemList=new(); }
 public class BagGroup:ItemGroup {}
 public class InventoryItem:Doubles.Native { public int GlobalIndex,SlotIndex,WearRole; public bool IsExclusive;public Template ItemTemplate=new(); }
 public class BagRow:InventoryItem {}
 public class Template { public int tid=36000; public string name="Lantern"; }
}
namespace Il2CppBag { public class UIcompBag { public Il2CppFairyGUI.GTextField texCapacity=new();public Il2CppFairyGUI.Controller ctrlOverLoad=new(); } public class UIbtnSlot:Il2CppFairyGUI.GObject { public Il2CppCommon.UIcompSlotCommon compSlotCommon=new(); } }
namespace Il2CppCommon { public class UIcompSlotCommon:Il2CppFairyGUI.GComponent { public Il2CppFairyGUI.GTextField title=new(); public Il2CppFairyGUI.GLoader loaderIcon=new(); public UIcompSlotCommon(){AddChild(loaderIcon);AddChild(title);} } }
namespace Il2CppUILandExplore { public class UIButtonItem:Il2CppFairyGUI.GButton { public Il2CppFairyGUI.GTextField texCount=new(),texHeavy=new(); } }
namespace Il2CppClient.UILogic.UILandExplore {
 public class Goods:Doubles.Native { public int GoodsID; public string Name="",Image="",Bg="",Desc="",TypeDesc=""; public bool IsSelect; public float Heavy; }
 public class UILandExploreModel:Doubles.Native { public Il2CppSystem.Collections.Generic.List<Goods> ListDeport=new(),ListLandBag=new(); public Goods CurGoods=new(); public int Supply,EntryType=1;public float MaxWeight=100,ItemWeight,SupplyWeight; public float CurWeight=>ItemWeight+SupplyWeight; public int Dirty; public void MarkDirty()=>Dirty++; }
 public class UILandExploreCtrl { public UILandExploreModel Model=new(); }
 public class UILandExploreView { public Il2CppUILandExplore.UIcompLandExplore UIContent=new();public bool IsInputActive=true; public UILandExploreModel _model=new(); }
}
namespace Il2CppClient.UILogic.UICommonInputNum {
 public class UICommonInputNumView {public UICommonInputNumModel _model=new();public Il2CppCommonPrompt.UICompInputNum UIContent=new();public void Refresh(){}}
 public class UICommonInputNumModel:Doubles.Native { public long _curNum; public long CurNum { get=>_curNum; set { if(Restitutor.ItemRebuild.EntryPoint.TestPopupSet(this,value)) throw new Exception("Unowned native setter (would require a slider)"); } } public int Dirty; public void MarkDirty()=>Dirty++; }
 public class UICommonInputNumCtrl:Doubles.Native {
  public static UICommonInputNumCtrl Instance=new(); public UICommonInputNumModel Model=new(); public int Opens,Closes;
  public void OpenInputNumPanel(long current,long max,long currency,Il2CppSystem.Action<long>? callback,object? slider) { Restitutor.ItemRebuild.EntryPoint.TestPopupOpening(this); Model.CurNum=current; Opens++; }
  public void OnClickBtnReturn() { Restitutor.ItemRebuild.EntryPoint.TestPopupReturn(this); Closes++; Restitutor.ItemRebuild.EntryPoint.TestPopupClosed(this); }
 }
}
namespace Il2CppClient.UILogic.UIPropStore {
 public enum ESelectType {All,Consumables,Equip_haveEq,Equip,Book,Auxiliary_ship}
 public class Slot:Doubles.Native {public int id,price,type,wearRole;public long guid;public bool isSelect,IsOnly,isTask,isParliamentTask,IsExclusive;}
 public class UIPropStoreModel:Doubles.Native {public Il2CppSystem.Collections.Generic.List<Slot> ListCurPlay=new();public int SelectNum,Sum,Dirty; public int curShopType=1;public Il2CppSystem.Collections.Generic.List<Slot> listPropStore=new(),listCollege=new(),listBlackMarket=new(),listGuild=new();public int BuyCount=>(curShopType switch {1=>listPropStore,2=>listCollege,3=>listBlackMarket,_=>listGuild}).Count(x=>x.isSelect);public ESelectType CurSelect;public Slot? CurGoods;public void MarkDirty()=>Dirty++;}
 public class UIPropStoreView:Doubles.Native {public bool IsInputActive=true;public UIPropStoreModel _model=new();public PlayerList ListPlayer=new();public Il2CppUIPropStore.UIcompPropStore UIContent=new();}
 public class PlayerList {public int selectedIndex=-1,numItems;public PaneStub? scrollPane=new();}
 public class PaneStub {public float posX,posY;}
 public class UIPropStoreCtrl:Doubles.Native {public UIPropStoreModel Model=new();public UIPropStoreView View=new();public void CalculateMoney(){Model.Sum=Model.ListCurPlay.Where(x=>x.isSelect).Sum(x=>x.price);}public void SetPlayerGoods(){}public void BtnMulti(Il2CppFairyGUI.EventContext e){}public bool IsSelectAll(Il2CppSystem.Collections.Generic.List<Slot> rows)=>rows.Where(x=>!x.isTask&&!x.isParliamentTask).All(x=>x.isSelect);public void SelectSameGoods(){}public void SetCurGoods(){var i=View.ListPlayer.selectedIndex;Model.CurGoods=i>=0&&i<Model.ListCurPlay.Count?Model.ListCurPlay[i]:null;Model.MarkDirty();}public void Exchange(){}public void CloseHook(){}}
}

namespace Il2CppGyyx.Template {public class Cargo {public int quality;} public class PrefabLane {public int tid,lineMap;} public static class TemplateManager {public static Dictionary<int,Cargo> Cargoes=new();public static List<PrefabLane> PrefabLaneValues=new();public static bool GetCargo(int id,out Cargo? c)=>Cargoes.TryGetValue(id,out c);}}

namespace Il2CppUIPropStore {
 public class UIbtnSlot:Il2CppFairyGUI.GComponent {public Il2CppFairyGUI.Controller ctrlMulti=new();public Il2CppFairyGUI.GTextField texAmount=new();}
 public class UIcompPropStore:Il2CppFairyGUI.GComponent {public Il2CppFairyGUI.GTextField texItemCapacity=new();}
}
namespace Il2CppClient.UILogic.UILandExploreSupply {
 public class UILandExploreSupplyModel:Doubles.Native {public int EntryType;public float ItemHeavy,MaxHeavy;public int AddNum,MaxNum,Price,CountPrice,Dirty;public bool BanBtnAdd,BanBtnCut;}
 public class UILandExploreSupplyView {public UILandExploreSupplyModel _model=new();public Il2CppUILandExplore.UIcompLandExploreSupply UIContent=new();}
 public class UILandExploreSupplyCtrl {public int Prepared;public void ShowHook(){Prepared++;Model.MaxNum=(int)Math.Floor((Math.Round(Model.MaxHeavy,1)-Math.Round(Model.ItemHeavy,1))/1.5); } public int Applied,Closed;public void OnAction_B(){Closed++;}public void OnClickBtnOK(){Applied++;Closed++;}public static UILandExploreSupplyCtrl Instance=new();public UILandExploreSupplyModel Model=new();public void SetCountPrice(){Model.CountPrice=Model.Price*Model.AddNum;Model.BanBtnAdd=Model.AddNum==Model.MaxNum;Model.BanBtnCut=Model.AddNum==0;Model.Dirty++;}}
}

namespace Il2CppCommonPrompt {
 public class UICompInputNum:Il2CppFairyGUI.GComponent {
  public Il2CppFairyGUI.GTextField texInput=new(),inputNum=new();
  public Il2CppFairyGUI.GButton btnReduce=new(),btnAdd=new(),btnReturn=new();
  public UICompInputNum(){SetSize(600,350);foreach(var x in new Il2CppFairyGUI.GObject[]{texInput,inputNum,btnReduce,btnAdd,btnReturn})AddChild(x);texInput.text="금액 입력";inputNum.SetXY(120,140);inputNum.SetSize(360,50);btnReduce.SetXY(60,140);btnReduce.SetSize(60,50);btnAdd.SetXY(480,140);btnAdd.SetSize(60,50);btnReturn.SetXY(170,270);btnReturn.SetSize(260,60);}
 }
}
namespace Il2CppUILandExplore {
 public class UIcompLandExploreSupply:Il2CppFairyGUI.GComponent {
  public Il2CppFairyGUI.GButton btnCut=new(),btnAdd=new(),btnOK=new();
  public UIcompLandExploreSupply(){SetSize(650,500);foreach(var x in new Il2CppFairyGUI.GObject[]{btnCut,btnAdd,btnOK})AddChild(x);btnCut.SetXY(120,240);btnCut.SetSize(60,40);btnAdd.SetXY(470,240);btnAdd.SetSize(60,40);btnOK.SetXY(195,420);btnOK.SetSize(260,60);}
 }
}

namespace Il2CppUILandExplore {public class UIcompLandExplore:Il2CppFairyGUI.GComponent {public UIButtonItem BtnSupply=new();}}

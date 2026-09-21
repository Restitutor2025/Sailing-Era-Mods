using Il2CppFairyGUI;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UICharacter;
namespace UnityEngine { public record struct Vector2(float x,float y); public struct Rect { public float x,y,width,height; public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;} public Vector2 center=>new(x+width/2,y+height/2); } public static class Application{public static bool isFocused=true;}public static class Time{public static int frameCount=10;public static float unscaledTime=10;}public readonly record struct Color(float r,float g,float b,float a) { public static Color clear=>new(0,0,0,0);public static Color white=>new(1,1,1,1);public static Color black=>new(0,0,0,1); } }
namespace UnityEngine.InputSystem {public class Button{public bool wasPressedThisFrame,isPressed;}public class Keyboard{public static Keyboard current=new();public Button escapeKey=new(),spaceKey=new();}public class Mouse{public static Mouse current=new();public Button rightButton=new();}}
namespace Il2CppClient.UILogic.UITips {
public class UITipsModel {public int CtrlSelect=6,CtrlBaseSelect=1;}
public class UITipsView {public GComponent _UIContent_k__BackingField=new();public Transition AniTips=new();public UITipsModel _model=new();}
public class UITipsCtrl {public static UITipsCtrl Instance=new();public UITipsView _View_k__BackingField=new();public int Calls;public string LastText="";public void ShowBaseTips(string text,string icon){
 var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
 typeof(Restitutor.TabCharacters.EntryPoint).GetMethod("BookTips",flags)!.Invoke(null,new object[]{this});
 if(_View_k__BackingField._UIContent_k__BackingField.parent==null)GRoot.inst.AddChild(_View_k__BackingField._UIContent_k__BackingField);Calls++;LastText=text;_View_k__BackingField.AniTips.playing=true;
 typeof(Restitutor.TabCharacters.EntryPoint).GetMethod("BookTipsAfter",flags)!.Invoke(null,null);
}}}
namespace UnityEngine.InputSystem { public class InputAction { public string name=""; public struct CallbackContext { public InputAction action;public bool performed,canceled,Pressed;public bool ReadValueAsButton()=>Pressed; } } }
namespace Il2CppSystem.Collections.Generic { public class List<T>:System.Collections.Generic.List<T> {} }
namespace Il2CppCore.InputSystem { public class InputSystemManager { public void OnEventCaptureInput(UnityEngine.InputSystem.InputAction.CallbackContext x) {} } }
namespace Il2CppClient.Manager { public class GameManager { public void ForceReset() {} } }
namespace Il2CppFairyGUI
{
 public class InputEvent {public int button,touchId;public float x=Stage.inst.touchPosition.x,y=Stage.inst.touchPosition.y;}
 public class Transition {public int _options;public const int OPTION_AUTO_STOP_DISABLED=2;public float timeScale=1;public bool playing;public Action? Completed;public int Stops;public void Stop(bool complete,bool callback){Stops++;playing=false;if(callback)Completed?.Invoke();}}
 public class DisplayObject {public T? TryCast<T>() where T:class=>this as T;} public class InputTextField:DisplayObject {}
 public class Stage {public DisplayObject? focus;public static Stage inst=new();public UnityEngine.Vector2 touchPosition;public int Cancelled;public void CancelClick(int id){Cancelled++;}}
 public class EventContext { public InputEvent inputEvent=new();public void CaptureTouch(){}public bool Stopped;public void StopPropagation()=>Stopped=true; }
 public sealed class EventCallback1 { readonly Action<EventContext> cb;public EventCallback1(Action<EventContext> a)=>cb=a;public static explicit operator EventCallback1(Action<EventContext> a)=>new(a);public void Invoke(EventContext e)=>cb(e); }
 public class EventListener { readonly List<EventCallback1> all=new();public int Count=>all.Count;public void Add(EventCallback1 c)=>all.Add(c);public void Remove(EventCallback1 c)=>all.Remove(c);public void Clear()=>all.Clear();public void Set(EventCallback1 c){Clear();Add(c);}public void Call(){foreach(var c in all.ToArray())c.Invoke(new());} }
 public class GObject
 {
  static long next;public IntPtr Pointer {get;}=(IntPtr)Interlocked.Increment(ref next);
  public bool isDisposed,touchable=true,visible=true;public float x,y,width=80,height=80,scaleX=1,scaleY=1;public int sortingOrder;public GComponent? parent;public GGroup? group;
  public EventListener onTouchBegin=new(),onTouchMove=new(),onTouchEnd=new(),onRollOut=new(),onClick=new(),onRemovedFromStage=new(),onRollOver=new();public Relations relations=new();public bool _gearLocked;
  public float xMin=>x;public float yMin=>y;public string name="";public float alpha=1;public string resourceURL="";public virtual GObject? displayObject=>this;
  public UnityEngine.Rect LocalToGlobal(UnityEngine.Rect r){if(displayObject==null)throw new NullReferenceException("LocalToGlobal requires displayObject");r=new(x+r.x*scaleX,y+r.y*scaleY,r.width*scaleX,r.height*scaleY);return parent==null?r:parent.LocalToGlobal(r);}
  public UnityEngine.Rect GlobalToLocal(UnityEngine.Rect r){if(parent!=null)r=parent.GlobalToLocal(r);return new((r.x-x)/scaleX,(r.y-y)/scaleY,r.width/scaleX,r.height/scaleY);}
  public int ControllerRefreshes;public void HandleControllerChanged(Controller c){ControllerRefreshes++;}
  public T? TryCast<T>() where T:class=>this as T;
  public void SetXY(float a,float b){x=a;y=b;}public void SetSize(float w,float h){width=w;height=h;}public void SetScale(float a,float b){scaleX=a;scaleY=b;}
  public void RemoveFromParent(){parent?.children.Remove(this);parent=null;}
  public uint Locks;public uint AddDisplayLock(){Locks++;return Locks;}public void ReleaseDisplayLock(uint n){Locks--;}
  public virtual void Dispose(){isDisposed=true;RemoveFromParent();onClick.Clear();onRollOver.Clear();}
 }
 public class GComponent:GObject
 {
  public OverflowType Overflow;public void SetupOverflow(OverflowType o)=>Overflow=o;
  public readonly List<GObject> children=new();public int numChildren=>children.Count;
  public GObject AddChild(GObject o)=>AddChildAt(o,children.Count);
  public GObject AddChildAt(GObject o,int i){o.RemoveFromParent();children.Insert(Math.Min(i,children.Count),o);o.parent=this;return o;}
  public void SetChildIndex(GObject o,int i){children.Remove(o);children.Insert(Math.Clamp(i,0,children.Count),o);} public GObject GetChildAt(int i)=>children[i];public int GetChildIndex(GObject o)=>children.IndexOf(o);
  public override void Dispose(){foreach(var c in children.ToArray())c.Dispose();base.Dispose();}
 }
 public class GGroup:GObject {public override GObject? displayObject=>null;public void EnsureBoundsCorrect(){}}
 public class GImage:GObject {public UnityEngine.Color color;} public static class UIPackage {public static GObject CreateObjectFromURL(string url)=>new GImage{resourceURL=url};}
 public enum AutoSizeType {None,Both,Height,Shrink}public enum AlignType {Left,Center,Right}public enum VertAlignType {Top,Middle,Bottom}public enum OverflowType {Visible,Hidden,Scroll}
 public class GLoader:GObject {public string url="";public float actualWidth=>width;public float actualHeight=>height;}
 public class Relations {public void CopyFrom(Relations r){}public void ClearAll(){}}
 public class GGraph:GObject {public UnityEngine.Color color;public void DrawRect(float w,float h,int line,UnityEngine.Color a,UnityEngine.Color b){SetSize(w,h);color=b;}}
 public class TextFormat {public int size=20;public string font="Korean";public UnityEngine.Color color;public object? gradientColor;public AlignType align;public void CopyFrom(TextFormat f){size=f.size;font=f.font;color=f.color;gradientColor=f.gradientColor;align=f.align;}}
 public class GTextField:GObject {public AlignType align;public VertAlignType verticalAlign;public float stroke;public UnityEngine.Color strokeColor;public string text="";public bool UBBEnabled,singleLine=true;public AutoSizeType autoSize=AutoSizeType.Both;public TextFormat textFormat=new();public float textWidth=>text.Length*textFormat.size*.6f;public float textHeight=>text.Split('\n').Sum(s=>singleLine||autoSize==AutoSizeType.Both?1:Math.Max(1,(int)Math.Ceiling(s.Length*Math.Max(1,textFormat.size)*.6/Math.Max(1,width))))*textFormat.size*1.2f;}
 public class GRoot:GComponent {public static GRoot inst=new(){width=1920,height=1080};public GObject? focus;}
 public class Controller {public int selectedIndex;public void SetSelectedIndex(int x)=>selectedIndex=x;}
 public sealed class ListItemRenderer {readonly Action<int,GObject> a;public ListItemRenderer(Action<int,GObject> cb)=>a=cb;public static explicit operator ListItemRenderer(Action<int,GObject> a)=>new(a);public void Invoke(int i,GObject o)=>a(i,o);}
 public class GList:GComponent
 {
  public ListItemRenderer itemRenderer=(ListItemRenderer)(Action<int,GObject>)((i,o)=>{});public int selectedIndex,columnGap=8,lineGap=8;
  public readonly List<GObject> Pool=new();
  public Func<GObject> Factory=()=>new GComponent();
  public int numItems {get=>numChildren;set {while(numChildren>value){var o=children[^1];o.RemoveFromParent();Pool.Add(o);}while(numChildren<value){var o=Pool.Count>0?Pool[^1]:Factory();Pool.Remove(o);AddChild(o);}for(int i=0;i<numChildren;i++)itemRenderer.Invoke(i,children[i]);}}
 }
}
namespace Il2CppCommon
{
 public class UIcompSlotCommon:Il2CppFairyGUI.GComponent {public static int Created;public static bool Missing;public Il2CppFairyGUI.GTextField title=new(){textFormat=new(){size=24,font="SlotFont"}};public static UIcompSlotCommon? CreateInstance(){Created++;return Missing?null:new();}}
}
namespace Il2CppCharacter
{
 public class UIBtn_SkillBook:GComponent {public Controller isSelect=new();public GLoader loaderItem=new(){x=8,y=6,width=64,height=66};public UIBtn_SkillBook(){AddChild(loaderItem);}}
 public class UICom_SkillItem:GComponent {public static UICom_SkillItem CreateInstance()=>new();public GTextField texName=new(){text="통솔"},texLevel=new(){text="8"},texAddLevel=new(){text="+5"};}
 public class UICharacterMain:GComponent {public GList listSkill=new();}
 public class UISheetCharacter:GComponent
 {
  public Controller tipsType=new(){selectedIndex=1};public GTextField TexMax=new();public GGroup groupTips=new(){x=1000,y=200,width=500,height=400};public GList listSkill=new();
  public UISheetCharacter(){
   AddChild(groupTips);var nested=new GGroup{group=groupTips};AddChild(nested);AddChild(new GTextField{text="nested tooltip text",group=nested,x=1024,y=550,width=400,height=30});AddChild(listSkill);AddChild(texLanguage);texLanguage.SetXY(450,160);
   AddChild(new GImage{resourceURL="ui://native-tooltip",x=1000,y=200,width=500,height=400,group=groupTips});
   int row=0;foreach(var o in new GObject[]{texSkillDesc,TexTitleSkillLevel,TexMax,texLevel,texAddLevel,listItem}){AddChild(o);o.group=groupTips;o.SetXY(1024,220+row++*50);o.SetSize(440,45);}
   texSkillDesc.textFormat.color=new(1,1,1,1);
  }
  public GTextField texLanguage=new(){text="한국어"},TexTitleCharacterLanguage=new(){text="언어"},texLanguageCount=new(){text="1/5"},texSkillDesc=new(){text="통솔 설명"},TexTitleSkillLevel=new(){text="스킬 레벨"},texLevel=new(){text="Lv.7"},texAddLevel=new(){text="+5"};public GList listItem=new();
 }
 public class UISheetSkill:GComponent
 {
  public GComponent comFilter=new(),btnSkillUp=new();public GList listBook=new(){Factory=()=>new UIBtn_SkillBook()},listCondition=new(){height=70};public Controller isSeaman=new(),noBook=new(),isExpNotEnough=new(),bookType=new(),btnType=new();public GTextField texCurExp=new(),TexNoBook=new(),TexTitleBook=new(){text="보유 책"},texName=new(){text="책 이름"},TexTitleBookCondition=new(){text="학습 조건"},TexTitleBookExp=new(){text="필요 경험치"},texExp=new(){text="500"},texBookDesc=new(){text="책 설명"},TexTitleBookSkill=new(){text="배울 수 있는 스킬"},texSkillName=new(){text="통솔"},texLanguageName=new(){text="한국어"},texTitleExp=new(){text="현재 함대 경험치"};public GObject loaderIconSkill=new();
  public UISheetSkill(){SetSize(2100,1200);foreach(var o in new GObject[]{listBook,btnSkillUp,listCondition,texCurExp,TexNoBook,TexTitleBook,texName,TexTitleBookCondition,TexTitleBookExp,texExp,texBookDesc,TexTitleBookSkill,texSkillName,texLanguageName,texTitleExp,loaderIconSkill,comFilter})AddChild(o);}
 }
}
namespace Il2CppClient.PlayerStore
{
 public class Native {static long n;public IntPtr Pointer {get;}=(IntPtr)Interlocked.Increment(ref n);}
 public class RoleSkillData {public int Level=7,MaxLevel=9;public bool IsMax=>Level>=MaxLevel;}
 public class PlayerRoleData:Native { public HashSet<int> Learned=new();public int RoleId=1,SkillPoints=3;public Dictionary<int,RoleSkillData> Skills=new(){{1,new()},{2,new()}};public RoleSkillData? GetRoleSkillById(int id)=>Skills.GetValueOrDefault(id); }
 public class PlayerHoldRoleDB {public int Calls;public PlayerRoleData? Replacement;public PlayerRoleData? FindHoldRole(int id)=>Replacement??UICharacterCtrl.Instance._View_k__BackingField._model.ListRole[0].HeroData;public bool UpdateRoleSkillLevelBySkillPoints(int id,int skill,int count){Calls++;var r=FindHoldRole(id)!;if(r.GetRoleSkillById(skill)!.IsMax)return false;r.GetRoleSkillById(skill)!.Level+=count;if(r.SkillPoints>=count)r.SkillPoints-=count;return true;}}
 public class PlayerItemData {public long Guid;public int ItemId,Number;public bool IsCommerce;}
 public class ItemBagData:Native {public Il2CppSystem.Collections.Generic.List<PlayerItemData> Items=new();}
 public class PlayerBagDB {public ItemBagData ItemBag=new();}
 public class PlayerCurrencyData {public long Amount=100;}
 public class PlayerCurrencyDB {public PlayerCurrencyData Currency=new();public PlayerCurrencyData FindCurrency(int id)=>Currency;}
 public class PlayerData {public PlayerBagDB PlayerBag=new();public PlayerCurrencyDB PlayerCurrency=new();public PlayerHoldRoleDB PlayerRole=new();}
}
namespace Il2CppClient.UILogic.UICharacter
{
 public enum ESheetType {Character,Skill,Equip}
 public enum ECurFoucType {None,Skill,SkillBook,Wear,Equip}
 public class Template {public int skillId,needExp=10;}
 public class SkillBook {public int BookId,BookType;public long BookGuid;public Template BookTemplate=new();}
 public class CharacterData {public PlayerRoleData HeroData=new();public bool isSeaman;}
 public class UICharacterModel:Native
 {
  public List<CharacterData> ListRole=new(){new()};public Dictionary<int,int> DictSkill=new(){{1,0},{2,1}};public int RoleIndex,SkillIndex,_bookIndex=-1;public ESheetType SheetType;public ECurFoucType CurFoucType;
  public Il2CppSystem.Collections.Generic.List<SkillBook> ListBook=new(),ListFilterBook=new();public long Exp;public bool _isSelectBook;public SkillBook? CurSelectBook;public int Dirty;
  public void ChangeDictAddItem(){}public int GetSkillIdByIndex(int i)=>i+1;public void MarkDirty()=>Dirty++;
 }
 public class UICharacterView:Native
 {
  public UICharacterModel _model=new();public Il2CppCharacter.UICharacterMain _UIContent_k__BackingField=new();public Il2CppCharacter.UISheetSkill SheetSkill=new();public Il2CppCharacter.UISheetCharacter SheetCharacter=new();public bool IsInputActive=true;public int NativeDetailCalls,NativeRenderCalls;public SkillBook? LastDetail;
  public UICharacterView(){_UIContent_k__BackingField.AddChild(SheetSkill);SheetSkill.visible=false;SheetSkill.SetXY(150,100);}
  public void RenderListBtnSkill(int i,GObject o){}
  public void RenderListBook(int i,GObject o){NativeRenderCalls++;o.onClick.Set((EventCallback1)(Action<EventContext>)(_=>{}));}
  public bool ThrowOnDetail;public GList? DetailPreview;
  public void RefreshSkillBookTips(SkillBook b,PlayerRoleData r){DetailPreview=_UIContent_k__BackingField.listSkill;if(ThrowOnDetail)throw new Exception("native detail failure");if(b.BookType==1&&_UIContent_k__BackingField.listSkill.numChildren==0)throw new Exception("native preview rows not prepared");NativeDetailCalls++;LastDetail=b;}
  public void RefreshSheetSkill(CharacterData role)=>throw new Exception("Regression: full skill refresh re-registers FairyGUI.GGraph while a request is pending");
  public void RefreshTipsSkill(PlayerRoleData r){if(_model.CurFoucType!=ECurFoucType.Skill)throw new Exception("native skill focus required");}
  public void RefreshSheetCharacter(CharacterData r){}public void RefreshTipsRoleInfo(){}
 }
 public class UICharacterCtrl:Native
 {
  public static UICharacterCtrl Instance=new();public static PlayerData Data=new();public UICharacterView _View_k__BackingField=new();public int Uses;public bool ThrowOnUse;
  public void InitBookData(PlayerBagDB b){var m=_View_k__BackingField._model;m.ListBook=new();foreach(var x in b.ItemBag.Items)m.ListBook.Add(new(){BookId=x.ItemId,BookGuid=x.Guid,BookType=x.ItemId==90?2:1,BookTemplate=new(){skillId=x.ItemId/10}});}
  public int GetBookStateByParam(SkillBook b,PlayerRoleData r)=>r.Learned.Contains(b.BookId)?1:_View_k__BackingField._model.Exp<b.BookTemplate.needExp?4:0;
  public void OnClickBtnBook(){if(ThrowOnUse)throw new Exception("native fault");Uses++;var m=_View_k__BackingField._model;var book=m.CurSelectBook??throw new Exception("missing selection");if(!m._isSelectBook)throw new Exception("not selected");var x=Data.PlayerBag.ItemBag.Items.Single(x=>x.Guid==book.BookGuid);if(--x.Number==0)Data.PlayerBag.ItemBag.Items.Remove(x);m.ListRole[m.RoleIndex].HeroData.Learned.Add(book.BookId);Data.PlayerCurrency.Currency.Amount-=book.BookTemplate.needExp;}
  public void InitSkillData(){}
 }
}
namespace Restitutor.TabCharacters
{
 public sealed partial class EntryPoint
 {
  private static bool Allowed=true;private static EntryPoint? host=new();public class Log {public static List<string> Lines=new();public void Msg(string s){Lines.Add(s);}public void Error(string x){}public void Warning(string x){Lines.Add(x);} }public Log LoggerInstance=new();
  private static bool IsCharacter(UICharacterView v)=>v._model.SheetType==ESheetType.Character;
  private void Patch(Type t,string n,Type[] a,string p,string? post=null){}
 }
}



namespace Il2CppClient.UILogic.UIHeroLevelUp {
 public class UIHeroLevelUpModel {public bool IsRewardSkill,BanTouch,IsSpecialLevelUp;public int ResultAniCount;}
 public class SkillLabel:GComponent {public Transition AniEnter=new();}
 public class UIHeroLevelUpView {public bool IsInputActive=true;public UIHeroLevelUpModel _model=new();public SkillLabel ComSkillLabel=new();}
 public class UIHeroLevelUpCtrl {public UIHeroLevelUpView _View_k__BackingField=new();}
}

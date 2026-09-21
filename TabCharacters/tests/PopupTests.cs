using System.Reflection;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using Restitutor.TabCharacters;
using UnityEngine.InputSystem;
int count=0;
void Check(bool b,string s){count++;if(!b)throw new Exception(s);}
object? Call(string n,params object?[] args)=>typeof(EntryPoint).GetMethod(n,BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,n=="CloseBooks"&&args.Length==0?new object?[]{"test"}:args);
UICharacterCtrl Fresh(params PlayerItemData[] items){Call("ResetBooks");UICharacterCtrl.Data=new();UICharacterCtrl.Data.PlayerBag.ItemBag.Items.AddRange(items);return UICharacterCtrl.Instance=new();}
PlayerItemData Item(int id,int qty,long guid)=>new(){ItemId=id,Number=qty,Guid=guid};
bool Input(string n,bool press){UnityEngine.Time.frameCount+=3;return (bool)Call("BookCapture",new InputAction.CallbackContext{action=new(){name=n},performed=press,canceled=!press,Pressed=press})!;}
IEnumerable<GObject> Descendants(GComponent c){foreach(var o in c.children){yield return o;if(o is GComponent child)foreach(var d in Descendants(child))yield return d;}}
void Escape(){Keyboard.current.escapeKey.wasPressedThisFrame=true;Call("TickBooks");Keyboard.current.escapeKey.wasPressedThisFrame=false;}
var ctrl=Fresh(Item(10,3,100),Item(20,2,200),Item(90,1,900));var view=ctrl._View_k__BackingField;var model=view._model;
bool Learn()=> (bool)Call("BookUse",ctrl)!;
var parent=view.SheetSkill.parent;var renderer=view.SheetSkill.listBook.itemRenderer;var focus=new GObject();GRoot.inst.focus=focus;
var sharedPreview=view._UIContent_k__BackingField.listSkill;
var originalBookWidth=view.SheetSkill.listBook.width;
var equipment=new GComponent();equipment.AddChild(new GTextField{text="항해자의 반지"});equipment.AddChild(new GTextField{text="+"});equipment.AddChild(new GTextField{text="1"});view.SheetCharacter.listItem.AddChild(equipment);
string TipState(GComponent c)=>string.Join("|",c.children.Select((o,i)=>$"{o.Pointer}:{i}:{o.x},{o.y},{o.width},{o.height},{o.scaleX},{o.scaleY},{o.visible},{o.touchable},{o.alpha},{o.group?.Pointer},{o.parent?.Pointer},{o._gearLocked}"))+$"#{c.GetType()}";
var nativeTipBefore=TipState(view.SheetCharacter);
Call("OpenBooks",view,0);
Check(TipState(view.SheetCharacter)==nativeTipBefore,"opening never moves, resizes, reparents or regroups native tooltip widgets");
Check(view._UIContent_k__BackingField.listSkill==sharedPreview&&sharedPreview.numChildren==0,"native detail restores shared preview without rendering/mutating original rows");
Check(view.DetailPreview!=sharedPreview&&view.DetailPreview?.numChildren==2,"isolated package rows satisfy native preview indexes");
var panel=view.SheetSkill.parent!;
var nestedGroup=view.SheetCharacter.children.OfType<GGroup>().Single(g=>g!=view.SheetCharacter.groupTips);
Check(nestedGroup.parent==view.SheetCharacter,"non-rendering nested group remains in native parent");
Check(!Descendants(panel.parent!).OfType<GTextField>().Any(t=>t.text=="nested tooltip text")&&view.SheetCharacter.children.OfType<GTextField>().Any(t=>t.text=="nested tooltip text"),"native tooltip text stays in its own parent");
var infoStandIn=(GComponent)panel.parent!.children.Single(o=>o is GComponent g&&g!=panel&&g.children.All(x=>!x.visible));
Check(infoStandIn.x==1000&&infoStandIn.y==200&&infoStandIn.width==500&&infoStandIn.height==400,"empty stand-in marks native tooltip bounds");
Check(panel.x+panel.width*panel.scaleX<=infoStandIn.x||panel.x>=infoStandIn.x+infoStandIn.width,"learning panel sits beside native tooltip without overlap");
float firstPanelX=panel.x,firstPanelY=panel.y;view.SheetCharacter.groupTips.x+=40;view.SheetCharacter.groupTips.width+=12;
Call("RefreshBookBindings",view);Call("TickBooks");
Check(panel.x==firstPanelX&&panel.y==firstPanelY&&infoStandIn.x==1000&&infoStandIn.width==500,"book change keeps the first side position even if native tooltip bounds change");
view.SheetCharacter.groupTips.x-=40;view.SheetCharacter.groupTips.width-=12;
Check(panel.width==488&&panel.height<1500&&panel.scaleX<=1,"popup excludes 2100x1200 native canvas and never enlarges content");
Check(view.SheetCharacter.listItem.parent==view.SheetCharacter&&equipment.parent==view.SheetCharacter.listItem,"original tooltip equipment widgets are retained intact");
Check(GRoot.inst.focus==focus&&panel.parent!.children.OfType<GGraph>().All(o=>o.color.a==0),"nonmodal pair neither steals focus nor adds full-screen shade");
Check(panel.children.OfType<GImage>().Any(o=>o.resourceURL=="ui://native-tooltip"),"companion clones native background package asset");
Check(panel.children.OfType<GImage>().Single(o=>o.resourceURL=="ui://native-tooltip").alpha==.8f&&panel.alpha==1&&Descendants(panel).Where(o=>o is not GImage||((GImage)o).resourceURL!="ui://native-tooltip").All(o=>o.alpha==1),"only the learning panel background is translucent (0.8)");
Check(view.SheetCharacter.children.OfType<GImage>().Concat(Descendants(panel.parent!).OfType<GImage>()).Where(o=>o.parent!=panel).All(o=>o.alpha==1),"native info pane background keeps its opacity");
Check(!(bool)Call("BookNativeTips",view)!,"background tooltip refresh cannot overwrite pinned skill info");
Check((bool)Call("BookNativeTips",new UICharacterView())!,"other character views retain native tooltip refresh");
for(int repeat=0;repeat<8;repeat++)Call("RefreshBookBindings",view);
Check(sharedPreview.numChildren==0&&view.NativeDetailCalls>=9,"repeated refresh never enters duplicate-holder full-sheet renderer");
Check(model.ListFilterBook.Count==1&&model.ListFilterBook[0].BookId==10,"filter skill by target ID, exclude other skill and language");
Check(view.NativeDetailCalls>0&&view.LastDetail?.BookId==10,"native detail renderer receives exact selected book");
Check(view.SheetSkill.parent!=parent&&view.SheetSkill.Locks==1,"native sheet displayed in popup despite character display gear");
Check(Input("Action_L1",true),"nonmodal page action passes through");
Check(!Input("Action_A",true)&&ctrl.Uses==1&&UICharacterCtrl.Data.PlayerBag.ItemBag.Items[0].Number==2,"confirm key learns selected learnable book: one native use, one quantity consumed");
Check(!Input("Action_A",true)&&ctrl.Uses==1,"held confirm cannot learn twice");
Check(!Input("Action_A",false)&&ctrl.Uses==1,"confirm release is swallowed after learning");
Check(Input("Action_A",true)&&ctrl.Uses==1,"confirm on an already used book keeps original path and never uses it");Input("Action_A",false);
Check(!Learn()&&ctrl.Uses==1,"button path still rechecks same book use history");Input("Action_A",false);Call("TickBooks");
Check(model.Exp==90,"experience refreshed after use");
Input("Action_Start",true);
Check(view.SheetSkill.parent==panel,"Tab no longer closes the popup");Escape();
Check(view.SheetSkill.parent==parent&&!view.SheetSkill.isDisposed&&view.SheetSkill.Locks==0,"close restores native sheet, never disposes it");
Check(view.SheetCharacter.children.OfType<GTextField>().Single(t=>t.text=="nested tooltip text").group==nestedGroup,"nested group membership restored on close");
Check(view.SheetCharacter.texSkillDesc.parent==view.SheetCharacter&&!view.SheetCharacter.texSkillDesc.isDisposed,"native tooltip restores original parent without disposal");
Check(view.SheetSkill.x==150&&view.SheetSkill.y==100&&!view.SheetSkill.visible&&view.SheetSkill.listBook.itemRenderer==renderer,"native geometry, visibility and renderer restored");
Check(view.SheetSkill.width==2100&&view.SheetSkill.height==1200&&view.SheetSkill.listBook.width==originalBookWidth,"compact layout restores full native dimensions");
Check(view.DetailPreview!.isDisposed,"popup-owned preview rows disposed at close");
Check(GRoot.inst.focus==focus,"original GUI focus restored");
Check(Input("Action_Start",false)&&Input("Action_L1",false)&&Input("Action_L1",true),"close release and pre-close held release swallowed; next press restored");
Check(model.CurSelectBook==null&&!model._isSelectBook&&model._bookIndex==-1,"no stale book selection after close");
Check(view.SheetSkill.listBook.Pool.All(o=>o.onClick.Count==0),"pooled slots cannot retain popup callbacks");
ctrl=Fresh(Item(10,1,101));view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);Learn();Input("Action_A",false);
Check(UICharacterCtrl.Data.PlayerBag.ItemBag.Items.Count==0&&view._model.ListFilterBook.Count==0,"last unit removes row and selection");
Check(view.SheetSkill.noBook.selectedIndex==1&&!view.SheetSkill.btnSkillUp.touchable,"empty list cannot learn");Call("CloseBooks");
ctrl=Fresh(Item(90,2,901),Item(10,2,102));view=ctrl._View_k__BackingField;Call("OpenBooks",view,-1);
Check(view._model.ListFilterBook.Count==1&&view._model.ListFilterBook[0].BookType==2,"language entry only offers language books");
Learn();Input("Action_A",false);Check(ctrl.Uses==1&&UICharacterCtrl.Data.PlayerBag.ItemBag.Items[0].Number==1,"language delegates same native use entry");Call("CloseBooks");
ctrl=Fresh(Item(10,2,103));view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);UICharacterCtrl.Data.PlayerCurrency.Currency.Amount=0;
Learn();Input("Action_A",false);Check(ctrl.Uses==0&&view._model.Exp==0,"experience changed after opening is rechecked");
view._model.ListRole[0]=new();Learn();Input("Action_A",false);Check(ctrl.Uses==0,"replacement role cannot receive captured book");Call("TickBooks");
Check(view.SheetSkill.parent==view._UIContent_k__BackingField,"replacement role closes popup");
ctrl=Fresh(Item(10,2,104));view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);UICharacterCtrl.Data.PlayerBag.ItemBag.Items.Clear();Learn();Input("Action_A",false);Check(ctrl.Uses==0,"removed inventory record cannot be consumed");Call("CloseBooks");
ctrl=Fresh(Item(10,1,110),Item(11,1,111));view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);UICharacterCtrl.Data.PlayerBag.ItemBag.Items.RemoveAt(0);Learn();Input("Action_A",false);Check(ctrl.Uses==0&&view.LastDetail?.BookId==11,"stale selection switches preview but must not consume replacement");Call("CloseBooks");
ctrl=Fresh(Item(10,1,105));view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);ctrl.ThrowOnUse=true;Learn();Call("TickBooks");Check(view.SheetSkill.parent==view._UIContent_k__BackingField&&view._model.CurSelectBook==null,"native exception closes popup and clears transient selection");Call("ResetBooks");
ctrl=Fresh(Item(10,1,115));view=ctrl._View_k__BackingField;
var clickable=new GComponent{touchable=false};int originalClicks=0;clickable.onClick.Add((EventCallback1)(Action<EventContext>)(_=>originalClicks++));
Call("BookRenderAfter",view,0,clickable);Call("BookRenderAfter",view,0,clickable);Check(clickable.onClick.Count==2,"one additional listener per character skill row");clickable.onClick.Call();Check(originalClicks==1&&view.NativeDetailCalls>0,"original skill click retained alongside popup");
GRoot.inst.SetSize(1280,720);Call("TickBooks");Check(view.SheetSkill.parent!.parent!.width==1280&&view.SheetSkill.parent.parent.height==720,"resizing keeps backdrop over complete screen");Call("CloseBooks");GRoot.inst.SetSize(1920,1080);
Call("BookRenderBefore",view,0,clickable);Check(clickable.onClick.Count==1&&!clickable.touchable,"re-render removes only own listener and restores touchability");
Call("RefreshBookBindings",view);view.SheetCharacter.texLanguage.onClick.Call();Check(view.SheetSkill.noBook.selectedIndex==1,"language entry opens safely with no language books");Call("ResetBooks");Check(view.SheetCharacter.texLanguage.onClick.Count==0,"reset releases language entry listeners");
ctrl=Fresh(Item(10,1,130));view=ctrl._View_k__BackingField;sharedPreview=view._UIContent_k__BackingField.listSkill;view.ThrowOnDetail=true;
try{Call("OpenBooks",view,0);throw new Exception("expected failure");}catch(TargetInvocationException){}
Check(view._UIContent_k__BackingField.listSkill==sharedPreview,"native detail failure restores shared pointer in finally");Call("CloseBooks");
Check(view.SheetSkill.parent==view._UIContent_k__BackingField&&view.SheetSkill.width==2100,"failure restores borrowed layout");
ctrl=Fresh(Item(10,1,201),Item(20,1,202));view=ctrl._View_k__BackingField;var tipsBeforeCycles=TipState(view.SheetCharacter);
for(int cycle=0;cycle<12;cycle++){Call("OpenBooks",view,cycle%2);Check(TipState(view.SheetCharacter)==tipsBeforeCycles,"native tooltip untouched while pinned");if(cycle%3==0)Escape();else Call("CloseBooks");}
Check(TipState(view.SheetCharacter)==tipsBeforeCycles,"12 open/close cycles leave native tooltip state identical (no cumulative drift)");
Call("OpenBooks",view,0);Call("OpenBooks",view,1);Check(view._model.ListFilterBook.Single().BookId==20&&view.SheetCharacter.texSkillDesc.parent==view.SheetCharacter&&!view.SheetCharacter.texSkillDesc.isDisposed,"clicking another skill replaces panel and preserves native info");Call("CloseBooks");
ctrl=Fresh(Enumerable.Range(10,9).Select((id,i)=>Item(id,1,150+i)).ToArray());view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);
Check(view.SheetSkill.listBook.height==176,"nine compact book slots use two rows instead of original full canvas");Call("CloseBooks");
ctrl=Fresh(Enumerable.Range(10,10).Select((id,i)=>Item(id,1,250+i)).ToArray());view=ctrl._View_k__BackingField;view.SheetSkill.listBook.Factory=()=>new Il2CppCharacter.UIBtn_SkillBook{width=180,height=120};Call("OpenBooks",view,0);Check(view.SheetSkill.listBook.height==3*128,"book viewport is capped at three rows even with five rows of items");Call("CloseBooks");
object Popup()=>typeof(EntryPoint).GetField("books",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
void Spend(){var popup=Popup();popup.GetType().GetMethod("SpendSkillPoint",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(popup,null);}
ctrl=Fresh(Item(10,99,190));view=ctrl._View_k__BackingField;
view.SheetCharacter.texSkillDesc.text=string.Concat(Enumerable.Repeat("긴 스킬 설명과 장비 효과를 함께 표시합니다. ",12));
view.SheetSkill.texBookDesc.text=string.Concat(Enumerable.Repeat("긴 책 설명입니다. ",15));
Call("OpenBooks",view,0);panel=view.SheetSkill.parent!;
var left=view.SheetSkill.listBook.parent!;var right=view.SheetSkill.texBookDesc.parent!;
Check(left!=right&&left.Overflow==OverflowType.Hidden&&right.Overflow==OverflowType.Hidden&&panel.Overflow==OverflowType.Hidden,"each column and outer panel has its own clipping boundary");
Check(view.SheetSkill.texBookDesc.autoSize==AutoSizeType.Height&&!view.SheetSkill.texBookDesc.singleLine&&view.SheetSkill.texBookDesc.width==440,"native description wraps at its column width");
Check(left.children.Where(o=>o.visible).All(o=>o.x>=0&&o.x+o.width<=440)&&right.children.Where(o=>o.visible).All(o=>o.x>=0&&o.x+o.width<=440),"visible controls fit horizontally inside both columns");
Check(left.children.Where(o=>o.visible).All(o=>o.y>=0&&o.y+o.height<=left.height)&&right.children.Where(o=>o.visible).All(o=>o.y>=0&&o.y+o.height<=right.height),"long content contributes to full column height");
Check(panel.children.OfType<GGraph>().Any(o=>o.width==440&&o.height==1),"horizontal divider separates books and selected detail");
var slot=(Il2CppCharacter.UIBtn_SkillBook)view.SheetSkill.listBook.GetChildAt(0);var badge=slot.children.OfType<GTextField>().Single();
Check(badge.text=="x99"&&badge.autoSize==AutoSizeType.Shrink&&badge.singleLine&&badge.visible,"book count uses bag badge text x<count>");
Check(badge.textFormat.font=="SlotFont"&&badge.textFormat.size==13&&badge.textFormat.color==UnityEngine.Color.white&&badge.align==AlignType.Right&&badge.verticalAlign==VertAlignType.Bottom&&badge.stroke==1&&badge.strokeColor==UnityEngine.Color.black&&badge.sortingOrder==int.MaxValue&&!badge.touchable,"book count copies bag badge font/size/colour/stroke/alignment");
Check(Il2CppCommon.UIcompSlotCommon.Created==1,"bag slot template read once and cached");
foreach(float width in new[]{48f,96f,192f})
{
 slot.loaderItem.SetSize(width,width*.8f);Call("RefreshBookBindings",view);
 float inset=Math.Min(14,Math.Min(width,width*.8f)/5);
 Check(Math.Abs(badge.x-(slot.loaderItem.x+inset))<.0001f&&Math.Abs(badge.x+badge.width-(slot.loaderItem.x+width-inset))<.0001f,"count spans illustration width minus bag inset (right aligned)");
 Check(Math.Abs(badge.y+badge.height-(slot.loaderItem.y+slot.loaderItem.height-inset))<.0001f,"count bottom uses bag inset");
 Check(badge.y>=slot.loaderItem.y&&badge.height<=13*1.6f+4+.001f,"count stays inside illustration with bag height rule");
}
Check(view.SheetSkill.texExp.ControllerRefreshes>0,"native state and color controllers replay after details moved to columns");
Check(right.y>left.y+left.height,"action sections never overlap");
var oldBookParent=view.SheetSkill;Call("CloseBooks");
Check(view.SheetSkill.listBook.parent==oldBookParent&&view.SheetSkill.texBookDesc.parent==oldBookParent&&view.SheetSkill.texBookDesc.autoSize==AutoSizeType.Both&&view.SheetSkill.texBookDesc.singleLine,"native parents and text sizing restored after reparented columns");
Call("OpenBooks",view,0);Mouse.current.rightButton.wasPressedThisFrame=true;Mouse.current.rightButton.isPressed=true;Call("TickBooks");Mouse.current.rightButton.wasPressedThisFrame=false;
Check(view.SheetSkill.parent==view._UIContent_k__BackingField,"physical right mouse closes popup without InputAction name");
UnityEngine.Time.frameCount+=10;Call("TickBooks");Check(!(bool)Call("BookCapture",new InputAction.CallbackContext{action=new(){name="Action_B"},canceled=true})!,"holding right mouse continues to suppress background close actions");Mouse.current.rightButton.isPressed=false;UnityEngine.Time.frameCount+=3;
// 0.6.10 regression: after a right-click close, other actions pass even while right mouse is held again.
Call("OpenBooks",view,0);Mouse.current.rightButton.wasPressedThisFrame=true;Mouse.current.rightButton.isPressed=true;Call("TickBooks");Mouse.current.rightButton.wasPressedThisFrame=false;
Check((bool)Call("BookCapture",new InputAction.CallbackContext{action=new(){name="SailUp"},performed=true,Pressed=true})!,"other actions pass while the closing right click is still held");
Check(!(bool)Call("BookCapture",new InputAction.CallbackContext{action=new(){name="Action_B"},performed=true,Pressed=true})!,"closing key's own Action_B is swallowed while held");
Mouse.current.rightButton.isPressed=false;
Check(!(bool)Call("BookCapture",new InputAction.CallbackContext{action=new(){name="Action_B"},canceled=true})!,"closing key's own release is swallowed once");
Check((bool)Call("BookCapture",new InputAction.CallbackContext{action=new(){name="Action_B"},canceled=true})!,"later Action_B release passes");
Mouse.current.rightButton.isPressed=true;UnityEngine.Time.frameCount+=100;Call("TickBooks");
Check((bool)Call("BookCapture",new InputAction.CallbackContext{action=new(){name="SailUp"},performed=true,Pressed=true})!,"a later held right click never blocks keyboard actions (0.6.9 bug)");
Check((bool)Call("BookCapture",new InputAction.CallbackContext{action=new(){name="Search"},performed=true,Pressed=true})!,"Alt search passes with right mouse held");
Check((bool)Call("BookCapture",new InputAction.CallbackContext{action=new(){name="Action_B"},performed=true,Pressed=true})!,"a later right click Action_B reaches the game");
Mouse.current.rightButton.isPressed=false;UnityEngine.Time.frameCount+=3;
ctrl=Fresh();view=ctrl._View_k__BackingField;var role=view._model.ListRole[0].HeroData;Call("RefreshBookBindings",view);
var footer=GRoot.inst.children.OfType<GTextField>().Single(o=>o.sortingOrder==31900);
Check(footer.visible&&footer.text.EndsWith("3"),"selected character points shown in character footer");
Call("OpenBooks",view,0);Check(Descendants(view.SheetSkill.parent!).OfType<GTextField>().Any(o=>o.text.Contains("스킬 레벨 +1")&&o.visible),"point upgrade exists even without related books");
Spend();Check(role.SkillPoints==2&&role.GetRoleSkillById(1)!.Level==8&&UICharacterCtrl.Data.PlayerRole.Calls==1,"one native DB call raises selected skill and spends one point");
Spend();Check(UICharacterCtrl.Data.PlayerRole.Calls==1,"duplicate same-frame point click cannot consume twice");
Check(UICharacterCtrl.Data.PlayerCurrency.Currency.Amount==100&&UICharacterCtrl.Data.PlayerBag.ItemBag.Items.Count==0,"point upgrade does not spend experience or books");
Check(footer.text.EndsWith("2"),"point footer immediately reflects consumed point");
var nativeTips=Il2CppClient.UILogic.UITips.UITipsCtrl.Instance;
Check(nativeTips.Calls==1&&nativeTips.LastText.Contains("스킬 레벨이 1 상승"),"point result uses original tips controller");
Check(nativeTips._View_k__BackingField.AniTips.timeScale==2&&nativeTips._View_k__BackingField._UIContent_k__BackingField.sortingOrder==32760,"native animation doubled and native layer raised");
UnityEngine.Time.frameCount++;Spend();UnityEngine.Time.frameCount++;Spend();Check(role.GetRoleSkillById(1)!.Level==9&&role.SkillPoints==1&&UICharacterCtrl.Data.PlayerRole.Calls==2,"native maximum level prevents further point spend");
role.GetRoleSkillById(1)!.Level=7;role.SkillPoints=0;UnityEngine.Time.frameCount++;Spend();Check(UICharacterCtrl.Data.PlayerRole.Calls==2,"insufficient points never reach unsafe native DB function");
role.SkillPoints=2;UICharacterCtrl.Data.PlayerRole.Replacement=new();UnityEngine.Time.frameCount++;Spend();Check(UICharacterCtrl.Data.PlayerRole.Calls==2,"replaced DB role cannot receive stale selection");UICharacterCtrl.Data.PlayerRole.Replacement=null;
var popupNow=Popup();popupNow.GetType().GetField("Committing",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(popupNow,true);
Check((bool)Call("BookTips",nativeTips)!,"learning native message is never intercepted or replaced");
nativeTips._View_k__BackingField.AniTips.playing=true;Call("BookTipsAfter");
popupNow.GetType().GetField("Committing",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(popupNow,false);
Call("CloseBooks");Check(nativeTips._View_k__BackingField.AniTips.timeScale==2,"native notice continues at double speed after panel closes");
nativeTips._View_k__BackingField.AniTips.playing=false;Call("TickBooks");
Check(nativeTips._View_k__BackingField.AniTips.timeScale==1&&nativeTips._View_k__BackingField._UIContent_k__BackingField.sortingOrder==0,"native speed and layer restored on completion");
Check((bool)Call("BookTips",nativeTips)!,"unrelated native messages retain original path");Call("BookTipsAfter");
Check(nativeTips._View_k__BackingField.AniTips.timeScale==1,"unrelated native message has normal speed");
view._model.SheetType=ESheetType.Skill;Call("TickBooks");Check(!GRoot.inst.children.Any(o=>o.sortingOrder==31900),"point footer removed outside character tab");
ctrl=Fresh();view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);
var interaction=Popup();var it=interaction.GetType();
GObject Field(string name)=>(GObject)it.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(interaction)!;
var button=Field("pointButton");var dismiss=Field("outsideDismiss");
void Pointer(GObject target,float x=4,float y=4){var p=target.LocalToGlobal(new UnityEngine.Rect(x,y,0,0));Stage.inst.touchPosition=new(p.x,p.y);}
var actionPanel=Field("panel");float savedHeight=actionPanel.height;
actionPanel.height+=100;Call("TickBooks");float stableY=actionPanel.y;
actionPanel.height=savedHeight;Call("TickBooks");Check(actionPanel.y==stableY,"shorter learning content cannot shift panel down under repeated pointer clicks");
Pointer(button);button.onRollOver.Call();button.onTouchBegin.Call();
Check(UICharacterCtrl.Data.PlayerRole.Calls==0,"press does not spend point");
Check(((List<GGraph>)it.GetField("pointGradient",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(interaction)!).All(x=>x.visible),"blue gradient responds to hover and press");
button.onRollOut.Call();button.onRollOver.Call();button.onTouchEnd.Call();Check(UICharacterCtrl.Data.PlayerRole.Calls==0,"drag out then back in cancels entire gesture");
button.onTouchBegin.Call();button.onTouchEnd.Call();Check(UICharacterCtrl.Data.PlayerRole.Calls==0,"release queues commit until pointer dispatch has ended");UnityEngine.Time.frameCount++;Call("TickBooks");Check(UICharacterCtrl.Data.PlayerRole.Calls==1,"release inside applies once");
button.onClick.Call();Check(UICharacterCtrl.Data.PlayerRole.Calls==1,"subsequent click event cannot spend twice");
Pointer(Field("panel"));dismiss.onTouchBegin.Call();Check(Popup()==interaction,"left click inside action panel cannot dismiss");
Pointer(Field("infoPane"));dismiss.onTouchBegin.Call();Check(Popup()==interaction,"left click inside info panel cannot dismiss");
Stage.inst.touchPosition=new(-10,-10);dismiss.onTouchBegin.Call();Check(Popup()==interaction,"outside press alone cannot close panel");
Pointer(Field("panel"));dismiss.onTouchEnd.Call();Check(Popup()==interaction,"outside-to-inside gesture cannot close panel");
Stage.inst.touchPosition=new(-10,-10);dismiss.onTouchBegin.Call();dismiss.onTouchEnd.Call();
Check(view.SheetSkill.parent==view._UIContent_k__BackingField&&Stage.inst.Cancelled>0,"outside left press dismisses and cancels background click");
ctrl=Fresh();view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);interaction=Popup();it=interaction.GetType();button=Field("pointButton");Pointer(button);button.onTouchBegin.Call();button.onTouchEnd.Call();Call("CloseBooks");UnityEngine.Time.frameCount++;Call("TickBooks");Check(UICharacterCtrl.Data.PlayerRole.Calls==0,"closing before deferred commit cancels pending point request");
void Space(bool down,bool pressedNow){Keyboard.current.spaceKey.isPressed=down;Keyboard.current.spaceKey.wasPressedThisFrame=pressedNow;UnityEngine.Time.frameCount++;Call("TickBooks");Keyboard.current.spaceKey.wasPressedThisFrame=false;}
ctrl=Fresh(Item(10,1,170),Item(11,2,171));view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);
Space(true,true);Check(ctrl.Uses==1&&UICharacterCtrl.Data.PlayerBag.ItemBag.Items.All(i=>i.ItemId!=10),"physical Space learns selected learnable book once");
Check(view._model.ListFilterBook.Count==1&&view._model.ListFilterBook[0].BookId==11,"last copy removed; next learnable book becomes selected");
Space(true,false);Check(ctrl.Uses==1,"holding Space cannot learn the newly selected book");
Check(Input("Action_A",true)&&ctrl.Uses==1,"Action_A from the same held press cannot learn again");Input("Action_A",false);
Space(false,false);Space(true,true);Check(ctrl.Uses==2,"releasing and pressing Space again learns the next book");Space(false,false);Call("CloseBooks");
ctrl=Fresh(Item(10,2,172));view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);Stage.inst.focus=new InputTextField();
Space(true,true);Check(ctrl.Uses==0,"Space typed into a text field never learns");Stage.inst.focus=null;Space(false,false);
view._model.ListRole[0].HeroData.Learned.Add(10);Call("RefreshBookBindings",view);Space(true,true);Check(ctrl.Uses==0,"Space on an unlearnable book does nothing");Space(false,false);Call("CloseBooks");
Space(true,true);Check(ctrl.Uses==0,"Space with the panel closed does nothing");Space(false,false);
ctrl=Fresh(Item(10,2,160));view=ctrl._View_k__BackingField;var nativeLearn=view.SheetSkill.btnSkillUp;nativeLearn.SetSize(300,50);var learnParent=nativeLearn.parent;
Call("OpenBooks",view,0);interaction=Popup();it=interaction.GetType();
Check(nativeLearn.visible&&nativeLearn.parent!=learnParent&&nativeLearn.width==300&&nativeLearn.height==50&&nativeLearn.scaleX==1&&nativeLearn.x==70,"native learn button keeps its own size and is centred in the column");
Check(!Descendants(nativeLearn.parent!).OfType<GTextField>().Any(t=>t.text=="스킬 배우기"),"no replacement label drawn over native button");
float learnRestX=nativeLearn.x,learnRestY=nativeLearn.y;
Pointer(nativeLearn,150,25);nativeLearn.onTouchBegin.Call();
Check(Math.Abs(nativeLearn.scaleX-.95f)<.0001f&&Math.Abs(nativeLearn.x-(learnRestX+300*.025f))<.001f&&Math.Abs(nativeLearn.y-(learnRestY+50*.025f))<.001f,"press shrinks native button to 95% around its centre");
nativeLearn.onTouchEnd.Call();Check(nativeLearn.scaleX==1&&nativeLearn.x==learnRestX&&nativeLearn.y==learnRestY,"release restores native button");
nativeLearn.onTouchBegin.Call();nativeLearn.onRollOut.Call();Check(nativeLearn.scaleX==1&&nativeLearn.x==learnRestX,"dragging out clears pressed state");
nativeLearn.onTouchBegin.Call();Stage.inst.touchPosition=new(-50,-50);nativeLearn.onTouchMove.Call();Check(nativeLearn.scaleX==1,"moving outside while held clears pressed state");
Check(ctrl.Uses==0,"press feedback alone never learns");Check(!Learn()&&ctrl.Uses==1,"native click path still learns once");
Pointer(nativeLearn,150,25);nativeLearn.onTouchBegin.Call();Call("CloseBooks");
Check(nativeLearn.parent==learnParent&&nativeLearn.scaleX==1&&nativeLearn.width==300&&nativeLearn.height==50&&nativeLearn.onTouchBegin.Count==0&&nativeLearn.onTouchEnd.Count==0&&nativeLearn.onRollOut.Count==0&&nativeLearn.onTouchMove.Count==0,"close while pressed restores native button and removes press listeners");
var levelCtrl=new Il2CppClient.UILogic.UIHeroLevelUp.UIHeroLevelUpCtrl();var lv=levelCtrl._View_k__BackingField;int completions=0;
lv.ComSkillLabel.AniEnter.Completed=()=>{completions++;lv._model.IsRewardSkill=false;lv._model.IsSpecialLevelUp=false;};
lv._model.IsRewardSkill=true;lv.ComSkillLabel.AniEnter.playing=true;Call("RewardAfter",levelCtrl);
Check(!lv.ComSkillLabel.touchable,"reward notice does not intercept next mouse input");
lv._model.BanTouch=true;Call("AdvancePastReward",levelCtrl);Check(completions==0,"unfinished level transaction cannot be bypassed");
lv._model.BanTouch=false;lv._model.ResultAniCount=1;Call("AdvancePastReward",levelCtrl);Check(completions==0,"pending stat animation/settlement remains protected");
lv._model.ResultAniCount=0;Call("AdvancePastReward",levelCtrl);
Check(completions==1&&!lv._model.IsRewardSkill&&lv.ComSkillLabel.touchable,"next level request completes only reward animation via native callback and restores input");
Call("AdvancePastReward",levelCtrl);Check(completions==1,"controller and action hooks cannot complete same reward twice");
lv._model.IsRewardSkill=true;lv.ComSkillLabel.AniEnter.playing=true;lv.IsInputActive=false;Call("AdvancePastReward",levelCtrl);Check(completions==1,"inactive level view is unchanged");
for(int type=1;type<=2;type++)for(int id=1;id<=20;id++)for(int selected=-1;selected<=20;selected++)Check(BookRules.Matches(type,id,selected)==(selected<0?type==2:type==1&&id==selected),"skill/language filter matrix");
for(int direction=0;direction<2;direction++)foreach(float w in new[]{640f,1280f,1920f,2560f})foreach(float h in new[]{720f,1080f,1440f})foreach(float ax in new[]{0f,w/2,w-10})foreach(float ay in new[]{0f,h/2,h-10})foreach(float contentHeight in new[]{300f,1200f})
{
 var p=SidePanelPlacement.Calculate(w,h,ax,ay,500,400,488,contentHeight,direction==1);
 Check(p.Scale>0&&p.Scale<=1,"pair never enlarges native widgets");
 Check(p.InfoX>=0&&p.ActionsX>=0&&p.Y>=0&&p.InfoX+500*p.Scale<=w+.001&&p.ActionsX+488*p.Scale<=w+.001&&p.Y+Math.Max(400,contentHeight)*p.Scale<=h+.001,"pair fits screen at all edges");
 Check(direction==1?p.InfoX+500*p.Scale<p.ActionsX:p.ActionsX+488*p.Scale<p.InfoX,"ordered pair retains a visible gap");
}
foreach(bool preferRight in new[]{false,true})foreach(float w in new[]{1280f,1920f,2560f})foreach(float h in new[]{720f,1080f,1440f})foreach(float ax in new[]{0f,w*.2f,w/2,w-600})foreach(float aw in new[]{300f,600f})foreach(float ph in new[]{400f,1200f})
{
 var bp=SidePanelPlacement.Beside(w,h,ax,h*.3f,aw,488,ph,preferRight);
 Check(bp.Scale>0&&bp.Scale<=1&&bp.X>=0&&bp.Y>=0&&bp.X+488*bp.Scale<=w+.001&&bp.Y+ph*bp.Scale<=h+.001,"beside panel fits screen");
 Check(bp.Overlaps||bp.X+488*bp.Scale<=ax+.001||bp.X>=ax+aw-.001,"beside panel never covers native tooltip when a side has room");
 Check(bp.Overlaps||bp.Scale>=.5f,"side placement keeps at least half scale");
}
// Reproduce native detached/FeatureUI-attached alternation: never promote the parent.
ctrl=Fresh();view=ctrl._View_k__BackingField;Call("OpenBooks",view,0);
var ownerPopup=Popup();var committing=ownerPopup.GetType().GetField("Committing",BindingFlags.Instance|BindingFlags.NonPublic)!;
var feature=new GComponent{name="FeatureUI"};GRoot.inst.AddChild(feature);
var sibling=new GComponent();feature.AddChild(sibling);
var tipContent=nativeTips._View_k__BackingField._UIContent_k__BackingField;
var animation=nativeTips._View_k__BackingField.AniTips;
for(int attempt=0;attempt<4;attempt++)
{
 Call("ClearLearningNotice");tipContent.RemoveFromParent();tipContent.sortingOrder=7;
 animation.playing=false;animation._options=8;
 if(attempt%2==0)feature.AddChild(tipContent);
 committing.SetValue(ownerPopup,true);Call("BookTips",nativeTips);committing.SetValue(ownerPopup,false);
 Call("BookTipsAfter");
 Check(feature.sortingOrder==0&&tipContent.sortingOrder==7,"pending native transition never changes layer order");
 if(tipContent.parent==null)feature.AddChild(tipContent);
 animation.playing=true;Call("TickLearningNotice");
 Check(tipContent.parent==GRoot.inst&&tipContent.sortingOrder==32760&&!tipContent.touchable,"only native notice lifted, input remains pass-through");
 Check(feature.parent==GRoot.inst&&feature.sortingOrder==0&&sibling.parent==feature,"shared parent and siblings unchanged");
 Check(animation.playing&&animation._options==8&&animation.timeScale==2,"native animation and option restoration retained");
 if(attempt==3)tipContent.RemoveFromParent(); // Native Hide already removed it.
 animation.playing=false;Call("TickLearningNotice");
 Check(tipContent.parent==(attempt==3?null:feature)&&tipContent.sortingOrder==7&&tipContent.touchable,"restore parent unless native already detached notice");
 Check(animation.timeScale==1,"completion restores original animation speed");
}
Call("CloseBooks");feature.Dispose();tipContent.RemoveFromParent();
// Observe root ordering without repairing/hiding/reparenting objects.
Call("ClearUiTrace");EntryPoint.Log.Lines.Clear();
var probe=new GComponent{name="trace-probe",sortingOrder=25000};GRoot.inst.AddChild(probe);
var originalParent=probe.parent;
Call("TraceUi","point-before",true);
int traceCount=EntryPoint.Log.Lines.Count;
Call("TickUiTrace");Check(EntryPoint.Log.Lines.Count==traceCount,"identical frame state not repeated");
probe.sortingOrder=32760;Call("TickUiTrace");
Check(EntryPoint.Log.Lines.Count==traceCount+1&&EntryPoint.Log.Lines.Last().Contains("trace-probe"),"root order change captured");
Check(probe.parent==originalParent&&probe.visible&&probe.sortingOrder==32760,"diagnostics do not mutate UI");
for(int n=0;n<220;n++){probe.visible=!probe.visible;Call("TickUiTrace");}
Check(EntryPoint.Log.Lines.Count==181,"capture bounded to 180 snapshots plus limit marker");
Call("TraceUi","point-before",true);Check(EntryPoint.Log.Lines.Count==182,"next point restarts capture");
UnityEngine.Time.unscaledTime+=16;Call("TickUiTrace");
Check((float)typeof(EntryPoint).GetField("traceUntil",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)! == 0,"capture expires");
probe.Dispose();
Console.WriteLine($"PASS {count} popup adapter checks against production BookPopup/BookRules using native API doubles. No native UI/game code executed.");




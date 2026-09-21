using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UILandExplore;
using Il2CppClient.UILogic.UICommonInputNum;
using Il2CppClient.UILogic.UIBag;
using MelonLoader.Utils;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
 public static bool TestPopupSet(UICommonInputNumModel m,long n)=>PopupSet(m,n);
 public static void TestPopupOpening(UICommonInputNumCtrl c)=>PopupOpening(c);
 public static void TestPopupReturn(UICommonInputNumCtrl c)=>PopupReturn(c);
 public static void TestPopupClosed(UICommonInputNumCtrl c)=>PopupClosed(c);
 static int checks;
 static void Check(bool b,string why) { checks++; if(!b)throw new Exception(why); }
 static PlayerItemData Item(int id,int n,long guid=1)=>new(){ItemId=id,Number=n,Guid=guid};
 static void SetBag(params PlayerItemData[] xs)
 {
  Reset(); var db=new PlayerBagDB(); db.ItemBag.Items.AddRange(xs); Bind(db);
  Il2CppClient.Manager.PlayerDataManager.Instance.Data.PlayerBag=db;
 }
 public static void Main()
 {
  MelonEnvironment.UserDataDirectory=Path.Combine(AppContext.BaseDirectory,"test-userdata",DateTime.UtcNow.Ticks.ToString());
  enabled=true;thread=Environment.CurrentManagedThreadId;log=new();runtime=new();
  // 0.1.23: registration goes through Restitutor.Core; the game counts as ready (first scene-entered frame).
  runtime.hooks=new Restitutor.Core.HookSet(runtime.HarmonyInstance,typeof(EntryPoint));
  Il2CppCore.SceneSystem.SceneManager.Instance=new();MelonLoader.MelonEvents.OnUpdate.Invoke();
  Check(Restitutor.Core.GameReady.IsReady,"Core game-ready gate open for deferred UIManager hook");
  using(var historical=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"saved-items.json"))))
  {
   var xs=historical.RootElement.EnumerateArray().Select(e=>new PlayerItemData {
    ItemId=e.GetProperty("itemId").GetInt32(),Number=e.GetProperty("number").GetInt32(),Guid=e.GetProperty("guid").GetInt64(),
    BuyPrice=e.GetProperty("buyPrice").GetInt32(),Supply=e.GetProperty("supply").GetInt32(),Quality=e.GetProperty("quality").GetInt32(),
    IsSuper=e.GetProperty("isSuper").GetInt32()!=0,IsCommerce=e.GetProperty("isCommerce").GetInt32()!=0,state=(Il2CppClient.Const.ECargoState)e.GetProperty("state").GetInt32()
   }).ToArray();
   var totals=xs.GroupBy(x=>x.ItemId).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.Number));
   var untouched=xs.Where(x=>!Eligible(x)).ToArray();SetBag(xs);Compact(bag);
   foreach(var pair in totals) Check(bag!.GetItemAmount(pair.Key)==pair.Value,"historical save quantity conservation");
   Check(bag!.Items.Where(x=>!Eligible(x)).SequenceEqual(untouched),"historical non-target identity/order preserved");
   Check(bag.Items.Count(x=>Eligible(x)&&x.ItemId==36000)==1&&bag.GetItemAmount(36000)==17,"historical 17 lanterns into one");
   Console.WriteLine($"Historical 2026-09-18 snapshot: {xs.Length} inventory records -> {bag.Items.Count}; every item total preserved.");
  }
  // Existing save migration: quantities, identity, order, excluded records and idempotency.
  var lantern=Item(36000,1,101);lantern.Quality=4;lantern.BuyPrice=90;lantern.IsSuper=true;
  var book=Item(31000,2,103); var trade=Item(36000,9,104);trade.IsCommerce=true;
  SetBag(lantern,Item(36000,7,102),book,trade,Item(36013,2,105));Compact(bag);
  Check(bag!.Items.Count==4&&lantern.Number==8&&lantern.Guid==101,"migration quantity/representative guid");
  Check(lantern.Quality==0&&lantern.BuyPrice==0&&!lantern.IsSuper,"normalization");
  Check(ReferenceEquals(bag.Items[1],book)&&trade.Number==9,"non-target order/state");
  Check(owner!.Dirty==1,"save dirty");Compact(bag);Check(owner.Dirty==1,"idempotent");
  Check(Directory.GetFiles(MelonEnvironment.UserDataDirectory,"*.json",SearchOption.AllDirectories).Length==2,"backup before merge");
  // Overflow is detected before writes.
  SetBag(Item(36000,int.MaxValue),Item(36000,1));
  try{Compact(bag);throw new Exception("missing overflow");}catch(OverflowException){}
  Check(bag!.Items.Count==2&&bag.Items[0].Number==int.MaxValue,"overflow no writes");
  // Backup failure also prevents writes.
  string valid=MelonEnvironment.UserDataDirectory; string blocked=Path.Combine(valid,"blocked");File.WriteAllText(blocked,"x");MelonEnvironment.UserDataDirectory=blocked;
  SetBag(Item(36000,2,201),Item(36000,3,202));
  try{Compact(bag);throw new Exception("missing backup error");}catch(IOException){}
  Check(bag!.Items.Count==2&&bag.Items[0].Number==2,"backup failure no writes");MelonEnvironment.UserDataDirectory=valid;
  // Native add adapter: the original sees one (pileNum remains one), then requested count is consolidated.
  for(int old=0;old<8;old++) for(int qty=1;qty<8;qty++)
  {
   SetBag(old>0?new[]{Item(36000,old,300)}:Array.Empty<PlayerItemData>());
   int amount=qty;bool limit=true,result=false;
   Check(AddBegin(bag!,36000,ref amount,ref limit,ref result,out var state)&&amount==1,"native add one");
   Check(limit==(old==0),"existing kind bypass capacity only");
   bag!.Items.Add(Item(36000,1,301));AddEnd(bag,true,state);
   Check(bag.Items.Count==1&&bag.Items[0].Number==old+qty,"add conservation");
  }
  SetBag(Item(36000,5));int a=2;bool l=true,r=false;AddBegin(bag!,36000,ref a,ref l,ref r,out var failed);
  bag!.Items.Add(Item(36000,1,2));bag.Items[0].Number=100;var fault=new Exception("injected");
  Check(ReferenceEquals(AddFinal(fault,failed),fault)&&bag.Items.Count==1&&bag.Items[0].Number==5,"add exception rollback");
  Check(!RemoveGuid(bag,1,ref r)&&r&&bag.Items[0].Number==4,"guid removes one");
  Check(!RemoveIndex(bag,0,ref r)&&bag.Items[0].Number==3,"index removes one");
  var indices=new Il2CppSystem.Collections.Generic.List<int>{0,0};RemoveMany(bag,ref indices);
  Check(indices.Count==0&&bag.Items[0].Number==2,"batch dedup one unit");
  bag.Items[0].Number=1;Check(RemoveGuid(bag,1,ref r),"last unit native deletion");
  var alien=new ItemBagData();alien.Items.Add(Item(36000,9));Check(RemoveIndex(alien,0,ref r)&&alien.Items[0].Number==9,"non-player bag untouched");
  foreach(int id in new[]{1,31000,36008,36014,2000})Check(!Rules.Target(id),"target whitelist");
  // Exact cap, stock and weight properties over a range of selections.
  for(int stock=0;stock<20;stock++)for(int requested=-2;requested<30;requested++)for(int capacity=0;capacity<5;capacity++)
   Check(Rules.Select(requested,stock,2,capacity*2)==Math.Min(Math.Max(0,requested),Math.Min(3,Math.Min(stock,capacity))),"selection cap/weight");
  Check(Rules.Select(3,9,.1f,.3f)==3,"float tolerance");
  // Actual production callbacks, popup ownership, deferred commit, weight and departure adapter.
  SetBag(Item(36000,8)); var c=new UILandExploreCtrl();var m=c.Model;
  // Native ShowHook adds a null supply sentinel at index zero.
  m.ListLandBag.Add(null!);
  var g=new Goods{GoodsID=36000,Name="Lantern",Heavy=2};m.ListDeport.Add(g);m.CurGoods=g;
  LandBuilt(c);Check(!popupBoundaryReady&&HarmonyLib.Harmony.Registrations==0,"no UIManager hook during inventory/land setup");
  var supplyOnly=m.ListLandBag;JumpBegin(c,out var supplyState);
  Check(m.ListLandBag.Count==1&&m.ListLandBag[0]==null,"supply-only departure retains sentinel");JumpFinal(null,supplyState);
  Check(ReferenceEquals(m.ListLandBag,supplyOnly),"supply-only departure restores original list");
  Check(!ChooseLand(c),"owned click consumed");var p=UICommonInputNumCtrl.Instance;
  Check(popupBoundaryReady&&HarmonyLib.Harmony.Registrations==1,"deferred hook at first owned popup");
  Check(p.Model.CurNum==1&&m.ListLandBag.Count==1&&m.ListLandBag[0]==null,"popup deferred, supply sentinel retained");PopupAdd(p);Check(p.Model.CurNum==2,"plus one");
  PopupAdd(p);PopupAdd(p);Check(p.Model.CurNum==0,"plus wraps max to zero");PopupReduce(p);Check(p.Model.CurNum==3,"minus wraps zero to max");PopupReduce(p);Check(p.Model.CurNum==2,"minus one");
  p.Model.CurNum=long.MaxValue;Check(p.Model.CurNum==3,"typed max clamp");PopupApplyAction(p);
  Check(m.ListLandBag.Count==2&&m.ListLandBag[0]==null&&m.ItemWeight==6&&g.Heavy==6&&landRows[36000].Selected==3,"commit count and weight, supply sentinel retained");
  var landView=new UILandExploreView{_model=m};var landButton=new Il2CppUILandExplore.UIButtonItem();
  landButton.texCount.text="supply";CarryLabel(landView,0,landButton);
  Check(landButton.texCount.text=="supply","supply slot left to native renderer");
  CarryLabel(landView,1,landButton);Check(landButton.texCount.text=="X3","actual carried item label");
  Check(IndexOf(m.ListLandBag,g.Pointer)==1&&IndexOf(m.ListLandBag,new IntPtr(-1))==-1,"lookup skips supply sentinel");
  Check(IndexOf(null,g.Pointer)==-1,"missing list safe lookup");
  m.ListLandBag.Add(null!);CarryLabel(landView,2,landButton);
  Check(landButton.texCount.text=="X3","extra empty row ignored");m.ListLandBag.RemoveAt(2);
  var missingCount=new Il2CppUILandExplore.UIButtonItem();missingCount.texCount=null!;CarryLabel(landView,1,missingCount);
  Check(missingCount.title=="","missing UI count component is ignored");
  var original=m.ListLandBag;JumpBegin(c,out var js);
  Check(m.ListLandBag.Count==4&&m.ListLandBag[0]==null&&m.ListLandBag.Skip(1).All(x=>x.Heavy==2&&x.GoodsID==36000),"departure expansion preserves supply sentinel");
  Check(ReferenceEquals(JumpFinal(fault,js),fault)&&ReferenceEquals(m.ListLandBag,original),"departure list restored even on fault");
  ChooseLand(c);p.Model.CurNum=-500;PopupApplyAction(p);Check(m.ListLandBag.Count==1&&m.ListLandBag[0]==null&&m.ItemWeight==0,"zero clears only items");
  m.MaxWeight=3;ChooseLand(c);p.Model.CurNum=3;PopupApplyAction(p);Check(landRows[36000].Selected==1&&m.ItemWeight==2,"weight cap on commit");
  ChooseLand(c);p.Model.CurNum=0;OtherPopup();Check(popup==null&&landRows[36000].Selected==1,"other popup cancels pending edit");
  Check(PopupAdd(p)&&PopupReduce(p)&&PopupSet(p.Model,100)&&PopupValue(p),"other numeric popup untouched");
  ChooseLand(c);p.Model.CurNum=0;var stale=chooseCallback;ResetLand();stale!.Invoke(3);
  Check(popup==null&&landRows.Count==0&&m.ListLandBag.Count==2,"close/reset cancels stale callback");
  // A single owned item still opens the popup, limited to one.
  SetBag(Item(36000,1));c=new();c.Model.ListLandBag.Add(null!);c.Model.ListDeport.Add(g);c.Model.CurGoods=g;LandBuilt(c);ChooseLand(c);p.Model.CurNum=3;
  Check(p.Model.CurNum==1,"stock one popup cap");PopupApplyAction(p);
  // Pooled bag labels restore without accumulating suffixes, including after reset.
  var v=new UIBagView();var group=new BagGroup();group.ItemList.Add(new BagRow{GlobalIndex=0});v._model.BagGroupList.Add(group);
  var slot=new Il2CppBag.UIbtnSlot();slot.compSlotCommon.title.text="original";
  var common=slot.compSlotCommon;common.SetXY(30,40);common.scaleX=2;common.scaleY=2;
  common.loaderIcon.SetXY(10,12);common.loaderIcon.SetSize(80,70);
  Il2CppFairyGUI.GObject.RejectGlobalTransforms=true;
  BagLabel(v,0,slot);BagLabel(v,0,slot);var badge=bagBadges[common.Pointer];
  Check(badge.Label.text=="x1"&&badge.Label.textFormat.size==22&&badge.Label.textFormat.color.value==1&&badge.Label.stroke==1&&badge.Label.strokeColor.value==0&&badge.Label.align==Il2CppFairyGUI.AlignType.Right&&badge.Label.verticalAlign==Il2CppFairyGUI.VertAlignType.Bottom&&!common.title.visible,"badge separate from original package title");
  Check(badge.Label.x==24&&badge.Label.y+badge.Label.height==68&&badge.Label.x+badge.Label.width==76,"badge inside actual icon bottom-right with inset under scaled parent");
  Check(common.Children.Count==3&&!badge.Label.touchable&&badge.Label.sortingOrder==int.MaxValue,"one non-interactive badge after rerender");
  // Purchase adapter -> same representative identity -> visible quantity without rerender.
  int purchased=7;bool purchaseLimit=true,ok=false;AddBegin(bag!,36000,ref purchased,ref purchaseLimit,ref ok,out var purchaseState);
  bag!.Items.Add(Item(36000,1,909));AddEnd(bag,true,purchaseState);RefreshBagBadges();
  Check(badge.Label.text=="x8","purchase synchronized without slot rerender");
  RemoveGuid(bag,bag.Items[0].Guid,ref ok);RefreshBagBadges();Check(badge.Label.text=="x7","single consumption synchronized");
  CountRemoving(bag,36000,out var countState);bag.Items[0].Number-=3;CountRemoved(countState);RefreshBagBadges();Check(badge.Label.text=="x4","native count-based consumption reflected");
  // Sorting/insertion must never transfer a label to an unrelated record at the old index.
  bag.Items.Insert(0,Item(30000,99,777));RefreshBagBadges();Check(badge.Label.text=="x4","quantity identity survives inventory index shifts");
  CountRemoving(bag,36000,out countState);bag.Items[1].Number=0;bag.Items.RemoveAt(1);CountRemoved(countState);RefreshBagBadges();Check(!badge.Label.visible,"last-unit removal hides stale quantity");
  BagLabel(v,0,slot);Check(common.title.visible&&common.Children.Count==2&&!bagBadges.ContainsKey(common.Pointer),"non-target pooled slot removes badge and restores title");
  bag.Items[0]=Item(36000,2,910);BagLabel(v,0,slot);Check(bagBadges[common.Pointer].Label.text=="x2","rebuy new identity renders new count");
  common.onStage=false;ClearBagBadges();Check(bagBadges.Count==0&&common.title.visible,"hidden bag releases overlays");common.onStage=true;
  BagLabel(v,0,slot);
  badge=bagBadges[common.Pointer];
  var otherSlot=new Il2CppBag.UIbtnSlot();bag.Items.Add(Item(36001,10,911));group.ItemList.Add(new BagRow{GlobalIndex=1,ItemTemplate=new Template{tid=36001}});BagLabel(v,1,otherSlot);
  var otherBadge=bagBadges[otherSlot.compSlotCommon.Pointer];
  var runner=new EntryPoint();
  for(int frame=0;frame<10000;frame++) runner.OnUpdate();
  Check(dirtyBadges.Count==0&&badge.Label.text=="x2"&&otherBadge.Label.text=="x10","idle frames do not enqueue or update inventory");
  CountRemoving(bag,36000,out countState);CountRemoved(countState);
  Check(dirtyBadges.Count==0,"failed/no-op count removal causes no redraw");
  RemoveGuidBegin(bag,910,ref ok,out var deleteState);RemoveOneEnd(ok,deleteState);
  Check(dirtyBadges.Count==1&&dirtyBadges.Contains(badge),"only changed identity is queued");
  CountRemoving(bag,36000,out countState);bag.Items[0].Number=5;CountRemoved(countState);
  Check(dirtyBadges.Count==1&&badge.Label.text=="x2","same-frame changes coalesce and defer UI work");
  runner.OnUpdate();Check(badge.Label.text=="x5"&&otherBadge.Label.text=="x10"&&dirtyBadges.Count==0,"flush reads final count of changed record only");
  bag.Items[0].Number=1;RemoveIndexBegin(bag,0,ref ok,out deleteState);RemoveOneEnd(false,deleteState);
  Check(dirtyBadges.Count==0,"failed last-unit deletion does not hide badge");
  bag.Items.RemoveAt(0);RemoveOneEnd(true,deleteState);runner.OnUpdate();
  Check(!badge.Label.visible&&otherBadge.Label.text=="x10","successful last-unit deletion hides only its badge");
  bag.Items[0].Number=1;
  var batch=new Il2CppSystem.Collections.Generic.List<int>{0};RemoveManyBegin(bag,ref batch,out var batchState);
  bag.Items.RemoveAt(0);RemoveManyEnd(true,batchState);runner.OnUpdate();
  Check(!otherBadge.Label.visible,"native batch deletion notifies selected records");
  ClearBagBadges();Check(badgesByGuid.Count==0&&dirtyBadges.Count==0,"close clears identity index and pending work");
  bag.Items.Add(Item(36000,2,912));BagLabel(v,0,slot);
  Reset();Check(common.title.text=="original"&&common.title.textFormat.size==40&&common.title.stroke==1&&common.title.visible&&common.Children.Count==2,"reset restores original title and removes own overlay");
  Check(popupBoundaryReady&&HarmonyLib.Harmony.Registrations==1,"deferred hook installed once across popup/reset lifecycle");
  SetBag(Item(30000,1,20),Item(36000,8,21),Item(30001,1,22),Item(36002,3,23),Item(30002,1,24),Item(36001,5,25));
  GroupTools();
  Check(bag!.Items.Select(x=>x.Guid).SequenceEqual(new long[]{20,21,23,25,22,24}),"expedition tools contiguous at first original position, both relative orders preserved");
  Check(bag.Items.Where(Eligible).Select(x=>x.Number).SequenceEqual(new[]{8,3,5}),"grouping preserves quantities and identity");
  int dirtyBefore=owner!.Dirty;GroupTools();Check(owner.Dirty==dirtyBefore,"stable order does not dirty save repeatedly");
  v=new();group=new();group.ItemList.Add(new BagRow{GlobalIndex=1,ItemTemplate=new Template{tid=30000}});v._model.BagGroupList.Add(group);slot=new();
  BagLabel(v,0,slot);Check(bagBadges.Count==0,"stale model index never labels an unrelated illustration");
  group.ItemList[0].ItemTemplate.tid=36000;BagLabel(v,0,slot);
  Check(bagBadges[slot.compSlotCommon.Pointer].Label.text=="x8","correct template and index bind matching quantity");
  Reset();Il2CppFairyGUI.GObject.RejectGlobalTransforms=false;
  ExpansionTests();
  SkillBookTests(); SaleTooltipTests(); LanguageBookTests(); UnlockItemTests(); CapacityTests(); MouseDisplayTests(); QuickQuantityTests(); LandSupplyQuickTests();
  Console.WriteLine($"PASS {checks} assertions against production sources using offline doubles. No native game code executed.");
 }
}

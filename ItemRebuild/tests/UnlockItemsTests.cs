using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIBag;
using Il2CppClient.UILogic.UIPropStore;
using Il2CppGyyx.Template;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
 // 0.1.19: cabin unlock items hidden while held; route charts auto-used on purchase; owned unlock items leave shops.
 static void UnlockItemTests()
 {
  foreach(int id in Enumerable.Range(11000,11).Concat(new[]{11036,11037}))
   Check(Rules.CabinUnlock(id)&&Rules.UnlockItem(id)&&!Rules.AutoRoute(id)&&!Rules.Stack(id)&&!Rules.GroupedRecord(id),"13 cabin unlock items");
  foreach(int id in new[]{11011,11021,11030,11035,11038,10032})Check(!Rules.CabinUnlock(id),"other ship-drawing items unchanged (user scope)");
  foreach(int id in Enumerable.Range(130001,56))Check(Rules.AutoRoute(id)&&!Rules.CabinUnlock(id),"56 purchasable route charts");
  foreach(int id in new[]{12007,12039,12040,12041,12042,130057,130000})Check(!Rules.AutoRoute(id)&&!Rules.UnlockItem(id),"quest-related charts excluded");

  // Bag: held blueprint row hidden, record kept, no logical capacity.
  var blueprint=Item(11005,1,61001);var tool=Item(31000,1,61002);var other=Item(30000,1,61003);
  SetBag(blueprint,tool,other);
  var ctrl=new UIBagCtrl();var all=new ItemGroup();ctrl.Model.BagGroupList.Add(all);
  for(int i=0;i<bag!.Items.Count;i++){var row=new InventoryItem{GlobalIndex=i,SlotIndex=i,ItemTemplate=new Template{tid=bag.Items[i].ItemId}};ctrl.Model.ItemList.Add(row);all.ItemList.Add(row);}
  GroupEquipment(ctrl);
  Check(all.ItemList.Count==2&&ctrl.Model.ItemList.Count==2&&all.ItemList.All(r=>r.ItemTemplate.tid!=11005),"blueprint row hidden in bag views");
  Check(bag.Items.Contains(blueprint),"blueprint record kept (it is the unlock condition)");
  Check(Occupied().Count==2,"hidden blueprint takes no logical slot");
  bool check=true;Check(CapacityAllowsAdd(11006,1,0,0,false,ref check)&&!check,"incoming blueprint bypasses capacity");

  // Shop: held blueprint and unlocked/held route charts removed from every shop list.
  var lanes=PlayerDataManager.Instance.Data.PlayerLaneData=new PlayerLaneDB();
  TemplateManager.PrefabLaneValues.Clear();
  TemplateManager.PrefabLaneValues.AddRange(new[]{new PrefabLane{tid=1,lineMap=130001},new PrefabLane{tid=2,lineMap=130002},new PrefabLane{tid=3,lineMap=130003},new PrefabLane{tid=9,lineMap=12007}});
  laneByItem=null;laneMapFailed=false;
  lanes.Unlocked.Add(1);
  var held=Item(130003,1,61004);bag.Items.Add(held);
  var store=new UIPropStoreCtrl();var m=store.Model;
  Slot S(int id)=>new Slot{id=id,price=10};
  m.listPropStore.AddRange(new[]{S(11005),S(11006),S(30000)});
  m.listCollege.AddRange(new[]{S(130001),S(130002),S(130003)});
  m.listBlackMarket.Add(S(11005));m.listGuild.Add(S(12007));
  HideOwnedGoods(store);
  Check(m.listPropStore.Select(x=>x.id).SequenceEqual(new[]{11006,30000}),"held blueprint hidden, others stay");
  Check(m.listCollege.Select(x=>x.id).SequenceEqual(new[]{130002}),"unlocked and held route charts hidden");
  Check(m.listBlackMarket.Count==0&&m.listGuild.Count==1,"every shop type filtered; excluded chart untouched");

  // Purchase guard: owned or duplicate unlock items are deselected before native writes.
  m.curShopType=1;m.listPropStore.Clear();
  var a=S(11006);var b=S(11006);var owned=S(11005);var plain=S(30000);
  foreach(var x in new[]{a,b,owned,plain}){x.isSelect=true;m.listPropStore.Add(x);}
  Check(!RefuseOwnedPurchases(store),"refused when selection contains owned/duplicate unlock items");
  Check(a.isSelect&&!b.isSelect&&!owned.isSelect&&plain.isSelect,"one of each new unlock item kept, ordinary goods untouched");
  Check(RefuseOwnedPurchases(store),"corrected selection proceeds");

  // 0.1.21: route purchase is NOT auto-used; the bought chart stays in the bag, its lane stays locked,
  // and it disappears from the shop because it is now held. Bag use remains the native flow.
  var bought=Item(130002,1,61006);bag.Items.Add(bought);
  AfterExchange(store);
  Check(bag.Items.Contains(bought)&&!lanes.Unlocked.Contains(2)&&lanes.Dirty==0,"bought chart kept in bag, lane not unlocked by the mod");
  m.listCollege.Clear();m.listCollege.AddRange(new[]{S(130001),S(130002),S(130004)});HideOwnedGoods(store);
  Check(m.listCollege.Select(x=>x.id).SequenceEqual(new[]{130004}),"held and unlocked charts hidden; unowned chart stays");
  bag.Items.Remove(bought);lanes.UnlockPrefabLane(2);m.listCollege.Clear();m.listCollege.Add(S(130002));HideOwnedGoods(store);
  Check(m.listCollege.Count==0,"chart read from the bag (lane unlocked, record gone) stays hidden");
  m.curShopType=2;var again=S(130002);again.isSelect=true;m.listCollege.Add(again);
  Check(!RefuseOwnedPurchases(store)&&!again.isSelect,"re-purchase of an unlocked lane's chart refused");
  TemplateManager.PrefabLaneValues.Clear();laneByItem=null;
  PlayerDataManager.Instance.Data.PlayerLaneData=new PlayerLaneDB();
  Reset();
 }
}

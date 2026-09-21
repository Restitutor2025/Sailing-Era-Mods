using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIBag;
using Il2CppClient.UILogic.UIPropStore;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
 static void SkillBookTests()
 {
  foreach(int id in Enumerable.Range(71001,100).Concat(Enumerable.Range(71146,6)).Concat(new[]{10034,10098}))
   Check(Rules.SkillBook(id)&&Rules.Stack(id)&&!Rules.Equipment(id),"skill books use real quantity stacks");
  foreach(int id in new[]{71000,71101,71145,71152,31000,36000})Check(!Rules.SkillBook(id),"skill book exact category boundaries");
  var a=Item(71001,1,50101);var b=Item(71001,1,50102);var c=Item(71001,1,50103);var other=Item(71002,1,50104);
  var quality=Item(71001,1,50105);quality.Quality=2;
  var commerce=Item(71001,1,50106);commerce.IsCommerce=true;
  SetBag(a,b,c,other,quality,commerce);var records=bag!.Items.ToArray();Compact(bag);
  Check(bag.Items.Count==4&&owner!.Dirty==1&&a.Number==3&&!bag.Items.Contains(b)&&!bag.Items.Contains(c),"duplicate book records and GUIDs removed, quantity retained");
  Compact(bag);Check(bag.Items.Count==4&&a.Number==3&&owner!.Dirty==1,"book migration idempotent");
  Check(Occupied().Count==4,"three identical books occupy one slot; distinct attributes/type/commerce stay separate");
  Check(Occupied(new(){{a.Guid,2}}).Count==4&&Occupied(new(){{a.Guid,3}}).Count==3,"book stack frees slot only after last unit");
  var ctrl=new UIBagCtrl();var all=new ItemGroup();var category=new ItemGroup();ctrl.Model.BagGroupList.Add(all);ctrl.Model.BagGroupList.Add(category);
  for(int i=0;i<bag.Items.Count;i++){
   var row=new InventoryItem{GlobalIndex=i,SlotIndex=i,ItemTemplate=new Template{tid=bag.Items[i].ItemId}};
   ctrl.Model.ItemList.Add(row);all.ItemList.Add(row);category.ItemList.Add(row);
  }
  GroupEquipment(ctrl);Check(all.ItemList.Count==4&&category.ItemList.Count==4,"All and Books category share skill book grouping");
  var view=new UIBagView{_model=ctrl.Model};var button=new Il2CppBag.UIbtnSlot();BagLabel(view,0,button);
  Check(bagBadges[button.compSlotCommon.Pointer].Label.text=="x3","book badge shows x3");
  Check(category.ItemList.Select(x=>x.GlobalIndex).SequenceEqual(new[]{0,1,2,3}),"book rows retain compacted native global index");
  var shop=new UIPropStoreCtrl();foreach(var item in bag.Items)for(int n=0;n<item.Number;n++)shop.Model.ListCurPlay.Add(new Slot{id=item.ItemId,guid=item.Guid,price=100});
  Check(OpenSale(shop,shop.Model.ListCurPlay[0])&&popupLimit==3,"book sale slider exposes exact group stock");
  TestPopupSet(popup!.Model,2);PopupApplyAction(popup);
  Check(shop.Model.ListCurPlay.Count(x=>x.isSelect)==2&&ValidateSale(shop),"book sale selects two units of one stack GUID");
  bool result=false;Check(!RemoveGuidBegin(bag,a.Guid,ref result,out var removed)&&result&&removed==null&&a.Number==2,"learning consumes one unit, retains stack");
  result=false;Check(!RemoveGuidBegin(bag,a.Guid,ref result,out removed)&&result&&a.Number==1,"second use consumes exactly one unit");
  Check(!ValidateSale(shop),"stale sale selection rejected after book consumption");
  Check(RemoveGuidBegin(bag,a.Guid,ref result,out removed)&&removed==a,"last book delegates native record deletion");
  bag.Items.Remove(a);RemoveOneEnd(true,removed);
  Check(bag.GetItemAmount(71001)==2&&!bag.Items.Contains(a),"last-unit deletion preserves different attributes and commerce");
  Check(RemoveGuid(bag,50102,ref result),"discarded duplicate GUID is not remapped to another book");
  for(int old=1;old<=3;old++)for(int qty=1;qty<=3;qty++){
   var stack=Item(71001,old,50300);stack.Supply=1;SetBag(stack);
   int amount=qty;bool limit=true,ok=false;
   Check(AddBegin(bag!,71001,ref amount,ref limit,ref ok,out var state)&&amount==1&&!limit,"book acquisition adapts native pileNum one");
   var incoming=Item(71001,1,50301);incoming.Supply=1;bag!.Items.Add(incoming);AddEnd(bag,true,state);
   Check(bag.Items.Count==1&&stack.Number==old+qty&&stack.Guid==50300,"new books merge into one retained stack record");
  }
  SetBag(Item(71001,1,50200));bag!.Items[0].Supply=1;owner!.Capacity=1;bool check=true;
  Check(CapacityAllowsAdd(71001,3,0,-1,false,ref check)&&!check,"same book acquisition fits existing group");
  check=true;Check(CapacityAllowsAdd(71002,1,0,-1,false,ref check)&&check,"new book kind retains native capacity check when physical and logical counts match");
  Reset();
 }
}

using Il2CppClient.UILogic.UIBag;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
 // 0.1.18: 30 language-teaching type-73 books use the 0.1.16 skill-book real-quantity stack.
 static void LanguageBookTests()
 {
  foreach(int id in Enumerable.Range(71101,30))
   Check(Rules.LanguageBook(id)&&Rules.Book(id)&&Rules.Stack(id)&&Rules.GroupedRecord(id)&&!Rules.SkillBook(id)&&!Rules.Equipment(id)&&!Rules.Consumable(id),"language books use real quantity stacks");
  foreach(int id in Enumerable.Range(71131,15))
   Check(!Rules.LanguageBook(id)&&!Rules.Book(id)&&!Rules.Stack(id)&&!Rules.GroupedRecord(id),"type-73 skill-teaching books 71131-71145 stay individual (user decision)");
  foreach(int id in new[]{71100,71146})Check(!Rules.LanguageBook(id)&&Rules.SkillBook(id),"language range does not overlap skill books");
  var a=Item(71106,1,51101);var b=Item(71106,1,51102);var c=Item(71106,1,51103);var other=Item(71110,1,51104);
  var commerce=Item(71106,1,51105);commerce.IsCommerce=true;
  var loose1=Item(71131,1,51106);var loose2=Item(71131,1,51107);
  SetBag(a,b,c,other,commerce,loose1,loose2);Compact(bag!);
  Check(bag!.Items.Count==5&&a.Number==3&&!bag.Items.Contains(b)&&!bag.Items.Contains(c)&&bag.Items.Contains(loose1)&&bag.Items.Contains(loose2),"duplicate language books merge; 71131 records untouched");
  Check(Occupied().Count==5,"three identical language books occupy one slot");
  var ctrl=new UIBagCtrl();var all=new ItemGroup();ctrl.Model.BagGroupList.Add(all);
  for(int i=0;i<bag.Items.Count;i++){var row=new InventoryItem{GlobalIndex=i,SlotIndex=i,ItemTemplate=new Template{tid=bag.Items[i].ItemId}};ctrl.Model.ItemList.Add(row);all.ItemList.Add(row);}
  GroupEquipment(ctrl);
  var view=new UIBagView{_model=ctrl.Model};var button=new Il2CppBag.UIbtnSlot();BagLabel(view,0,button);
  Check(bagBadges[button.compSlotCommon.Pointer].Label.text=="x3","language book badge shows x3");
  bool result=false;Check(!RemoveGuidBegin(bag,a.Guid,ref result,out var removed)&&result&&removed==null&&a.Number==2,"learning a language consumes one unit, retains stack");
  Reset();
 }
}

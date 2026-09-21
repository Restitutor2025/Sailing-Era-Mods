using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIBag;
using Il2CppClient.UILogic.UIPropStore;
using Il2CppClient.UILogic.UICommonInputNum;
using Il2CppFairyGUI;

namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    static void ExpansionTests()
    {
        // 0.1.17 clothes: 59 installed type-33 IDs; trade statistics notebooks stay individual.
        var clothes=new[]{10111,33001,33002,33005,33006,33007,33008,33009,33010,33013,33014,33015,33023,33024,33025,33026,33027,33029,33030,33031,33032,33033,33035,33043,33044,
            33045,33046,33047,33048,33049,33050,33051,33052,33053,33054,33055,33056,33057,33058,33059,33060,33061,33071,33072,33073,33074,34001,34002,34006,34007,
            34009,34010,34011,34014,34026,34033,34038,34039,34042};
        Check(clothes.Length==59&&clothes.Distinct().Count()==59&&clothes.All(Rules.Equipment)&&clothes.All(id=>!Rules.Consumable(id)&&!Rules.SkillBook(id)&&!Rules.Target(id)),"59 clothes use the equipment display-group policy only");
        foreach(int id in Enumerable.Range(33062,9)) Check(!Rules.Equipment(id)&&!Rules.Stack(id),"trade statistics notebooks are never grouped");
        foreach(int id in Enumerable.Range(33062,9))
        {
            var a=Item(id,1,9101);var b=Item(id,1,9102);SetBag(a,b);
            var db=PlayerDataManager.Instance.Data.PlayerEquipData;db.Items.Clear();
            db.Items[a.Guid]=new EquipData{Equip=Item(id,1,a.Guid)};db.Items[b.Guid]=new EquipData{Equip=Item(id,1,b.Guid)};
            Check(!Unworn(a)&&Occupied().Count==2,"two unworn notebooks keep two native slots");
        }
        foreach(int id in new[]{31000,32001,33042,33026,10111,33030})
        {
            var a=Item(id,1,9001);var b=Item(id,1,9002);SetBag(a,b);
            var db=PlayerDataManager.Instance.Data.PlayerEquipData;db.Items.Clear();
            db.Items[a.Guid]=new EquipData{Equip=Item(id,1,a.Guid)};
            db.Items[b.Guid]=new EquipData{Equip=Item(id,1,b.Guid)};
            Check(db.Items[a.Guid].Equip.Pointer!=a.Pointer&&Unworn(a),"deserialized equipment copy is matched by persistent identity, not address");
            Check(Occupied().Count==1,"screenshot dagger/armor/tool duplicates occupy one slot after loading");
            Check(Occupied(new(){{9001,1}}).Count==1&&Occupied(new(){{9001,1},{9002,1}}).Count==0,"loaded equipment occupies one slot until final GUID sold");
            db.Items[a.Guid].Equip.Guid=9003;Check(!Unworn(a),"mismatched GUID is rejected");
            db.Items[a.Guid].Equip.Guid=a.Guid;db.Items[a.Guid].Equip.ItemId=id+1;Check(!Unworn(a),"mismatched item ID is rejected");
            db.Items[a.Guid].Equip.ItemId=id;db.Items[a.Guid].WearRoleId=42;Check(!Unworn(a)&&Occupied().Count==2,"loaded worn equipment stays independent");
            db.Items[a.Guid].WearRoleId=0;db.Items[a.Guid].EquipEffectGuid=7;Check(!Unworn(a),"active effect remains excluded");
        }
        var souvenirIds=Enumerable.Range(40001,38).Concat(new[]{10059,10077,10108});
        foreach(int id in souvenirIds) Check(Rules.Consumable(id)&&!Rules.Equipment(id)&&!Rules.Target(id),"all 41 installed souvenirs use consumable policy only");
        Check(!Rules.Consumable(40000)&&!Rules.Consumable(40039),"souvenir whitelist does not spill beyond installed category");
        SetBag(Item(40015,1,9010),Item(40015,1,9011));Compact(bag);
        Check(bag!.Items.Count==1&&bag.Items[0].Number==2&&Occupied().Count==1,"two leaf cards merge into a single occupied slot");
        Check(Occupied(new(){{9010,1}}).Count==1&&Occupied(new(){{9010,2}}).Count==0,"souvenir group frees slot only after final unit sold");
        foreach(int id in new[]{100019,42003,10216,10158,12006,10059,10077,10108,40001,40015,40038})
        {
            var a=Item(id,2,2001);a.Quality=5;a.IsSuper=true;a.BuyPrice=123;a.Supply=7;a.state=Il2CppClient.Const.ECargoState.Other;
            var b=Item(id,3,2002);b.Quality=5;b.IsSuper=true;b.BuyPrice=123;b.Supply=7;b.state=a.state;
            var different=Item(id,4,2003);different.Quality=6;
            SetBag(a,b,different);Compact(bag);
            Check(bag!.Items.Count==2&&a.Number==5&&a.Guid==2001&&different.Number==4,"new category merge by exact attributes");
            Check(a.Quality==5&&a.BuyPrice==123&&a.IsSuper&&a.Supply==7&&(int)a.state==1,"new consumable metadata preserved");
            Check(!Rules.Target(id),"new categories never enter expedition policy");
            int before=owner!.Dirty;Compact(bag);Check(owner.Dirty==before,"new migration idempotent");
        }
        for(int qty=1;qty<=25;qty++)
        {
            var a=Item(42003,8,2101);a.BuyPrice=75;a.Quality=2;a.IsSuper=true;a.Supply=1;
            SetBag(a);int request=qty;bool limit=true,result=false;
            AddBegin(bag!,42003,ref request,ref limit,ref result,out var state,75,2,true);
            Check(request==1&&!limit,"same-attribute new consumable bypasses occupied-slot limit");
            var added=Item(42003,1,2102);added.BuyPrice=75;added.Quality=2;added.IsSuper=true;added.Supply=1;bag!.Items.Add(added);AddEnd(bag,true,state);
            Check(bag.Items.Count==1&&a.Number==qty+8&&a.Quality==2&&a.IsSuper&&a.BuyPrice==75,"new acquisition preserves count and attributes");
        }
        SetBag(Item(42003,3,2103));int req=1;bool cap=true,res=false;
        AddBegin(bag!,42003,ref req,ref cap,ref res,out _,10);
        Check(cap,"different attribute acquisition still needs a new slot");

        // A native model contains distinct inventory rows, shared by All and category lists.
        var e1=Item(33000,1,2201);var e2=Item(33000,1,2202);var worn=Item(33000,1,2203);
        var quality=Item(33000,1,2204);quality.Quality=3;
        var missing=Item(33000,1,2205);var weapon=Item(31022,1,2206);var weapon2=Item(31022,1,2207);
        var armor=Item(32000,1,2208);var armor2=Item(32000,1,2209);
        SetBag(e1,e2,worn,quality,missing,weapon,weapon2,armor,armor2);
        var equips=PlayerDataManager.Instance.Data.PlayerEquipData;equips.Items.Clear();
        foreach(var item in bag!.Items) if(item!=missing) equips.Items[item.Guid]=new EquipData {Equip=Item(item.ItemId,item.Number,item.Guid),WearRoleId=item==worn?42:0};
        var ctrl=new UIBagCtrl();var all=new ItemGroup();var category=new ItemGroup();
        ctrl.Model.BagGroupList.Add(all);ctrl.Model.BagGroupList.Add(category);
        for(int i=0;i<bag.Items.Count;i++)
        {
            var row=new InventoryItem {GlobalIndex=i,SlotIndex=i,WearRole=i==2?42:0,ItemTemplate=new Template {tid=bag.Items[i].ItemId}};
            ctrl.Model.ItemList.Add(row);all.ItemList.Add(row);category.ItemList.Add(row);
        }
        var original=bag.Items.ToArray();var nativeRows=ctrl.Model.ItemList.ToArray();
        GroupEquipment(ctrl);
        Check(ctrl.Model.ItemList.Count==6&&all.ItemList.Count==6&&category.ItemList.Count==6,"all and equipment categories collapse duplicate unworn gear");
        Check(bag.Items.SequenceEqual(original)&&bag.Items.All(x=>x.Number==1)&&owner!.Dirty==0,"equipment grouping never changes persistent identities/counts/dirty state");
        Check(equipmentCounts[nativeRows[0].Pointer]==2&&equipmentCounts[nativeRows[5].Pointer]==2&&equipmentCounts[nativeRows[7].Pointer]==2,"tool armor weapon counts");
        Check(!equipmentCounts.ContainsKey(nativeRows[2].Pointer)&&!equipmentCounts.ContainsKey(nativeRows[4].Pointer),"worn and missing equip state are never grouped");
        Check(category.ItemList.Select(x=>x.GlobalIndex).SequenceEqual(new[]{0,2,3,4,5,7}),"representatives retain real inventory indices");
        Check(category.ItemList.Select(x=>x.SlotIndex).SequenceEqual(Enumerable.Range(0,6)),"category indices regenerated");
        var view=new UIBagView {_model=ctrl.Model};var button=new Il2CppBag.UIbtnSlot();BagLabel(view,0,button);
        Check(bagBadges[button.compSlotCommon.Pointer].Label.text=="x2","equipment aggregate badge");
        BagLabel(view,1,button);Check(bagBadges.Count==0&&button.compSlotCommon.title.visible,"worn slot keeps native title/avatar presentation");
        BagDiscarded(ctrl);Check(ctrl.Refreshes==1,"discard rebuilds remaining group");
        BagClosed();Check(equipmentCounts.Count==0,"close releases equipment model references");

        // Sales keep native per-unit records and mark N real slots: price/remove paths remain native.
        for(int quantity=0;quantity<=12;quantity++)
        {
            SetBag(Item(100019,12,2301));var shop=new UIPropStoreCtrl();
            for(int n=0;n<12;n++) shop.Model.ListCurPlay.Add(new Slot {id=100019,guid=2301,price=250});
            var rowButton=new GObject {data=shop.Model.ListCurPlay[3]};
            Check(!SaleClicked(shop,new EventContext {sender=new GComponent(),data=rowButton}),"click uses event data not list sender");
            Check(salePopup&&popupLimit==12&&popup!.Model.CurNum==0,"sale popup starts at selected count with full stock max");
            var popupView=new UICommonInputNumView {_model=popup!.Model};popupView.UIContent.SetSize(400,150);
            SalePopupRendered(popupView);
            Check(saleSlider!=null&&saleSlider.min==0&&saleSlider.max==12&&saleSlider.wholeNumbers,"integer slider min zero max stock");
            saleSlider!.value=quantity;saleSlider.onChanged.Call();
            Check(Math.Abs(saleSlider._gripObject!.x-(saleSlider.width*quantity/12-9))<.001,"slider grip follows integer value across full range");
            Check(popup.Model.CurNum==quantity&&shop.Model.ListCurPlay.All(x=>!x.isSelect),"drag updates input without committing sale selection");
            PopupApplyAction(popup);
            Check(shop.Model.ListCurPlay.Count(x=>x.isSelect)==quantity&&shop.Model.SelectNum==quantity&&shop.Model.Sum==quantity*250,"selection and native money calculation agree for zero through max");
            Check(saleSlider==null&&!salePopup&&sliderCallbacks.Count==0,"slider/delegates disposed on close");
            Check(ValidateSale(shop),"validated sale fits stock");
            foreach(var unit in shop.Model.ListCurPlay.Where(x=>x.isSelect))
            {
                bool ok=false;
                if(RemoveGuid(bag!,unit.guid,ref ok)) bag!.Items.RemoveAt(0); // model last-unit native removal
            }
            Check(bag!.GetItemAmount(100019)==12-quantity,"per-unit native settlement consumes exactly selected units");
        }
        SetBag(e1,e2,worn,quality);equips.Items.Clear();
        foreach(var item in bag!.Items) equips.Items[item.Guid]=new EquipData {Equip=Item(item.ItemId,item.Number,item.Guid),WearRoleId=item==worn?42:0};
        var gearShop=new UIPropStoreCtrl();
        foreach(var item in bag.Items) gearShop.Model.ListCurPlay.Add(new Slot {id=item.ItemId,guid=item.Guid,price=100,wearRole=item==worn?42:0});
        Check(OpenSale(gearShop,gearShop.Model.ListCurPlay[0])&&popupLimit==2,"equipment sale max excludes worn and differing attributes");
        popup!.Model.CurNum=2;PopupApplyAction(popup);
        Check(gearShop.Model.ListCurPlay.Select(x=>x.isSelect).SequenceEqual(new[]{true,true,false,false}),"equipment quantity maps to distinct available GUIDs");
        Check(ValidateSale(gearShop),"valid equipment selection");
        equips.Items[e2.Guid].WearRoleId=99;
        Check(!ValidateSale(gearShop)&&!gearShop.Model.ListCurPlay[1].isSelect,"newly worn gear rejects original exchange before currency writes");
        var fakeShopSlot=new Slot {id=e1.ItemId,guid=e1.Guid,price=100};
        Check(!OpenSale(gearShop,fakeShopSlot),"same ID/GUID on shop side cannot open player sale popup");
        // 0.1.22: with e2 worn, e1 is a single unit and toggles without a popup.
        Check(OpenSale(gearShop,gearShop.Model.ListCurPlay[0])&&popup==null&&!gearShop.Model.ListCurPlay[0].isSelect,"single available unit toggles directly");
        OpenSale(gearShop,gearShop.Model.ListCurPlay[0]);Check(gearShop.Model.ListCurPlay[0].isSelect,"second click selects it again");
        equips.Items[e2.Guid].WearRoleId=0;
        OpenSale(gearShop,gearShop.Model.ListCurPlay[0]);popup!.Model.CurNum=0;
        var callback=chooseCallback!;ResetSales();callback.Invoke(0);
        Check(gearShop.Model.ListCurPlay[0].isSelect,"cancel and stale callback do not commit");
        OpenSale(gearShop,gearShop.Model.ListCurPlay[0]);gearShop.Model.CurSelect=ESelectType.Book;popup!.Model.CurNum=0;PopupApplyAction(popup);
        Check(gearShop.Model.ListCurPlay[0].isSelect,"changed category invalidates pending quantity");
        SetBag(Item(42003,2,2401));var staleShop=new UIPropStoreCtrl();
        for(int i=0;i<3;i++) staleShop.Model.ListCurPlay.Add(new Slot {id=42003,guid=2401,price=10,isSelect=true});
        Check(!ValidateSale(staleShop)&&staleShop.Model.SelectNum==2,"stale unit rows cannot exceed live count");
        OpenSale(staleShop,staleShop.Model.ListCurPlay[0]);popup!.Model.CurNum=999;
        Check(popup.Model.CurNum==2,"direct input clamps to live max");
        popup.Model.CurNum=-1;Check(popup.Model.CurNum==0,"direct input clamps to zero");
        ResetSales();owner!.Capacity=bag!.Items.Count;staleShop.Model.listPropStore.Add(new Slot {id=99991,isSelect=true});
        staleShop.Model.ListCurPlay[1].isSelect=false;
        Check(!ValidateSale(staleShop)&&UIManager.Instance.Warnings==1,"partial stack sale does not free a slot for simultaneous purchase");
        staleShop.Model.ListCurPlay[1].isSelect=true;
        Check(ValidateSale(staleShop),"selling entire stack frees exactly one slot");
        staleShop.Model.listPropStore.Add(new Slot {id=99992,isSelect=true});
        Check(!ValidateSale(staleShop),"two sold units cannot credit two freed rows");
        owner.Capacity++;
        Check(ValidateSale(staleShop),"available row plus fully sold stack admits two purchases");
        Reset();
    }
}

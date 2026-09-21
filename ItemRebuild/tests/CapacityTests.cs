using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIBag;
using Il2CppClient.UILogic.UIPropStore;

namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    static void CapacityTests()
    {
        var equipDb=PlayerDataManager.Instance.Data.PlayerEquipData;
        var gear=Enumerable.Range(0,5).Select(i=>Item(31000,1,3000+i)).ToArray();
        foreach(var item in gear) item.Supply=1;
        SetBag(gear);equipDb.Items.Clear();
        foreach(var item in gear) equipDb.Items[item.Guid]=new EquipData{Equip=Item(item.ItemId,item.Number,item.Guid)};
        owner!.Capacity=1;
        var original=bag!.Items.ToArray();
        Check(Occupied().Count==1,"five identical unworn gear GUIDs occupy one slot");
        int free=-4;FreeCapacity(owner,ref free);Check(free==0,"logical free capacity at limit");
        var view=new UIBagView();BagCapacity(view);
        Check(view.UIContent.texCapacity.text=="1/1"&&view.UIContent.ctrlOverLoad.selectedIndex==1,"bag label and full indicator use groups");
        bool check=true;
        Check(CapacityAllowsAdd(31000,100,0,-1,false,ref check)&&!check,"full bag can acquire more of existing equipment group");
        check=true;Check(!CapacityAllowsAdd(32000,1,0,-1,false,ref check),"new gear group blocked at capacity");
        check=true;Check(!CapacityAllowsAdd(31000,1,0,7,false,ref check),"different attributes require another slot");
        owner.Capacity=2;check=true;
        Check(CapacityAllowsAdd(32000,50,0,-1,false,ref check)&&!check,"batch of new identical gear needs just one free slot");
        check=true;Check(CapacityAllowsAdd(99990,1,0,0,false,ref check)&&!check,"ordinary item can use space saved by equipment grouping");
        check=true;Check(!CapacityAllowsAdd(99990,2,0,0,false,ref check),"ordinary item batch cannot exceed remaining logical capacity");
        equipDb.Items[gear[0].Guid].WearRoleId=7;
        Check(Occupied().Count==2,"worn gear remains independent of unworn group");
        equipDb.Items[gear[0].Guid].WearRoleId=0;
        Check(Occupied().Count==1,"taking gear off rejoins same capacity group");
        gear[0].Quality=4;Check(Occupied().Count==2,"quality variants occupy separate slots");gear[0].Quality=0;
        owner.Capacity=1;
        for(int sold=0;sold<=5;sold++)
        {
            var shop=new UIPropStoreCtrl();
            for(int i=0;i<5;i++) shop.Model.ListCurPlay.Add(new Slot{id=31000,guid=gear[i].Guid,isSelect=i<sold});
            Check(ProjectedSpace(shop)==(sold==5?0:1),"equipment group survives every partial sale and disappears only at zero");
            shop.Model.listPropStore.Add(new Slot{id=32000,isSelect=true});
            Check(ValidateSale(shop)==(sold==5),"different purchase needs full equipment group sold");
            shop.Model.listPropStore[0].id=31000;
            for(int i=0;i<8;i++) shop.Model.listPropStore.Add(new Slot{id=31000,isSelect=true});
            Check(ValidateSale(shop)&&ProjectedSpace(shop)==1,"same gear purchases share one slot before and after selling all");
            Check(ExchangeBegin(shop,out var prior)&&exchangeSpace!=null,"start valid exchange scope");
            int credit=sold;ExchangeCapacityCredit(shop.Model,ref credit);
            Check(owner.Capacity-bag.Items.Count>=shop.Model.BuyCount-credit,"native raw-record comparison receives equivalent group capacity");
            int actual=sold;ExchangeCapacityCredit(shop.Model,ref actual);
            Check(actual==sold,"capacity credit consumed once; later sale counts stay actual");
            check=true;Check(CapacityAllowsAdd(31000,1,0,-1,false,ref check)&&!check,"native buy-before-sell can execute validated transaction");
            var ex=new InvalidOperationException("native failure");
            Check(ReferenceEquals(ExchangeFinal(shop,ex,prior),ex)&&exchangeSpace==null,"exchange exception clears temporary scope without swallowing error");
        }
        Check(bag.Items.SequenceEqual(original)&&bag.Items.All(x=>x.Number==1)&&owner.Capacity==1,"capacity queries and checks never alter stored GUID rows quantities or limit");
        var foreign=new PlayerBagDB();free=123;FreeCapacity(foreign,ref free);Check(free==123,"other bags retain native capacity");
        Il2CppGyyx.Template.TemplateManager.Cargoes[31000]=new(){quality=8};
        Check(IncomingKey(31000,0,-1,false).Quality==8,"negative quality uses actual cargo fallback");
        Il2CppGyyx.Template.TemplateManager.Cargoes.Clear();
        Reset();
    }
}

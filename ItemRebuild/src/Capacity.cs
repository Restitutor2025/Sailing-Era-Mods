using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIBag;
using Il2CppClient.UILogic.UIPropStore;
using Il2CppGyyx.Template;

namespace Restitutor.ItemRebuild;

public sealed partial class EntryPoint
{
    sealed class Space
    {
        public int Count;
        public readonly HashSet<ItemKey> Groups=new();
        public void Add(ItemKey key,bool grouped,int count=1)
        {
            if(grouped) { if(Groups.Add(key)) Count=checked(Count+1); }
            else Count=checked(Count+count);
        }
    }
    static Space Occupied(Dictionary<long,int>? sold=null)
    {
        var result=new Space();
        if(bag==null) return result;
        for(int i=0;i<bag.Items.Count;i++)
        {
            var item=bag.Items[i];
            if(item==null) {result.Count++;continue;}
            if(Rules.CabinUnlock(item.ItemId)) continue; // 0.1.19: hidden held unlock item, no slot
            if(sold!=null&&sold.GetValueOrDefault(item.Guid)>=item.Number&&item.Number>0) continue;
            result.Add(Key(item),item.Number>0&&(Eligible(item)||GroupableRecord(item)));
        }
        return result;
    }
    static ItemKey IncomingKey(int id,int price,int quality,bool super)
    {
        if(Rules.Target(id)) return new(id,0,0,0,false,0);
        // Match PlayerItemData's native constructor, including default quality from Cargo.
        if(quality==-1) quality=TemplateManager.GetCargo(id,out var cargo)&&cargo!=null?cargo.quality:0;
        return new(id,price,1,quality,super,0);
    }
    static bool CapacityAllowsAdd(int id,int amount,int price,int quality,bool super,ref bool check)
    {
        if(!check||bag==null||owner==null||!Own(bag)||amount<=0) return true;
        if(Rules.CabinUnlock(id)) {check=false;return true;} // 0.1.19: hidden, takes no slot
        if(exchangeSpace!=null) {check=false;return true;} // Validated final state; native buys before selling.
        var space=Occupied();
        bool grouped=Rules.Stack(id)||Rules.GroupedRecord(id);
        if(!grouped&&space.Count==bag.Items.Count) return true; // Unchanged native case.
        var key=grouped?IncomingKey(id,price,quality,super):default;
        if(Rules.Stack(id)&&space.Count==bag.Items.Count&&!space.Groups.Contains(key)) return true;
        space.Add(key,grouped,amount);
        if(space.Count>owner.Capacity) return false;
        check=false; // Replace raw-record capacity check only, never change the stored limit.
        return true;
    }
    static void FreeCapacity(PlayerBagDB __instance,ref int __result)
    {
        if(owner!=null&&owner.Pointer==__instance.Pointer&&bag!=null&&Own(bag)) __result=owner.Capacity-Occupied().Count;
    }
    static void BagCapacity(UIBagView __instance)
    {
        if(bag==null||owner==null||!Own(bag)||__instance.UIContent==null) return;
        int count=Occupied().Count;
        __instance.UIContent.texCapacity.text=$"{count}/{owner.Capacity}";
        __instance.UIContent.ctrlOverLoad.SetSelectedIndex(count>=owner.Capacity?1:0);
    }
    static Il2CppSystem.Collections.Generic.List<Slot>? Purchases(UIPropStoreModel m)=> (int)m.curShopType switch
    {
        1=>m.listPropStore,2=>m.listCollege,3=>m.listBlackMarket,4=>m.listGuild,_=>null
    };
    static int ProjectedSpace(UIPropStoreCtrl ctrl)
    {
        var sold=new Dictionary<long,int>();var rows=ctrl.Model.ListCurPlay;
        for(int i=0;i<rows.Count;i++) if(rows[i]!=null&&rows[i].isSelect)
            sold[rows[i].guid]=sold.GetValueOrDefault(rows[i].guid)+1;
        var space=Occupied(sold);
        var buys=Purchases(ctrl.Model);
        if(buys!=null) for(int i=0;i<buys.Count;i++)
        {
            var row=buys[i];if(row==null||!row.isSelect) continue;
            if(Rules.UnlockItem(row.id)) continue; // 0.1.19: hidden cabin items / auto-used route charts
            bool grouped=Rules.Stack(row.id)||Rules.GroupedRecord(row.id);
            space.Add(grouped?IncomingKey(row.id,0,-1,false):default,grouped);
        }
        return space.Count;
    }
    sealed class ExchangeSpace
    {
        public IntPtr Model;
        public int Credit;
        public bool Pending=true;
    }
    static ExchangeSpace? exchangeSpace;
    static bool ExchangeBegin(UIPropStoreCtrl __instance,out ExchangeSpace? __state)
    {
        __state=exchangeSpace;
        unlockFollowUp=false;
        if(!RefuseOwnedPurchases(__instance)) return false;
        if(!ValidateSale(__instance)) return false;
        unlockFollowUp=true;
        if(bag!=null&&Own(bag)) exchangeSpace=new ExchangeSpace {
            Model=__instance.Model.Pointer,
            Credit=checked(__instance.Model.BuyCount+bag.Items.Count-ProjectedSpace(__instance))
        };
        return true;
    }
    static void ExchangeCapacityCredit(UIPropStoreModel __instance,ref int __result)
    {
        // Exchange's one capacity comparison: rawFree >= BuyCount - this value.
        // Algebra reduces that comparison to storedLimit >= ProjectedSpace.
        // Consume once so native UI/settlement continues seeing real unit sale counts.
        if(exchangeSpace is {Pending:true} s&&s.Model==__instance.Pointer)
        { __result=s.Credit;s.Pending=false; }
    }
    static Exception? ExchangeFinal(UIPropStoreCtrl __instance,Exception? __exception,ExchangeSpace? __state)
    {
        exchangeSpace=__state;
        if(__exception==null&&unlockFollowUp)
        {
            try { AfterExchange(__instance); }
            catch(Exception ex) { log.Error("Unlock-item follow-up after purchase failed: "+ex); }
        }
        unlockFollowUp=false;
        return __exception;
    }
}

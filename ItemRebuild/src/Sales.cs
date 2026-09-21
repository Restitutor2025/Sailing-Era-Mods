using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIPropStore;
using Il2CppClient.UILogic.UICommonInputNum;
using Il2CppFairyGUI;

namespace Restitutor.ItemRebuild;

public sealed partial class EntryPoint
{
    static UIPropStoreCtrl? saleCtrl;
    static bool salePopup;
    static int saleGeneration;
    static void ResetSales()
    {
        saleGeneration++;
        ClearQuick(storeQuick);quickStore=null;
        if(salePopup) CancelPopup();
        salePopup=false; saleCtrl=null; DisposeSaleSlider();ClearSaleDisplay();
    }
    void InstallSales()
    {
        Patch(typeof(UIPropStoreCtrl),"SetPlayerGoods",nameof(StorePreparing));
        Patch(typeof(UIPropStoreCtrl),"BtnMulti",nameof(SaleClicked));
        Patch(typeof(UIPropStoreCtrl),"SelectSameGoods",nameof(SaleSame));
        Patch(typeof(UIPropStoreCtrl),"SelectAllCargos",nameof(SaleAll));
        Patch(typeof(UIPropStoreCtrl),"Exchange",nameof(ExchangeBegin),null,nameof(ExchangeFinal));
        Patch(typeof(UIPropStoreModel),"get_SellCount",null,nameof(ExchangeCapacityCredit));
        Patch(typeof(UIPropStoreCtrl),"CloseHook",nameof(ResetSales));
        Patch(typeof(UICommonInputNumView),"Refresh",null,nameof(SalePopupRendered));
        Patch(typeof(UIPropStoreView),"Refresh",nameof(SaleDisplayBegin),nameof(SaleDisplayEnd));
        Patch(typeof(UIPropStoreView),"ListPlayerRender",nameof(SaleRenderBegin),nameof(SaleRenderEnd));
        Patch(typeof(UIPropStoreView),"ListShopRender",nameof(ClearSaleLabel),nameof(BindStoreQuick));
        Patch(typeof(UIPropStoreCtrl),"OnAction_R2",nameof(StoreMiddleLegacy));
        Patch(typeof(UIPropStoreCtrl),"OnAction_L2",nameof(StoreMiddleLegacy));
        // 0.1.22: native SetCurGoods picks ListCurPlay[ListPlayer.selectedIndex] (raw row) for the
        // tooltip; with grouped sale rows the visible index is not the raw index.
        Patch(typeof(UIPropStoreCtrl),"SetCurGoods",null,nameof(CurGoodsSelected));
    }
    static void StorePreparing(UIPropStoreCtrl __instance)
    {
        ResetSales();
        quickStore=__instance;
        // Before native construction of per-unit sale slots; no temporary inventory expansion.
        var db=PlayerDataManager.Instance?.Data?.PlayerBag;
        if(db!=null) {Bind(db); Compact(db.ItemBag);}
    }
    static PlayerItemData? FindLive(long guid)
    {
        if(bag==null||!Own(bag)) return null;
        for(int i=0;i<bag.Items.Count;i++) if(bag.Items[i].Guid==guid) return bag.Items[i];
        return null;
    }
    static bool SaleTarget(Slot slot,out PlayerItemData? item)
    {
        item=FindLive(slot.guid);
        return item!=null&&item.ItemId==slot.id&&!item.IsCommerce&&item.Number>0&&slot.wearRole==0
            &&(Eligible(item)||GroupableRecord(item));
    }
    static bool SameSale(Slot a,Slot b,PlayerItemData item)
        => a.id==b.id&&a.price==b.price&&a.type==b.type&&a.IsOnly==b.IsOnly
            &&a.isTask==b.isTask&&a.isParliamentTask==b.isParliamentTask&&a.IsExclusive==b.IsExclusive
            &&SaleTarget(b,out var other)&&Key(item)==Key(other!);
    static List<Slot> SaleSlots(UIPropStoreCtrl ctrl,Slot selected)
    {
        var result=new List<Slot>();
        var list=ctrl.Model?.ListCurPlay;
        if(list==null||!SaleTarget(selected,out var item)) return result;
        bool present=false;
        var used=new Dictionary<long,int>();
        var seen=new HashSet<IntPtr>();
        for(int i=0;i<list.Count;i++)
        {
            var slot=list[i];
            if(slot==null) continue;
            if(slot.Pointer==selected.Pointer) present=true;
            if(!seen.Add(slot.Pointer)||!SameSale(selected,slot,item!)) continue;
            var live=FindLive(slot.guid)!;
            int count=used.GetValueOrDefault(slot.guid);
            if(count>=live.Number) continue;
            used[slot.guid]=count+1; result.Add(slot);
        }
        if(!present) result.Clear(); // A shop-side slot must never match a player-side group by ID alone.
        return result;
    }
    static bool SaleClicked(UIPropStoreCtrl __instance,EventContext __0)
    {
        // Native BtnMulti reads EventContext.data (the clicked row), not sender (the list).
        var button=__0?.data?.TryCast<GObject>();
        var slot=button?.data?.TryCast<Slot>();
        if(__0?.inputEvent?.button==2) {if(slot!=null)QuickStoreMax(__instance,slot);return false;}
        return slot==null||!OpenSale(__instance,slot);
    }
    static bool SaleSame(UIPropStoreCtrl __instance)
    {
        var list=__instance.Model?.ListCurPlay;
        int i=__instance.View?.ListPlayer?.selectedIndex??-1;
        if(list==null||i<0) return true;
        var selected=VisibleSale(__instance,i);
        if(selected==null) return false;
        if(OpenSale(__instance,selected)) return false;
        if(Rules.Stack(selected.id)||Rules.GroupedRecord(selected.id)) return false;
        // Non-grouped items still use their actual row; visible indices can differ from raw indices.
        if(!HasSaleDisplay(__instance.View)) return true;
        bool value=!selected.isSelect;
        for(int n=0;n<list.Count;n++) if(list[n].id==selected.id&&(!value||(!list[n].isTask&&!list[n].isParliamentTask))) list[n].isSelect=value;
        RecountSale(__instance);return false;
    }
    static bool SaleAll(UIPropStoreCtrl __instance)
    {
        if(!HasSaleDisplay(__instance.View)||__instance.View.ListPlayer.selectedIndex<0) return true;
        var rows=__instance.Model.ListCurPlay;
        bool CanSelect(Slot row)=>!row.isTask&&!row.isParliamentTask
            &&(!(Rules.Stack(row.id)||Rules.GroupedRecord(row.id))||SaleTarget(row,out _));
        bool value=false;
        for(int i=0;i<rows.Count;i++) if(rows[i]!=null&&CanSelect(rows[i])&&!rows[i].isSelect) {value=true;break;}
        for(int i=0;i<rows.Count;i++)
        {
            var row=rows[i];if(row==null) continue;
            if(!value) {row.isSelect=false;continue;}
            if(!CanSelect(row)) continue;
            row.isSelect=true;
        }
        RecountSale(__instance);return false;
    }
    static bool OpenSale(UIPropStoreCtrl ctrl,Slot selected)
    {
        var slots=SaleSlots(ctrl,selected);
        if(slots.Count==0) return false;
        if(slots.Count==1)
        {
            // 0.1.22: a single unit toggles directly; no quantity popup, no focus change.
            slots[0].isSelect=!slots[0].isSelect;
            RecountSale(ctrl);
            return true;
        }
        if(!EnsurePopupBoundary()) return true;
        CancelPopup();
        saleCtrl=ctrl; salePopup=true;
        int generation=++saleGeneration;
        var model=ctrl.Model;
        var category=model.CurSelect;
        int current=slots.Count(x=>x.isSelect);
        popupLimit=slots.Count;
        chooseCallback=(Il2CppSystem.Action<long>)(Action<long>)(value=>
        {
            if(!salePopup||generation!=saleGeneration||saleCtrl==null||saleCtrl.Pointer!=ctrl.Pointer
                ||ctrl.Model.Pointer!=model.Pointer||model.CurSelect!=category) return;
            var live=SaleSlots(ctrl,selected);
            int count=(int)Math.Clamp(value,0,live.Count);
            // Clear the captured group too, so a newly worn or removed unit cannot remain selected.
            foreach(var row in slots) row.isSelect=false;
            for(int i=0;i<live.Count;i++) live[i].isSelect=i<count;
            RecountSale(ctrl);
        });
        RememberSaleView(ctrl);
        openingOwnPopup=true;
        try { UICommonInputNumCtrl.Instance.OpenInputNumPanel(current,popupLimit,popupLimit,chooseCallback,null); }
        catch { CancelPopup(); throw; }
        finally { openingOwnPopup=false; }
        return true;
    }
    static void RecountSale(UIPropStoreCtrl ctrl)
    {
        int count=0; var rows=ctrl.Model.ListCurPlay;
        if(rows!=null) for(int i=0;i<rows.Count;i++) if(rows[i].isSelect) count++;
        ctrl.Model.SelectNum=count;
        ctrl.CalculateMoney(); ctrl.Model.MarkDirty();
    }
    static bool ValidateSale(UIPropStoreCtrl __instance)
    {
        var list=__instance.Model?.ListCurPlay;
        if(list==null) return true;
        var used=new Dictionary<long,int>();
        bool stale=false;
        for(int i=0;i<list.Count;i++)
        {
            var row=list[i];
            if(row==null||!row.isSelect||!(Rules.Stack(row.id)||Rules.GroupedRecord(row.id))) continue;
            int count=used.GetValueOrDefault(row.guid)+1;
            if(!SaleTarget(row,out var item)||count>item!.Number) {row.isSelect=false; stale=true;}
            else used[row.guid]=count;
        }
        if(!stale) return SaleCapacity(__instance);
        RecountSale(__instance);
        log.Msg("Sale selection changed with inventory/equipment; invalid units deselected. Confirm again.");
        return false; // Before original currency writes and item removal.
    }
    static bool SaleCapacity(UIPropStoreCtrl ctrl)
    {
        if(bag==null||owner==null||!Own(bag)||ctrl.Model.BuyCount<=0) return true;
        if(ProjectedSpace(ctrl)<=owner.Capacity) return true;
        ShowSaleCapacityWarning();
        return false;
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void ShowSaleCapacityWarning()
        => UIManager.Instance.ShowConfirmationPanel("가방 공간이 부족합니다. 판매를 먼저 완료한 뒤 구매해 주세요.",null,null,null,null);
}

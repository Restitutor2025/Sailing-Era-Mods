using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIPropStore;
using Il2CppGyyx.Template;
using SlotList=Il2CppSystem.Collections.Generic.List<Il2CppClient.UILogic.UIPropStore.Slot>;

namespace Restitutor.ItemRebuild;

// 0.1.19 (user decisions 2026-09-21, handoff/UNLOCK_ITEMS_DESIGN.md):
//  - Cabin blueprints + cabin expansion (Rules.CabinUnlock): held = unlocked. Hidden in the bag,
//    no logical capacity, hidden from shops and refused at purchase while one is held.
// 0.1.21 (user decision 2026-09-21): route charts 130001-130056 (Rules.AutoRoute) are NOT used
//    automatically any more. 0.1.19 auto-use and the 0.1.20 open-line view/knowledge record/backfill
//    are removed: 0.1.20 opened UIOpenLineCtrl over the shop during Exchange and the shop view and
//    input did not come back after the view closed (user report, Fiona save). The player reads a chart
//    from the bag with the native UseSailLineDrawing flow. The only remaining rule: a chart that is
//    held or whose lane is already unlocked is hidden from shops and refused at purchase.
public sealed partial class EntryPoint
{
    static Dictionary<int,int>? laneByItem;
    static bool laneMapFailed;
    static bool unlockFollowUp;
    static void ResetUnlockItems() { unlockFollowUp=false; }
    void InstallUnlockItems()
    {
        Patch(typeof(UIPropStoreCtrl),"SetStoreGoods",null,nameof(HideOwnedGoods));
    }
    static ItemBagData? LiveBag() => PlayerDataManager.Instance?.Data?.PlayerBag?.ItemBag;
    static int Held(ItemBagData? data,int id)
    {
        if(data==null) return 0;
        int n=0;
        for(int i=0;i<data.Items.Count;i++) { var x=data.Items[i]; if(x!=null&&x.ItemId==id&&x.Number>0) n=Rules.Add(n,x.Number); }
        return n;
    }
    // Native UseSailLineDrawing scans TemplateManager.PrefabLaneValues for lane.lineMap == item id.
    static int LaneOf(int itemId)
    {
        if(laneByItem==null&&!laneMapFailed)
        {
            try
            {
                var map=new Dictionary<int,int>();
                foreach(var lane in TemplateManager.PrefabLaneValues)
                    if(lane!=null&&Rules.AutoRoute(lane.lineMap)&&!map.TryAdd(lane.lineMap,lane.tid))
                        log.Msg($"Route chart {lane.lineMap} maps to several lanes; keeping lane {map[lane.lineMap]}.");
                laneByItem=map;
                log.Msg($"Route charts mapped to lanes: {map.Count}.");
            }
            catch(Exception ex) { laneMapFailed=true; log.Error("Route lane map unavailable; route charts keep native behavior: "+ex); }
        }
        return laneByItem!=null&&laneByItem.TryGetValue(itemId,out int id)?id:0;
    }
    static bool LaneUnlocked(int lane)
    {
        var db=PlayerDataManager.Instance?.Data?.PlayerLaneData;
        return db!=null&&lane>0&&db.CheckLaneIsUnlockByLaneId(lane);
    }
    // True when buying this item again would do nothing.
    internal static bool Owned(int id)
    {
        if(Rules.CabinUnlock(id)) return Held(LiveBag(),id)>0;
        if(Rules.AutoRoute(id)) { int lane=LaneOf(id); return Held(LiveBag(),id)>0||LaneUnlocked(lane); }
        return false;
    }
    static void HideOwnedGoods(UIPropStoreCtrl __instance)
    {
        if(!enabled) return;
        var m=__instance?.Model;
        if(m==null) return;
        int removed=0;
        foreach(var list in new SlotList?[]{m.listPropStore,m.listCollege,m.listBlackMarket,m.listGuild})
        {
            if(list==null) continue;
            for(int i=list.Count-1;i>=0;i--)
                if(list[i]!=null&&Rules.UnlockItem(list[i].id)&&Owned(list[i].id)) { list.RemoveAt(i); removed++; }
        }
        if(removed>0) m.MarkDirty();
    }
    // Called first in ExchangeBegin. Returns false (exchange refused, selection corrected) when a
    // selected purchase is already owned/unlocked or selected more than once.
    static bool RefuseOwnedPurchases(UIPropStoreCtrl ctrl)
    {
        var buys=Purchases(ctrl.Model);
        if(buys==null) return true;
        var seen=new HashSet<int>();
        int dropped=0;
        for(int i=0;i<buys.Count;i++)
        {
            var row=buys[i];
            if(row==null||!row.isSelect||!Rules.UnlockItem(row.id)) continue;
            if(Owned(row.id)||!seen.Add(row.id)) { row.isSelect=false; dropped++; }
        }
        if(dropped==0) return true;
        ctrl.CalculateMoney(); ctrl.Model.MarkDirty();
        HideOwnedGoods(ctrl);
        log.Msg($"Purchase refused: {dropped} owned/unlocked or duplicate unlock item(s) deselected. Confirm again.");
        return false;
    }
    // After a completed exchange: a chart or blueprint just bought is now held, so drop it from the shop.
    static void AfterExchange(UIPropStoreCtrl ctrl) => HideOwnedGoods(ctrl);
}

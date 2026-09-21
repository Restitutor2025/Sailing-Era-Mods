using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIBag;
using RowList=Il2CppSystem.Collections.Generic.List<Il2CppClient.UILogic.UIBag.InventoryItem>;

namespace Restitutor.ItemRebuild;

public sealed partial class EntryPoint
{
    static readonly Dictionary<IntPtr,int> equipmentCounts=new();
    static void ClearEquipmentGroups()=>equipmentCounts.Clear();
    static void BagClosed() { ClearBagBadges(); ClearEquipmentGroups(); }
    static bool GroupableRecord(PlayerItemData item) => !item.IsCommerce && (Rules.Book(item.ItemId) || Unworn(item));
    static bool Unworn(PlayerItemData item)
    {
        if(item.IsCommerce||!Rules.Equipment(item.ItemId)) return false;
        var equip=PlayerDataManager.Instance?.Data?.PlayerEquipData?.GetEquipByGuid(item.Guid);
        // Missing/stale equipment state is not evidence that a record is available.
        // Deserialize constructs an independent PlayerItemData for EquipData. Its pointer
        // need not match the bag object; native equipment lookups use the persistent GUID.
        return equip!=null&&equip.Equip!=null&&equip.Equip.Guid==item.Guid&&equip.Equip.ItemId==item.ItemId&&equip.WearRoleId==0
            &&equip.EquipEffectGuid==0&&equip.SpecialEffectGuid==0&&equip.ExclusiveEffectGuid==0;
    }
    static void GroupEquipment(UIBagCtrl __instance)
    {
        ClearBagBadges(); ClearEquipmentGroups();
        var m=__instance.Model;
        if(bag==null||!Own(bag)||m?.ItemList==null||m.BagGroupList==null) return;
        var first=new Dictionary<ItemKey,InventoryItem>();
        var hidden=new HashSet<IntPtr>();
        for(int i=0;i<m.ItemList.Count;i++)
        {
            var row=m.ItemList[i];
            // 0.1.19: held cabin blueprints/expansion are the unlock condition; keep the record, hide the row.
            if(row?.ItemTemplate!=null&&Rules.CabinUnlock(row.ItemTemplate.tid)) {hidden.Add(row.Pointer);continue;}
            if(row==null||row.WearRole!=0||row.IsExclusive||row.GlobalIndex<0||row.GlobalIndex>=bag.Items.Count) continue;
            var item=bag.Items[row.GlobalIndex];
            if(item==null||item.Number<=0||row.ItemTemplate==null||row.ItemTemplate.tid!=item.ItemId||!GroupableRecord(item)) continue;
            var key=Key(item);
            if(first.TryGetValue(key,out var representative))
            {
                equipmentCounts[representative.Pointer]=Rules.Add(equipmentCounts[representative.Pointer],item.Number);
                hidden.Add(row.Pointer);
            }
            else { first.Add(key,row); equipmentCounts[row.Pointer]=item.Number; }
        }
        // Filter only UI rows. Persistent item order, GUIDs and equipment records are untouched.
        var visited=new HashSet<IntPtr>();
        void Filter(RowList list)
        {
            if(list==null||!visited.Add(list.Pointer)) return;
            for(int i=list.Count-1;i>=0;i--) if(list[i]!=null&&hidden.Contains(list[i].Pointer)) list.RemoveAt(i);
        }
        Filter(m.ItemList);
        for(int g=0;g<m.BagGroupList.Count;g++)
        {
            var list=m.BagGroupList[g]?.ItemList;
            if(list==null) continue;
            Filter(list);
            // Group zero is the native All view and shares rows with the category lists.
            if(g>0) for(int i=0;i<list.Count;i++) if(list[i]!=null) list[i].SlotIndex=i;
        }
        if(m.GroupIndex>=0&&m.GroupIndex<m.BagGroupList.Count)
        {
            int count=m.BagGroupList[m.GroupIndex].ItemList.Count;
            m.SlotIndex=count==0?0:Math.Clamp(m.SlotIndex,0,count-1);
        }
        m.MarkDirty();
    }
    static void BagDiscarded(UIBagCtrl __instance)
    {
        // Native discard removes the display row even when a stack still has units.
        // Rebuild after the entire batch, once its saved GlobalIndex values are no longer needed.
        if(bag!=null&&Own(bag)) __instance.RefreshDataModel();
    }
}

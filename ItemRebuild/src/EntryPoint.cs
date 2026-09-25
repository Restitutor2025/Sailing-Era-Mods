using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using HarmonyLib;
using MelonLoader;
using Restitutor.Core;
using MelonLoader.Utils;
using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIBag;
using Il2CppFairyGUI;
using IList = Il2CppSystem.Collections.Generic.List<Il2CppClient.PlayerStore.PlayerItemData>;

[assembly: MelonInfo(typeof(Restitutor.ItemRebuild.EntryPoint), "Restitutor Additional Item Rebuild", "0.1.24", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.ItemRebuild;

public sealed partial class EntryPoint : MelonMod
{
    static ItemBagData? bag;
    static PlayerBagDB? owner;
    static bool enabled;
    static int thread;
    static MelonLogger.Instance log = null!;
    static EntryPoint runtime = null!;
    static readonly HashSet<long> backedUp = new();
    internal static bool Eligible(PlayerItemData x) => x != null && Rules.Stack(x.ItemId) && !x.IsCommerce;
    internal readonly record struct ItemKey(int Id,int Price,int Supply,int Quality,bool Super,int State);
    internal static ItemKey Key(PlayerItemData x) => Rules.Target(x.ItemId)
        ? new(x.ItemId,0,0,0,false,0) : new(x.ItemId,x.BuyPrice,x.Supply,x.Quality,x.IsSuper,(int)x.state);
    static bool Own(ItemBagData x) => enabled && Environment.CurrentManagedThreadId == thread && bag != null && x.Pointer == bag.Pointer;
    internal static ItemBagData? CurrentBag => bag;

    public override void OnInitializeMelon()
    {
        log = LoggerInstance;
        runtime = this;
        thread = Environment.CurrentManagedThreadId;
        // Everything that touches Restitutor.Core sits in Install(): without Restitutor.Core.dll in
        // UserLibs the failure surfaces here and only this mod stays disabled.
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { log.Error("Restitutor.Core.dll is missing from UserLibs; Item Rebuild stays disabled."); }
    }
    HookSet? hooks;
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    void Install()
    {
        if (!CoreInfo.Require(log, "0.1.0")) return;
        hooks = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (hooks.InstallAll(log, "Item Rebuild install", () =>
        {
            string path = Path.Combine(MelonEnvironment.GameRootDirectory, "GameAssembly.dll");
            using var f = File.OpenRead(path);
            using var sha=SHA256.Create();
            if (Convert.ToHexString(sha.ComputeHash(f)) != "50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA")
                throw new InvalidOperationException("Unsupported GameAssembly.dll; Item Rebuild was not enabled.");
            Patch(typeof(PlayerBagDB), "Deserialize", null, nameof(Loaded));
            Patch(typeof(PlayerBagDB), "AddItem", nameof(Bind));
            Patch(typeof(PlayerBagDB), "GetFreeCapacity", null, nameof(FreeCapacity));
            Patch(typeof(UIBagCtrl), "RefreshDataModel", nameof(RefreshBag), nameof(GroupEquipment));
            Patch(typeof(UIBagCtrl), "DiscardItem", null, nameof(BagDiscarded));
            Patch(typeof(ItemBagData), "InnerAddItem", nameof(AddBegin), nameof(AddEnd), nameof(AddFinal));
            Patch(typeof(ItemBagData), "InnerRemoveItemByIndex", nameof(RemoveIndexBegin), nameof(RemoveOneEnd));
            Patch(typeof(ItemBagData), "InnerRemoveItemByGuid", nameof(RemoveGuidBegin), nameof(RemoveOneEnd));
            Patch(typeof(ItemBagData), "InnerRemoveCargoByIndexList", nameof(RemoveManyBegin), nameof(RemoveManyEnd));
            Patch(typeof(ItemBagData), "InnerRemoveItem", nameof(CountRemoving), nameof(CountRemoved));
            Patch(typeof(UIBagCtrl), "CloseHook", nameof(BagClosed));
            Patch(typeof(UIBagView), "BagSlotRenderer", null, nameof(BagLabel));
            Patch(typeof(UIBagView), "RefreshBagData", null, nameof(BagCapacity));
            Patch(typeof(GameManager), "ForceReset", nameof(Reset));
            InstallLand();
            InstallSales(); InstallSupplySlider(); InstallUnlockItems();
            enabled = true;
        }))
            log.Msg("Item Rebuild 0.1.24 ready (route charts found from PrefabLane.lineMap, no fixed id range; Restitutor.Core " + CoreInfo.Version + "; hook registration only, behaviour as 0.1.22 (sale tooltip follows grouped rows, single units toggle without popup, sale list position restored after popup): consumable/skill-book/language-book stacks, equipment display groups (weapon/armor/tool/clothes), sale quantity slider, hidden cabin blueprints, owned cabin blueprints and held/unlocked route charts hidden from shops.");
    }
    // Declared-only, name must be unique (as 0.1.22). UIManager targets are refused by Core until the
    // game is ready; the only one (ShowInputNumPromptBox) is installed lazily during play.
    void Patch(Type type, string name, string? prefix = null, string? postfix = null, string? finalizer = null)
    {
        hooks!.Hook(type,name,prefix:prefix,postfix:postfix,finalizer:finalizer);
    }
    static void Reset() { ResetLand(); ResetSales(); ResetUnlockItems(); ClearSupplySlider(); exchangeSpace=null; ClearEquipmentGroups(); ClearBagBadges(); bag=null; owner=null; backedUp.Clear(); }
    static void Bind(PlayerBagDB __instance) { if(bag!=null&&bag.Pointer!=__instance.ItemBag.Pointer) ClearBagBadges(); owner=__instance; bag=__instance.ItemBag; }
    static void Loaded(PlayerBagDB __instance)
    {
        Bind(__instance);
        if (enabled) Compact(__instance.ItemBag);
    }
    static void RefreshBag()
    {
        var data=PlayerDataManager.Instance?.Data;
        if(data?.PlayerBag==null) return;
        Bind(data.PlayerBag);
        Compact(bag);
        GroupTools();
    }
    static void Backup(IList items)
    {
        var rows=new List<object>();
        long key=0;
        for(int i=0;i<items.Count;i++)
        {
            var x=items[i];
            if(!Eligible(x)) continue;
            if(key==0) key=x.Guid;
            rows.Add(new {x.Guid,x.ItemId,x.Number,x.BuyPrice,x.Supply,x.Quality,x.IsSuper,x.IsCommerce,state=(int)x.state});
        }
        if(rows.Count==0 || backedUp.Contains(key)) return;
        string dir=Path.Combine(MelonEnvironment.UserDataDirectory,"Restitutor","ItemRebuild","inventory-backups");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+".json"),JsonSerializer.Serialize(new {version=1,utc=DateTime.UtcNow,rows},new JsonSerializerOptions{WriteIndented=true}));
        backedUp.Add(key);
    }
    internal static void Compact(ItemBagData? data)
    {
        if(data==null || !Own(data)) return;
        var items=data.Items;
        var first=new Dictionary<ItemKey,PlayerItemData>();
        var totals=new Dictionary<ItemKey,int>();
        var remove=new List<int>();
        for(int i=0;i<items.Count;i++)
        {
            var x=items[i]; if(!Eligible(x)) continue;
            if(x.Number<=0) throw new InvalidOperationException("Non-positive expedition count; refusing migration.");
            var key=Key(x);
            totals[key]=Rules.Add(totals.GetValueOrDefault(key),x.Number);
            if(!first.TryAdd(key,x)) remove.Add(i);
        }
        if(first.Count==0) return;
        bool changed=remove.Count!=0 || first.Values.Any(x=>Rules.Target(x.ItemId)&&(x.BuyPrice!=0||x.Supply!=0||x.Quality!=0||x.IsSuper||(int)x.state!=0));
        if(!changed) return;
        Backup(items); // Back up before discarding per-instance state; write failure aborts migration.
        var snapshot=new InventorySnapshot(items);
        var changedCounts=new List<PlayerItemData>();
        try
        {
        foreach(var pair in first)
        {
            var x=pair.Value;
            if(x.Number!=totals[pair.Key]) changedCounts.Add(x);
            x.Number=totals[pair.Key];
            if(Rules.Target(x.ItemId)) { x.BuyPrice=0; x.Supply=0; x.Quality=0; x.IsSuper=false; x.state=0; }
        }
        for(int i=remove.Count-1;i>=0;i--) items.RemoveAt(remove[i]);
        }
        catch { snapshot.Restore(); throw; }
        foreach(var item in changedCounts) Changed(item);
        foreach(var index in remove) Changed(snapshot.ItemAt(index),true);
        owner?.MarkDBDirty();
        log.Msg($"Compacted inventory: removed {remove.Count} duplicate records, kept quantities and consumable attributes.");
    }
    sealed class InventorySnapshot
    {
        readonly IList list;
        readonly List<(PlayerItemData Item,int Number,int Price,int Supply,int Quality,bool Super,int State)> rows=new();
        public InventorySnapshot(IList items)
        {
            list=items;
            for(int i=0;i<items.Count;i++)
            {
                var x=items[i];
                rows.Add((x,x.Number,x.BuyPrice,x.Supply,x.Quality,x.IsSuper,(int)x.state));
            }
        }
        public PlayerItemData ItemAt(int index)=>rows[index].Item;
        public void Restore()
        {
            list.Clear();
            foreach(var r in rows)
            {
                var x=r.Item;
                x.Number=r.Number; x.BuyPrice=r.Price; x.Supply=r.Supply; x.Quality=r.Quality;
                x.IsSuper=r.Super; x.state=(Il2CppClient.Const.ECargoState)r.State;
                list.Add(x); Changed(x);
            }
        }
    }
    sealed class AddState
    {
        public int Amount, Id, Count;
        public IList Items=null!;
        public InventorySnapshot Snapshot=null!;
    }
    static bool AddBegin(ItemBagData __instance, int __0, ref int __1, ref bool __6, ref bool __result, out AddState? __state, int __2=0,int __4=0,bool __5=false)
    {
        __state=null;
        if(Own(__instance)&&!CapacityAllowsAdd(__0,__1,__2,__4,__5,ref __6)) {__result=false;return false;}
        if(!Own(__instance)||!Rules.Stack(__0)||__1<=0) return true;
        var items=__instance.Items;
        long total=0;
        for(int i=0;i<items.Count;i++) if(Eligible(items[i])&&items[i].ItemId==__0) total+=items[i].Number;
        if(total+__1>int.MaxValue) { __result=false; return false; }
        __state=new AddState{Amount=__1,Id=__0,Count=items.Count,Items=items,Snapshot=new InventorySnapshot(items)};
        // Native constructor must receive exactly 1 while the native template pileNum stays 1.
        // Original gain tracking and PlayerBagDB dirty notification still run once.
        __1=1;
        // Native ctor gives these non-cargo types Supply=1. A negative quality asks the
        // native cargo-template fallback to decide, so do not predict it for capacity bypass.
        var incoming=new ItemKey(__0,__2,1,__4,__5,0);
        for(int i=0;i<items.Count;i++)
            if(Eligible(items[i])&&items[i].ItemId==__0&&(Rules.Target(__0)||(__4>=0&&Key(items[i])==incoming))) {__6=false;break;}
        return true;
    }
    static void AddEnd(ItemBagData __instance, bool __result, AddState? __state)
    {
        if(__state==null||!__result) return;
        var items=__state.Items;
        if(items.Count!=__state.Count+1 || items[items.Count-1].ItemId!=__state.Id)
            throw new InvalidOperationException("Unexpected native add result; quantity adapter cannot commit.");
        items[items.Count-1].Number=__state.Amount;
        Compact(__instance);
    }
    static Exception? AddFinal(Exception? __exception, AddState? __state)
    {
        if(__exception!=null)
        {
            __state?.Snapshot.Restore();
            log.Error("Item add failed (inventory restored): "+__exception);
        }
        return __exception;
    }
    static bool RemoveIndex(ItemBagData __instance,int __0,ref bool __result)
    {
        if(!Own(__instance)||__0<0||__0>=__instance.Items.Count) return true;
        var x=__instance.Items[__0];
        if(!Eligible(x)||x.Number<=1) return true;
        x.Number--; Changed(x); __result=true; return false;
    }
    static bool RemoveGuid(ItemBagData __instance,long __0,ref bool __result)
    {
        if(!Own(__instance)) return true;
        for(int i=0;i<__instance.Items.Count;i++) if(__instance.Items[i].Guid==__0) return RemoveIndex(__instance,i,ref __result);
        return true;
    }
    static void RemoveMany(ItemBagData __instance, ref Il2CppSystem.Collections.Generic.List<int> __0)
    {
        if(!Own(__instance)) return;
        var remaining=new Il2CppSystem.Collections.Generic.List<int>();
        var seen=new HashSet<int>();
        for(int i=0;i<__0.Count;i++)
        {
            int index=__0[i]; if(!seen.Add(index)) continue;
            if(index>=0&&index<__instance.Items.Count&&Eligible(__instance.Items[index])&&__instance.Items[index].Number>1)
            {
                var item=__instance.Items[index]; item.Number--; Changed(item);
            }
            else remaining.Add(index);
        }
        __0=remaining;
    }
    static void BagLabel(UIBagView __instance,int __0,GObject __1)
    {
        var slot=__1.TryCast<Il2CppBag.UIbtnSlot>();
        var common=slot?.compSlotCommon?.TryCast<Il2CppCommon.UIcompSlotCommon>();
        if(common==null) return;
        // Virtualized slots can now represent a different item: discard the old binding first.
        if(bagBadges.TryGetValue(common.Pointer,out var old)) ForgetBadge(old);
        if(bag==null) return;
        var m=__instance._model;
        if(m==null||m.BagGroupList==null||m.GroupIndex<0||m.GroupIndex>=m.BagGroupList.Count) return;
        var list=m.BagGroupList[m.GroupIndex].ItemList;
        if(list==null||__0<0||__0>=list.Count) return;
        var row=list[__0];
        if(row==null) return;
        int index=row.GlobalIndex;
        if(index<0||index>=bag.Items.Count) return;
        var x=bag.Items[index];
        int count=x.Number;
        if(!Eligible(x)&&!equipmentCounts.TryGetValue(row.Pointer,out count)) return;
        // Never label an unrelated image if another mod leaves model indices stale.
        if(row.ItemTemplate==null||row.ItemTemplate.tid!=x.ItemId) return;
        if(common.title==null||common.loaderIcon==null) return;
        var badge=new BagBadge(common,x);
        bagBadges[common.Pointer]=badge;
        if(!badgesByGuid.TryGetValue(x.Guid,out var views)) badgesByGuid[x.Guid]=views=new();
        views.Add(badge);
        badge.Update(count);
    }
}

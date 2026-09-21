using Il2CppFairyGUI;
using Il2CppClient.PlayerStore;
using UnityEngine;

namespace Restitutor.ItemRebuild;

public sealed partial class EntryPoint
{
    static readonly Dictionary<IntPtr,BagBadge> bagBadges=new();
    static readonly Dictionary<long,HashSet<BagBadge>> badgesByGuid=new();
    static readonly HashSet<BagBadge> dirtyBadges=new();
    public override void OnUpdate()
    {
        // No timer, inventory lookup or allocation while there are no changes.
        if(!enabled||dirtyBadges.Count==0||Environment.CurrentManagedThreadId!=thread) return;
        RefreshBagBadges();
    }
    static void ForgetBadge(BagBadge badge)
    {
        dirtyBadges.Remove(badge);
        if(badgesByGuid.TryGetValue(badge.Guid,out var views))
        {
            views.Remove(badge);
            if(views.Count==0) badgesByGuid.Remove(badge.Guid);
        }
        bagBadges.Remove(badge.Common.Pointer);
        badge.Dispose();
    }
    static void ClearBagBadges()
    {
        foreach(var badge in bagBadges.Values) badge.Dispose();
        bagBadges.Clear(); badgesByGuid.Clear(); dirtyBadges.Clear();
    }
    static void Changed(PlayerItemData item,bool removed=false)
    {
        if(!badgesByGuid.TryGetValue(item.Guid,out var views)) return;
        foreach(var badge in views)
            if(badge.ItemId==item.ItemId)
            {
                badge.Item=item; badge.Removed=removed; dirtyBadges.Add(badge);
            }
    }
    // The native count-removal path writes Number directly (including zero before deletion).
    // Queue only displayed records of the requested type; read their final values next frame.
    static void CountRemoving(ItemBagData __instance,int __0,out List<(BagBadge Badge,int Count)>? __state)
    {
        __state=null;
        if(!Own(__instance)||!Rules.Stack(__0)) return;
        foreach(var badge in bagBadges.Values)
            if(badge.ItemId==__0) (__state??=new()).Add((badge,badge.Item.Number));
    }
    static void CountRemoved(List<(BagBadge Badge,int Count)>? __state)
    {
        if(__state==null) return;
        foreach(var row in __state)
            if(row.Badge.Item.Number!=row.Count&&bagBadges.TryGetValue(row.Badge.Common.Pointer,out var live)&&ReferenceEquals(live,row.Badge)) dirtyBadges.Add(row.Badge);
    }
    static void RefreshBagBadges()
    {
        if(dirtyBadges.Count==0) return;
        var work=dirtyBadges.ToArray(); dirtyBadges.Clear();
        foreach(var badge in work)
        {
            if(badge.Common.isDisposed||!badge.Common.onStage||bag==null)
            { ForgetBadge(badge); continue; }
            badge.Update(badge.Removed?0:badge.Item.Number);
        }
    }
    sealed class BagBadge
    {
        public readonly Il2CppCommon.UIcompSlotCommon Common;
        public readonly GTextField Label;
        public PlayerItemData Item;
        public bool Removed;
        public readonly long Guid;
        public readonly int ItemId;
        readonly GTextField original;
        readonly bool wasVisible;
        readonly int fontSize;
        public BagBadge(Il2CppCommon.UIcompSlotCommon common,PlayerItemData item)
        {
            Common=common; Item=item; Guid=item.Guid; ItemId=item.ItemId;
            original=common.title; wasVisible=original.visible;
            var format=new TextFormat(); format.CopyFrom(original.textFormat);
            fontSize=Math.Max(1,(int)Math.Round(Math.Round(format.size/2.0)*1.1));
            format.size=fontSize; format.color=Color.white; format.gradientColor=null; format.align=AlignType.Right;
            // A new child has no package gear, relation, pivot or group that can move the title.
            Label=new GTextField {touchable=false,sortingOrder=int.MaxValue,autoSize=AutoSizeType.Shrink,singleLine=true};
            Label.textFormat=format; Label.align=AlignType.Right; Label.verticalAlign=VertAlignType.Bottom; Label.stroke=1; Label.strokeColor=Color.black;
            common.AddChild(Label);
            original.visible=false;
        }
        public void Update(int count)
        {
            if(Label.isDisposed||Common.isDisposed) return;
            var icon=Common.loaderIcon;
            if(icon==null||icon.isDisposed||count<=0) {Label.visible=false;return;}
            // ConstructFromXML binds loaderIcon with GetChildAt: it shares this parent.
            // Do not use Stage/global transforms during a virtual-list renderer callback.
            float left=icon.xMin, top=icon.yMin;
            float w=icon.actualWidth,h=icon.actualHeight;
            if(w<=0||h<=0) {Label.visible=false;return;}
            // Reserve space for the frame, glyph overhang and outline inside the illustration.
            float inset=Math.Min(14,Math.Min(w,h)/5);
            float height=Math.Min(h-2*inset,fontSize*1.6f+4);
            Label.SetSize(w-2*inset,height);
            Label.SetXY(left+inset,top+h-inset-height);
            string text="x"+count;
            if(Label.text!=text) Label.text=text;
            Label.visible=true;
            original.visible=false;
        }
        public void Dispose()
        {
            if(!Label.isDisposed) Label.Dispose();
            if(!original.isDisposed) original.visible=wasVisible;
        }
    }
}

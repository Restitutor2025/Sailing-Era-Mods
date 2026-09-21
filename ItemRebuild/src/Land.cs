using Il2CppClient.Manager;
using Il2CppClient.UILogic.UILandExplore;
using Il2CppClient.Utils;
using Il2CppClient.UILogic.UICommonInputNum;
using Il2CppFairyGUI;
using GoodsList=Il2CppSystem.Collections.Generic.List<Il2CppClient.UILogic.UILandExplore.Goods>;

namespace Restitutor.ItemRebuild;

public sealed partial class EntryPoint
{
    sealed class LandRow
    {
        public Goods Goods=null!;
        public int Stock,Selected;
        public float UnitWeight;
    }
    static readonly Dictionary<int,LandRow> landRows=new();
    static UILandExploreModel? landModel;
    static UILandExploreCtrl? landCtrl;
    static int landGeneration;
    static Il2CppSystem.Action<long>? chooseCallback;
    static UICommonInputNumCtrl? popup;
    static int popupLimit;
    static bool popupBoundaryReady;
    void InstallLand()
    {
        Patch(typeof(UILandExploreCtrl),"SetDeportGoods",nameof(LandBegin),nameof(LandBuilt));
        Patch(typeof(UILandExploreCtrl),"OnClickListDepot",nameof(ChooseLand));
        Patch(typeof(UILandExploreCtrl),"JumpToLandExplore",nameof(JumpBegin),null,nameof(JumpFinal));
        Patch(typeof(UILandExploreCtrl),"CloseHook",null,nameof(LandClosed));
        Patch(typeof(UILandExploreView),"ListDepotRender",null,nameof(DepotLabel));
        Patch(typeof(UILandExploreView),"ListLandBagRender",null,nameof(CarryLabel));
        Patch(typeof(UILandExploreView),"Refresh",null,nameof(LandSupplyRendered));
        Patch(typeof(UICommonInputNumCtrl),"OnClickBtnAdd",nameof(PopupAdd));
        Patch(typeof(UICommonInputNumCtrl),"OnClickBtnReduce",nameof(PopupReduce));
        Patch(typeof(UICommonInputNumCtrl),"OnValueChange",nameof(PopupValue));
        Patch(typeof(UICommonInputNumCtrl),"OnClickBtnReturn",nameof(PopupReturn),nameof(PopupClosed));
        Patch(typeof(UICommonInputNumCtrl),"OnAction_B",nameof(PopupCancelAction));
        Patch(typeof(UICommonInputNumCtrl),"OnAction_A",nameof(PopupApplyAction));
        Patch(typeof(UICommonInputNumModel),"set_CurNum",nameof(PopupSet));
        Patch(typeof(UICommonInputNumCtrl),"OpenInputNumPanel",nameof(PopupOpening));
    }
    // Resolving a UIManager native method forces its native .cctor, which creates
    // FairyGUI.Stage. Never run this during Melon initialization before Unity UI startup.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    void InstallPopupBoundary()
    {
        Patch(typeof(UIManager),"ShowInputNumPromptBox",nameof(OtherPopup));
    }
    static bool EnsurePopupBoundary()
    {
        if(popupBoundaryReady) return true;
        try
        {
            log.Msg("Registering deferred numeric-popup boundary (expedition UI is active).");
            runtime.InstallPopupBoundary();
            popupBoundaryReady=true;
            log.Msg("Deferred numeric-popup boundary ready.");
            return true;
        }
        catch(Exception ex) { log.Error("Quantity popup could not be enabled: "+ex); return false; }
    }
    static void CancelPopup()
    {
        chooseCallback=null;
        if(popup!=null) popup.OnClickBtnReturn();
        popup=null;
        salePopup=false; DisposeSaleSlider();
    }
    static void ResetLand() { CancelPopup(); ClearQuick(landQuick);ClearCarryBadges(); landGeneration++; landRows.Clear(); landModel=null;landCtrl=null; }
    static void LandClosed() => ResetLand();
    static void LandBegin()
    {
        RefreshBag();
        ResetLand();
    }
    static void LandBuilt(UILandExploreCtrl __instance)
    {
        landModel=__instance.Model;
        landCtrl=__instance;
        if(bag==null) return;
        for(int i=0;i<landModel.ListDeport.Count;i++)
        {
            var g=landModel.ListDeport[i]; if(g==null||!Rules.Target(g.GoodsID)) continue;
            int stock=0;
            for(int j=0;j<bag.Items.Count;j++)
            {
                var x=bag.Items[j];
                if(Eligible(x)&&x.ItemId==g.GoodsID) stock=Rules.Add(stock,x.Number);
            }
            landRows[g.GoodsID]=new LandRow{Goods=g,Stock=stock,UnitWeight=g.Heavy};
        }
    }
    static bool ChooseLand(UILandExploreCtrl __instance)
        =>ChooseLandItem(__instance,__instance.Model?.CurGoods,false);
    static bool ChooseLandItem(UILandExploreCtrl __instance,Il2CppClient.UILogic.UILandExplore.Goods? g,bool maximum)
    {
        var m=__instance.Model;
        if(m==null||m.ListLandBag==null) return true;
        if(landModel==null||m.Pointer!=landModel.Pointer||g==null||!landRows.TryGetValue(g.GoodsID,out var row)) return true;
        if(row.Stock<=0) return false;
        if(!maximum&&!EnsurePopupBoundary()) return false;
        CancelPopup();
        int generation=landGeneration;
        int current=Contains(m.ListLandBag,g.Pointer)?row.Selected:0;
        int limit=Rules.Select(3,row.Stock,row.UnitWeight,Math.Max(0,m.MaxWeight-m.CurWeight+row.UnitWeight*current));
        chooseCallback=(Il2CppSystem.Action<long>)(Action<long>)(value=>
        {
            if(generation!=landGeneration||landModel==null||landModel.Pointer!=m.Pointer||m.ListLandBag==null) return;
            int old=Contains(m.ListLandBag,g.Pointer)?row.Selected:0;
            float available=Math.Max(0,m.MaxWeight-m.CurWeight+row.UnitWeight*old);
            int n=Rules.Select((int)Math.Clamp(value,0,3),Math.Min(3,row.Stock),row.UnitWeight,available);
            // Revalidate live stock: a callback must not commit stale quantity after another UI change.
            int stock=bag?.GetItemAmount(g.GoodsID)??0;
            n=Math.Min(n,stock);
            int index=IndexOf(m.ListLandBag,g.Pointer);
            if(n==0 && index>=0) m.ListLandBag.RemoveAt(index);
            else if(n>0 && index<0) m.ListLandBag.Insert(Math.Min(1,m.ListLandBag.Count),g);
            row.Selected=n;
            g.IsSelect=n>0;
            g.Heavy=row.UnitWeight*Math.Max(1,n);
            m.ItemWeight=(float)Math.Round(Math.Max(0,m.ItemWeight+row.UnitWeight*(n-old)),2);
            m.MarkDirty();
        });
        popupLimit=limit;
        if(maximum)
        {
            var apply=chooseCallback;chooseCallback=null;apply?.Invoke(limit);return false;
        }
        openingOwnPopup=true;
        try {
        UICommonInputNumCtrl.Instance.OpenInputNumPanel(Math.Min(limit,current>0?current:1),limit,limit,chooseCallback,null);
        } finally { openingOwnPopup=false; }
        return false;
    }
    static bool openingOwnPopup;
    static void OtherPopup() { if(!openingOwnPopup) CancelPopup(); }
    static void PopupOpening(UICommonInputNumCtrl __instance)
    {
        if(openingOwnPopup) popup=__instance;
        else CancelPopup();
    }
    static bool IsPopup(UICommonInputNumCtrl c)=>popup!=null&&popup.Pointer==c.Pointer;
    static bool PopupSet(UICommonInputNumModel __instance,long __0)
    {
        if(popup==null||popup.Model.Pointer!=__instance.Pointer) return true;
        long n=Math.Clamp(__0,0,popupLimit);
        __instance._curNum=n;
        SyncSaleSlider(n);
        // Native input parsing already clamps MaxNum before calling this setter.
        // Refresh even if the clamped value is unchanged so typed "999" visibly becomes "3".
        __instance.MarkDirty();
        return false;
    }
    static bool PopupAdd(UICommonInputNumCtrl __instance)
    {
        if(!IsPopup(__instance)) return true;
        __instance.Model.CurNum=__instance.Model.CurNum>=popupLimit?0:__instance.Model.CurNum+1;
        return false;
    }
    static bool PopupReduce(UICommonInputNumCtrl __instance)
    {
        if(!IsPopup(__instance)) return true;
        __instance.Model.CurNum=__instance.Model.CurNum<=0?popupLimit:__instance.Model.CurNum-1;
        return false;
    }
    static bool PopupValue(UICommonInputNumCtrl __instance)=>!IsPopup(__instance);
    static void PopupReturn(UICommonInputNumCtrl __instance)
    {
        if(IsPopup(__instance)) chooseCallback=null;
    }
    static bool PopupCancelAction(UICommonInputNumCtrl __instance)
    {if(!IsPopup(__instance)) return true;CancelPopup();return false;}
    static bool PopupApplyAction(UICommonInputNumCtrl __instance)
    {
        if(!IsPopup(__instance)) return true;
        var callback=chooseCallback;chooseCallback=null;
        try {callback?.Invoke(Math.Clamp(__instance.Model.CurNum,0,popupLimit));}
        finally {__instance.OnClickBtnReturn();}
        return false;
    }
    static void PopupClosed(UICommonInputNumCtrl __instance)
    {
        if(IsPopup(__instance)) { bool sale=salePopup; popup=null; chooseCallback=null; salePopup=false; DisposeSaleSlider(); if(sale) RestoreSaleView(); }
    }
    static bool Contains(GoodsList? xs,IntPtr ptr)=>IndexOf(xs,ptr)>=0;
    static int IndexOf(GoodsList? xs,IntPtr ptr)
    {
        if(xs==null) return -1;
        // Native ShowHook reserves index zero by adding null for the supply row.
        for(int i=0;i<xs.Count;i++)
        {
            var item=xs[i];
            if(item!=null&&item.Pointer==ptr) return i;
        }
        return -1;
    }
    static void DepotLabel(UILandExploreView __instance,int __0,GObject __1)
    {
        ClearCarryBadge(__1);
        BindLandQuick(__instance,__0,__1);
        var m=__instance._model;
        if(landModel==null||m==null||m.Pointer!=landModel.Pointer||m.ListDeport==null||__0<0||__0>=m.ListDeport.Count) return;
        var g=m.ListDeport[__0];
        if(g==null||!landRows.TryGetValue(g.GoodsID,out var row)) return;
        var b=__1?.TryCast<Il2CppUILandExplore.UIButtonItem>();
        if(b==null||b.texCount==null) return;
        b.title=TextLibUtils.Text(g.Name)+" X"+row.Stock;
        b.texCount.text="X"+row.Stock;
        b.texCount.visible=true;
        if(b.texHeavy!=null) b.texHeavy.text=row.UnitWeight.ToString("0.##");
    }
    static void CarryLabel(UILandExploreView __instance,int __0,GObject __1)
    {
        ClearCarryBadge(__1);
        if(__1!=null&&landQuick.TryGetValue(__1.Pointer,out var oldQuick))oldQuick.Dispose();
        if(__0==0&&__1!=null)BindSupplyQuick(__instance,__1);
        var m=__instance._model;
        // Slot zero is rendered from model supply fields, not a Goods instance.
        if(landModel==null||m==null||m.Pointer!=landModel.Pointer||m.ListLandBag==null||__0<=0||__0>=m.ListLandBag.Count) return;
        var g=m.ListLandBag[__0];
        if(g==null||!landRows.TryGetValue(g.GoodsID,out var row)) return;
        var b=__1?.TryCast<Il2CppUILandExplore.UIButtonItem>();
        if(b==null||b.texCount==null) return;
        b.title=TextLibUtils.Text(g.Name)+" X"+row.Selected;
        b.texCount.text="X"+row.Selected;
        b.texCount.visible=true;
        DrawCarryBadge(b,row.Selected);
    }
    sealed class JumpState
    {
        public UILandExploreModel Model=null!;
        public GoodsList Original=null!;
    }
    static void JumpBegin(UILandExploreCtrl __instance,out JumpState? __state)
    {
        __state=null;
        var m=__instance.Model;
        if(landModel==null||m==null||m.Pointer!=landModel.Pointer||m.ListLandBag==null) return;
        var expanded=new GoodsList();
        for(int i=0;i<m.ListLandBag.Count;i++)
        {
            var g=m.ListLandBag[i];
            // Keep the supply sentinel in place; native departure filters null entries.
            if(g==null) { expanded.Add(null!); continue; }
            if(!landRows.TryGetValue(g.GoodsID,out var row)) { expanded.Add(g); continue; }
            int count=Math.Clamp(row.Selected,0,Math.Min(3,bag?.GetItemAmount(g.GoodsID)??0));
            if(count!=row.Selected) throw new InvalidOperationException("Expedition stock changed before departure; reopen preparation.");
            for(int n=0;n<count;n++) expanded.Add(new Goods{GoodsID=g.GoodsID,Name=g.Name,Image=g.Image,Bg=g.Bg,Desc=g.Desc,TypeDesc=g.TypeDesc,Heavy=row.UnitWeight,IsSelect=true});
        }
        __state=new JumpState{Model=m,Original=m.ListLandBag};
        m.ListLandBag=expanded;
    }
    static Exception? JumpFinal(Exception? __exception,JumpState? __state)
    {
        if(__state!=null) __state.Model.ListLandBag=__state.Original;
        return __exception;
    }
}

using Il2CppClient.UILogic.UIPropStore;
using Il2CppFairyGUI;
using UnityEngine;

namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    sealed class SaleDisplayRow
    {
        public int Index;
        public Slot First=null!;
        public readonly List<Slot> Units=new();
        public bool Grouped;
    }
    readonly record struct SaleDisplayKey(ItemKey Item,int Price,int Type,bool Only,bool Task,bool Parliament,bool Exclusive);
    static readonly List<SaleDisplayRow> saleDisplay=new();
    static UIPropStoreView? saleDisplayView;
    static IntPtr saleDisplayList;
    static readonly Dictionary<IntPtr,GTextField> saleLabels=new();
    // 0.1.22: one-shot state, set by events only (no polling).
    static bool curGoodsPending;
    static (IntPtr view,int index,float x,float y)? saleViewMemo;
    static bool saleViewPending;
    static bool IsPlayerRow(UIPropStoreModel model,Slot? slot)
    {
        var rows=model.ListCurPlay;
        if(slot==null||rows==null) return false;
        for(int i=0;i<rows.Count;i++) if(rows[i]?.Pointer==slot.Pointer) return true;
        return false;
    }
    // Returns true when CurGoods was changed.
    static bool AlignCurGoods(UIPropStoreView view,UIPropStoreModel model)
    {
        var list=view.ListPlayer;
        if(list==null||saleDisplay.Count==0) return false;
        int index=list.selectedIndex;
        if(index>=saleDisplay.Count) { index=saleDisplay.Count-1; list.selectedIndex=index; }
        if(index<0) return false;
        var want=saleDisplay[index].First;
        var cur=model.CurGoods;
        if(cur!=null&&cur.Pointer==want.Pointer) return false;
        model.CurGoods=want; return true;
    }
    static void CurGoodsSelected(UIPropStoreCtrl __instance)
    {
        var model=__instance.Model;var view=__instance.View;
        if(model==null||view==null||!IsPlayerRow(model,model.CurGoods)) return;
        if(!HasSaleDisplay(view)) { curGoodsPending=true; return; } // list rebuilt; align after its Refresh
        if(AlignCurGoods(view,model)) model.MarkDirty();
    }
    static void RememberSaleView(UIPropStoreCtrl ctrl)
    {
        var view=ctrl.View;var list=view?.ListPlayer;
        if(view==null||list==null) { saleViewMemo=null; return; }
        var pane=list.scrollPane;
        saleViewMemo=(view.Pointer,list.selectedIndex,pane?.posX??0,pane?.posY??0);
    }
    static void RestoreSaleView()
    {
        if(saleViewMemo==null||saleDisplayView==null||saleDisplayView.Pointer!=saleViewMemo.Value.view) { saleViewMemo=null; return; }
        ApplySaleView(saleDisplayView);
        saleViewPending=true; // once more after the next Refresh, in case focus return resets it later
    }
    static void ApplySaleView(UIPropStoreView view)
    {
        var memo=saleViewMemo;var list=view.ListPlayer;
        if(memo==null||list==null) return;
        if(memo.Value.index>=0&&memo.Value.index<list.numItems) list.selectedIndex=memo.Value.index;
        var pane=list.scrollPane;
        if(pane!=null) { pane.posX=memo.Value.x; pane.posY=memo.Value.y; }
        var model=view._model;
        if(model!=null&&IsPlayerRow(model,model.CurGoods)&&AlignCurGoods(view,model)) model.MarkDirty();
    }
    static void ClearSaleDisplay()
    {
        curGoodsPending=false;saleViewMemo=null;saleViewPending=false;
        foreach(var label in saleLabels.Values) if(!label.isDisposed) label.Dispose();
        saleLabels.Clear();saleDisplay.Clear();saleDisplayView=null;saleDisplayList=IntPtr.Zero;
    }
    static void SaleDisplayBegin(UIPropStoreView __instance)
    {
        saleDisplay.Clear();saleDisplayView=__instance;
        var rows=__instance._model?.ListCurPlay;
        saleDisplayList=rows?.Pointer??IntPtr.Zero;
        if(rows==null||bag==null||!Own(bag)) return;
        var groups=new Dictionary<SaleDisplayKey,SaleDisplayRow>();
        for(int i=0;i<rows.Count;i++)
        {
            var row=rows[i];if(row==null) continue;
            bool grouped=SaleTarget(row,out var item);
            var key=grouped?new SaleDisplayKey(Key(item!),row.price,(int)row.type,row.IsOnly,row.isTask,row.isParliamentTask,row.IsExclusive):default;
            if(grouped&&groups.TryGetValue(key,out var existing)) {existing.Units.Add(row);continue;}
            var display=new SaleDisplayRow {Index=i,First=row,Grouped=grouped};
            display.Units.Add(row);saleDisplay.Add(display);
            if(grouped) groups.Add(key,display);
        }
    }
    static bool HasSaleDisplay(UIPropStoreView? view)=>view!=null&&saleDisplayView!=null&&view.Pointer==saleDisplayView.Pointer
        &&view._model?.ListCurPlay?.Pointer==saleDisplayList&&bag!=null&&Own(bag);
    static void SaleDisplayEnd(UIPropStoreView __instance)
    {
        if(!HasSaleDisplay(__instance)) return;
        __instance.ListPlayer.numItems=saleDisplay.Count;
        if(__instance.ListPlayer.selectedIndex>=saleDisplay.Count) __instance.ListPlayer.selectedIndex=-1;
        if(saleViewPending) { saleViewPending=false; ApplySaleView(__instance); saleViewMemo=null; }
        if(curGoodsPending)
        {
            curGoodsPending=false;
            var model=__instance._model;
            if(model!=null&&IsPlayerRow(model,model.CurGoods)&&AlignCurGoods(__instance,model)) model.MarkDirty();
        }
        if(owner!=null&&__instance.UIContent!=null) __instance.UIContent.texItemCapacity.text=$"{Occupied().Count}/{owner.Capacity}";
    }
    static void ClearSaleLabel(int __0,GObject __1)
    {
        if(__1!=null&&saleLabels.Remove(__1.Pointer,out var label)&&!label.isDisposed) label.Dispose();
    }
    static bool SaleRenderBegin(UIPropStoreView __instance,ref int __0,GObject __1,out SaleDisplayRow? __state)
    {
        __state=null;ClearSaleLabel(__0,__1);
        if(!HasSaleDisplay(__instance)) return true;
        if(__0<0||__0>=saleDisplay.Count) return false;
        __state=saleDisplay[__0];__0=__state.Index;
        return true; // Original renderer binds the real Slot to button.data and retains unit price.
    }
    static void SaleRenderEnd(GObject __1,SaleDisplayRow? __state)
    {
        if(saleDisplayView!=null)BindStoreQuick(saleDisplayView,__1);
        var button=__1?.TryCast<Il2CppUIPropStore.UIbtnSlot>();
        if(button==null||__state==null||!__state.Grouped) return;
        int selected=__state.Units.Count(x=>x.isSelect);
        var text=new GTextField {touchable=false,singleLine=true,autoSize=AutoSizeType.Shrink,sortingOrder=int.MaxValue};
        text.textFormat=new TextFormat {size=27,color=Color.white,align=AlignType.Right};
        text.stroke=1;text.strokeColor=Color.black;
        text.text=selected>0?$"{selected}/{__state.Units.Count}":$"x{__state.Units.Count}";
        text.SetXY(4,4);text.SetSize(Math.Max(1,button.width-8),36);button.AddChild(text);
        saleLabels[button.Pointer]=text;
        button.ctrlMulti.SetSelectedIndex(selected>0?1:0);
    }
    static Slot? VisibleSale(UIPropStoreCtrl ctrl,int index)
    {
        if(HasSaleDisplay(ctrl.View)) return index>=0&&index<saleDisplay.Count?saleDisplay[index].First:null;
        var list=ctrl.Model.ListCurPlay;return index>=0&&index<list.Count?list[index]:null;
    }
}

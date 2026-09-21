using Il2CppFairyGUI;
using Il2CppClient.UILogic.UIPropStore;
using Il2CppClient.UILogic.UILandExplore;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    static UIPropStoreCtrl? quickStore;
    static readonly Dictionary<IntPtr,QuickBinding> storeQuick=new(),landQuick=new();
    sealed class QuickBinding : IDisposable
    {
        readonly GObject target;
        readonly EventCallback1 pressed,removed;
        readonly Dictionary<IntPtr,QuickBinding> owner;
        public Action Action;
        bool disposed;
        public QuickBinding(Dictionary<IntPtr,QuickBinding> owner,GObject target,Action action)
        {
            this.owner=owner;this.target=target;Action=action;
            pressed=(EventCallback1)(Action<EventContext>)(e=>
            {
                if(disposed||e.inputEvent?.button!=2||!target.onStage||!target.visible)return;
                e.StopPropagation();Action();
            });
            removed=(EventCallback1)(Action<EventContext>)(_=>Dispose());
            target.onTouchBegin.Add(pressed);target.onRemovedFromStage.Add(removed);
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            if(!target.isDisposed){target.onTouchBegin.Remove(pressed);target.onRemovedFromStage.Remove(removed);}
            owner.Remove(target.Pointer);Action=()=>{};
        }
    }
    static void ClearQuick(Dictionary<IntPtr,QuickBinding> bindings)
    {foreach(var b in bindings.Values.ToArray())b.Dispose();bindings.Clear();}
    static void BindQuick(Dictionary<IntPtr,QuickBinding> bindings,GObject? target,Action action)
    {
        if(target==null||target.isDisposed)return;
        if(bindings.TryGetValue(target.Pointer,out var b))b.Action=action;
        else bindings[target.Pointer]=new QuickBinding(bindings,target,action);
    }
    static void BindStoreQuick(UIPropStoreView __instance,GObject __1)
    {
        var model=__instance._model;
        BindQuick(storeQuick,__1,()=>
        {
            var ctrl=quickStore;
            if(ctrl==null||model==null||ctrl.Model.Pointer!=model.Pointer||!__instance.IsInputActive||popup!=null)return;
            var slot=__1.data?.TryCast<Slot>();if(slot!=null)QuickStoreMax(ctrl,slot);
        });
    }
    static bool QuickStoreMax(UIPropStoreCtrl ctrl,Slot selected)
    {
        var sales=ctrl.Model.ListCurPlay;
        bool belongs=false;
        for(int i=0;i<sales.Count;i++)if(sales[i]?.Pointer==selected.Pointer){belongs=true;break;}
        if(belongs)
        {
            if(selected.wearRole!=0||selected.isTask||selected.isParliamentTask)return false;
            var rows=SaleSlots(ctrl,selected);
            if(Rules.Stack(selected.id)||Rules.GroupedRecord(selected.id))
            {
                if(rows.Count==0)return false;
                foreach(var row in rows)row.isSelect=true;
            }
            else
            {
                // Ordinary sellable items use native per-unit rows too.
                for(int i=0;i<sales.Count;i++)
                {
                    var row=sales[i];
                    if(row!=null&&row.id==selected.id&&row.price==selected.price&&row.wearRole==0&&!row.isTask&&!row.isParliamentTask)
                        row.isSelect=true;
                }
            }
        }
        else
        {
            var buys=Purchases(ctrl.Model);if(buys==null)return false;
            bool present=false;
            for(int i=0;i<buys.Count;i++)if(buys[i]?.Pointer==selected.Pointer){present=true;break;}
            if(!present)return false;
            for(int i=0;i<buys.Count;i++)if(buys[i]!=null&&buys[i].id==selected.id)buys[i].isSelect=true;
        }
        RecountSale(ctrl);return true;
    }
    // The game's middle button is also bound to R2/same-kind. Suppress that duplicate
    // route only for a physical middle press; keyboard/controller R2 remains native.
    static bool StoreMiddleLegacy()=>!UnityEngine.Input.GetMouseButton(2)&&!UnityEngine.Input.GetMouseButtonUp(2);
    static void BindLandQuick(UILandExploreView view,int index,GObject target)
    {
        var model=view._model;
        var goods=model?.ListDeport!=null&&index>=0&&index<model.ListDeport.Count?model.ListDeport[index]:null;
        int generation=landGeneration;
        BindQuick(landQuick,target,()=>
        {
            if(landCtrl==null||landModel==null||model==null||model.Pointer!=landModel.Pointer
                ||generation!=landGeneration||!view.IsInputActive||popup!=null||goods==null)return;
            ChooseLandItem(landCtrl,goods,true);
        });
    }
}

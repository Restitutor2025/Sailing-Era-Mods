using Il2CppClient.UILogic.UILandExploreSupply;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    static QuantityTrack? supplyTrack;
    static IntPtr supplyModel;
    static QuantityActions? supplyActions;
    static void ClearSupplySlider() {supplyActions?.Dispose();supplyActions=null;supplyTrack?.Dispose();supplyTrack=null;supplyModel=IntPtr.Zero;}
    void InstallSupplySlider()
    {
        Patch(typeof(UILandExploreSupplyView),"Refresh",null,nameof(SupplyRendered));
        Patch(typeof(UILandExploreSupplyCtrl),"ShowHook",nameof(ClearSupplySlider));
        Patch(typeof(UILandExploreSupplyCtrl),"OnAction_A",nameof(SupplyApplyAction));
        Patch(typeof(UILandExploreSupplyCtrl),"OnAction_B",nameof(SupplyCancelAction));
    }
    static void SupplyCancelAction(UILandExploreSupplyCtrl __instance)
    {if(supplyModel==__instance.Model.Pointer)ClearSupplySlider();}
    static bool SupplyApplyAction(UILandExploreSupplyCtrl __instance)
    {
        if(supplyTrack==null||supplyTrack.Panel.isDisposed||supplyModel!=__instance.Model.Pointer)return true;
        ClearSupplySlider();__instance.OnClickBtnOK();return false;
    }
    static void SupplyRendered(UILandExploreSupplyView __instance)
    {
        var model=__instance._model;var parent=__instance.UIContent;
        var ctrl=UILandExploreSupplyCtrl.Instance;
        if(model==null||parent==null||parent.isDisposed||ctrl.Model.Pointer!=model.Pointer) return;
        if(supplyTrack!=null&&(supplyTrack.Panel.isDisposed||supplyModel!=model.Pointer||supplyTrack.Parent.Pointer!=parent.Pointer)) ClearSupplySlider();
        if(supplyTrack==null)
        {
            supplyModel=model.Pointer;
            supplyTrack=new QuantityTrack(parent,"보급품 수량",()=>ctrl.Model.Pointer==model.Pointer,
                n=> {if(ctrl.Model.Pointer!=model.Pointer) return;model.AddNum=Math.Clamp(n,0,Math.Max(0,model.MaxNum));ctrl.SetCountPrice();},true);
            supplyActions=new QuantityActions(parent,parent.btnOK,()=>ctrl.Model.Pointer==model.Pointer,
                ()=>{ClearSupplySlider();ctrl.OnAction_B();},()=>SupplyApplyAction(ctrl));
        }
        // Native barSupply is a progress display, not an input slider. Cover the visible bar
        // between the native minus/plus buttons with an interactive quantity track.
        float left=parent.btnCut.x+parent.btnCut.width,right=parent.btnAdd.x;
        supplyTrack.Place(left,parent.btnCut.y,Math.Max(1,right-left),parent.btnCut.height);
        supplyActions?.Layout();
        supplyTrack.Sync(model.AddNum,model.MaxNum);
    }
}

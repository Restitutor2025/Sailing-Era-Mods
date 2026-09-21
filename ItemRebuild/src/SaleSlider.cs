using Il2CppClient.UILogic.UICommonInputNum;
using Il2CppFairyGUI;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    static QuantityTrack? popupTrack;
    static GSlider? saleSlider;
    static QuantityActions? popupActions;
    static GTextField? popupTitle;
    static string? originalPopupTitle;
    static readonly List<EventCallback1> sliderCallbacks=new();
    static void DisposeSaleSlider()
    {
        popupActions?.Dispose();popupActions=null;
        if(popupTitle!=null&&!popupTitle.isDisposed)popupTitle.text=originalPopupTitle??"";
        popupTitle=null;originalPopupTitle=null;
        popupTrack?.Dispose();popupTrack=null;saleSlider=null;sliderCallbacks.Clear();
    }
    static void SyncSaleSlider(long value)=>popupTrack?.Sync(value,popupLimit);
    static void SalePopupRendered(UICommonInputNumView __instance)
    {
        if(popup==null||__instance._model==null||__instance._model.Pointer!=popup.Model.Pointer) return;
        var parent=__instance.UIContent;
        if(parent==null||parent.isDisposed) return;
        if(popupTrack!=null&&(popupTrack.Panel.isDisposed||popupTrack.Parent.Pointer!=parent.Pointer)) DisposeSaleSlider();
        if(popupTrack==null)
        {
            var model=popup.Model;
            popupTrack=new QuantityTrack(parent,salePopup?"판매 수량":"탐험 도구 수량",
                ()=>popup!=null&&popup.Model.Pointer==model.Pointer,
                n=> {if(popup!=null&&popup.Model.Pointer==model.Pointer) popup.Model.CurNum=n;},true,true);
            popupTitle=parent.texInput;originalPopupTitle=popupTitle.text;
            var ctrl=popup;
            popupActions=new QuantityActions(parent,parent.btnReturn,()=>IsPopup(ctrl),()=>PopupCancelAction(ctrl),()=>PopupApplyAction(ctrl));
            saleSlider=popupTrack.Slider;sliderCallbacks.AddRange(popupTrack.Callbacks);
        }
        parent.texInput.text=$"수량 선택 (0~{popupLimit})";
        float left=parent.btnReduce.x+parent.btnReduce.width,right=parent.btnAdd.x;
        float top=parent.inputNum.y+parent.inputNum.height;
        float gap=Math.Max(1,parent.btnReturn.y-top);
        popupTrack.Place(left,top+gap*.1f,Math.Max(1,right-left),gap*.75f);
        popupActions?.Layout();
        SyncSaleSlider(popup.Model.CurNum);
    }
}

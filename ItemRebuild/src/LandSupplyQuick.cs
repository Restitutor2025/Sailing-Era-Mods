using Il2CppClient.UILogic.UILandExplore;
using Il2CppClient.UILogic.UILandExploreSupply;
using Il2CppFairyGUI;
using UnityEngine;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    static readonly Dictionary<IntPtr,(GObject Owner,GTextField Label,EventCallback1 Removed)> carryBadges=new();
    static void ClearCarryBadge(GObject? item)
    {
        if(item==null||!carryBadges.Remove(item.Pointer,out var badge))return;
        if(!badge.Owner.isDisposed)badge.Owner.onRemovedFromStage.Remove(badge.Removed);
        if(!badge.Label.isDisposed)badge.Label.Dispose();
    }
    static void ClearCarryBadges()
    {foreach(var badge in carryBadges.Values.ToArray())ClearCarryBadge(badge.Owner);}
    static void DrawCarryBadge(GComponent item,int count)
    {
        ClearCarryBadge(item);
        var label=new GTextField {text=$"x{count}",touchable=false,singleLine=true,autoSize=AutoSizeType.Shrink,sortingOrder=int.MaxValue};
        label.textFormat=new TextFormat {size=26,color=Color.white,align=AlignType.Right};label.stroke=1;label.strokeColor=Color.black;
        label.SetXY(item.width*.04f,item.height*.03f);label.SetSize(item.width*.92f,item.height*.3f);item.AddChild(label);
        var removed=(EventCallback1)(Action<EventContext>)(_=>ClearCarryBadge(item));
        item.onRemovedFromStage.Add(removed);carryBadges[item.Pointer]=(item,label,removed);
    }
    static void LandSupplyRendered(UILandExploreView __instance)
    {if(__instance.UIContent!=null)BindSupplyQuick(__instance,__instance.UIContent.BtnSupply);}
    static void BindSupplyQuick(UILandExploreView view,GObject target)
    {
        var model=view._model;int generation=landGeneration;
        BindQuick(landQuick,target,()=>
        {
            if(model==null||landModel==null||model.Pointer!=landModel.Pointer||generation!=landGeneration||!view.IsInputActive||popup!=null)return;
            FillSupplyMax(model);
        });
    }
    static void FillSupplyMax(UILandExploreModel model)
    {
        // Mirror OnClickSupply's model setup, then run its native maximum calculation.
        // Do not Show/Close a hidden popup: only its data preparation is needed.
        var ctrl=UILandExploreSupplyCtrl.Instance;var supply=ctrl.Model;
        supply.EntryType=model.EntryType;supply.ItemHeavy=model.ItemWeight;
        supply.MaxHeavy=(float)Math.Round(model.MaxWeight,1);supply.AddNum=model.Supply;
        ctrl.ShowHook();
        supply.AddNum=Math.Max(0,supply.MaxNum);ctrl.SetCountPrice();
        // Native OnClickBtnOK does this assignment and dirty notification before Close.
        model.Supply=supply.AddNum;model.MarkDirty();
    }
}

using Il2CppClient.UILogic.UILandExplore;
using Il2CppClient.UILogic.UILandExploreSupply;
using Il2CppUILandExplore;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    static void LandSupplyQuickTests()
    {
        Reset();SetBag(Item(36000,9,12001));var ctrl=new UILandExploreCtrl();var m=ctrl.Model;
        var g=new Goods{GoodsID=36000,Heavy=.7f};m.CurGoods=g;m.ListDeport.Add(g);m.ListLandBag.Add(null!);LandBuilt(ctrl);
        var view=new UILandExploreView{_model=m};LandSupplyRendered(view);
        var supply=UILandExploreSupplyCtrl.Instance;int closed=supply.Closed,applied=supply.Applied,prepared=supply.Prepared;
        m.MaxWeight=19;m.ItemWeight=9.1f;m.Supply=6;
        view.UIContent.BtnSupply.onTouchBegin.Call(Middle());
        Check(m.Supply==6&&supply.Prepared==prepared+1,"screenshot remaining 0.9 kg cannot fit another 1.5 kg unit");
        Check(supply.Model.ItemHeavy==9.1f&&supply.Model.MaxHeavy==19&&supply.Model.EntryType==m.EntryType,"shortcut passes current native weight and entry context");
        Check(supply.Closed==closed&&supply.Applied==applied&&popup==null,"quick supply never opens or closes a quantity dialog");
        m.ItemWeight=6.3f;view.UIContent.BtnSupply.onTouchBegin.Call(Middle());
        Check(m.Supply==8,"remaining weight refills total supply rather than adding duplicate carried supplies");
        view.UIContent.BtnSupply.onTouchBegin.Call(Middle());Check(m.Supply==8,"repeated supply max is idempotent");
        m.ItemWeight=19;view.UIContent.BtnSupply.onTouchBegin.Call(Middle());Check(m.Supply==0,"no remaining weight gives zero");
        m.ItemWeight=20;view.UIContent.BtnSupply.onTouchBegin.Call(Middle());Check(m.Supply==0,"overweight clamps negative native max to zero");
        var button=new UIButtonItem();CarryLabel(view,0,button);m.ItemWeight=1;m.MaxWeight=10;
        button.onTouchBegin.Call(Middle());Check(m.Supply==6,"left bag supply icon also supports middle max");
        view.IsInputActive=false;m.ItemWeight=0;view.UIContent.BtnSupply.onTouchBegin.Call(Middle());Check(m.Supply==6,"background supply button cannot act under popup");view.IsInputActive=true;
        m.ItemWeight=0;ChooseLandItem(ctrl,g,true);CarryLabel(view,1,button);
        Check(carryBadges[button.Pointer].Label.text=="x3"&&carryBadges[button.Pointer].Label.visible,"carried tools have independent visible x3 label");
        button.texCount.visible=false;Check(carryBadges[button.Pointer].Label.visible,"native supply-only visibility does not hide tool quantity");
        Check(!landQuick.ContainsKey(button.Pointer),"supply slot recycled as tool loses supply shortcut");
        var previous=carryBadges[button.Pointer].Label;CarryLabel(view,1,button);
        Check(previous.isDisposed&&carryBadges.Count==1,"re-render replaces badge without accumulation");
        CarryLabel(view,0,button);Check(carryBadges.Count==0,"tool recycled as supply clears custom badge and retains native count");
        CarryLabel(view,1,button);button.onRemovedFromStage.Call();Check(carryBadges.Count==0,"virtualized badge listener is cleaned up");
        CarryLabel(view,1,button);ResetLand();Check(carryBadges.Count==0&&landQuick.Count==0,"closing expedition removes badges and supply shortcut");
        Reset();
    }
}

using Il2CppClient.UILogic.UIPropStore;
using Il2CppClient.UILogic.UILandExplore;
using Il2CppClient.UILogic.UICommonInputNum;
using Il2CppFairyGUI;
using UnityEngine;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    static EventContext Middle(int button=2)=>new(){inputEvent=new(){button=button}};
    static void QuickQuantityTests()
    {
        foreach(int maximum in new[]{0,1,3,999})
        {
            int value=-1;var host=new GComponent();host.SetSize(600,350);
            var track=new QuantityTrack(host,"quantity",()=>true,n=>value=n,true,true);track.Sync(0,maximum);
            track.MaxButton!.onClick.Call();Check(value==maximum,"Max uses exact live range including zero and large stock");
            track.MinButton!.onClick.Call();Check(value==0,"Min always selects zero");
            track.Sync(0,2);track.MaxButton.onClick.Call();Check(value==2,"Max follows changed stock/range instead of creation-time maximum");
            Check(track.MinButton.x+track.MinButton.width<track.Slider.x&&track.Slider.x+track.Slider.actualWidth<track.MaxButton.x,"endpoint buttons do not overlap slider");track.Dispose();
        }
        Reset();SetBag(Item(40015,3,10001),Item(40016,1,10002));
        var ctrl=new UIPropStoreCtrl();ctrl.View._model=ctrl.Model;StorePreparing(ctrl);
        for(int i=0;i<3;i++)ctrl.Model.ListCurPlay.Add(new(){id=40015,guid=10001,price=100});
        var other=new Slot{id=40016,guid=10002,price=90};ctrl.Model.ListCurPlay.Add(other);
        var row=new GObject{data=ctrl.Model.ListCurPlay[0]};BindStoreQuick(ctrl.View,row);
        int opens=UICommonInputNumCtrl.Instance.Opens;
        row.onTouchBegin.Call(Middle(0));Check(ctrl.Model.SelectNum==0,"left click does not trigger max shortcut");
        row.onTouchBegin.Call(Middle(1));Check(ctrl.Model.SelectNum==0,"right click does not trigger max shortcut");
        var middle=Middle();row.onTouchBegin.Call(middle);
        Check(ctrl.Model.SelectNum==3&&ctrl.Model.Sum==300&&!other.isSelect&&middle.Stopped,"middle selects max of clicked sale group only");
        Check(popup==null&&UICommonInputNumCtrl.Instance.Opens==opens,"middle sale never opens a quantity prompt");
        SaleClicked(ctrl,new(){data=row,inputEvent=new(){button=2}});Check(popup==null&&ctrl.Model.SelectNum==3,"fallback native middle click cannot reopen quantity dialog");
        row.onTouchBegin.Call(Middle());Check(ctrl.Model.SelectNum==3,"repeated middle is max, not toggle off");
        ctrl.View.IsInputActive=false;row.data=other;row.onTouchBegin.Call(Middle());Check(!other.isSelect,"unfocused store cannot accept middle shortcut");ctrl.View.IsInputActive=true;
        BindStoreQuick(ctrl.View,row);row.onTouchBegin.Call(Middle());Check(other.isSelect&&row.onTouchBegin.Callbacks.Count==1,"pooled row updates its item without duplicate listeners");
        var a=new Slot{id=40015,price=100};var b=new Slot{id=40015,price=100};var c=new Slot{id=40016,price=90};
        ctrl.Model.listPropStore.Add(a);ctrl.Model.listPropStore.Add(b);ctrl.Model.listPropStore.Add(c);
        var buyRow=new GObject{data=a};BindStoreQuick(ctrl.View,buyRow);buyRow.onTouchBegin.Call(Middle());
        Check(a.isSelect&&b.isSelect&&!c.isSelect&&popup==null,"buy middle selects all stock of same item without quantity prompt");
        ctrl.Model.curShopType=2;a.isSelect=b.isSelect=false;buyRow.onTouchBegin.Call(Middle());Check(!a.isSelect&&!b.isSelect,"stale purchase row from another store ignored");ctrl.Model.curShopType=1;
        var task=new Slot{id=40016,guid=10002,isTask=true};ctrl.Model.ListCurPlay.Add(task);Check(!QuickStoreMax(ctrl,task)&&!task.isSelect,"task sale shortcut is rejected");
        Check(!QuickStoreMax(ctrl,new Slot{id=40015,guid=10001}),"unbound stale slot rejected");
        Input.MiddleHeld=true;Check(!StoreMiddleLegacy(),"suppress same-kind/all legacy shortcut while middle held");Input.MiddleHeld=false;Input.MiddleUp=true;Check(!StoreMiddleLegacy(),"suppress duplicate legacy shortcut on middle release");Input.MiddleUp=false;Check(StoreMiddleLegacy(),"keyboard and controller shortcuts retained");
        row.onRemovedFromStage.Call();Check(!storeQuick.ContainsKey(row.Pointer)&&row.onTouchBegin.Callbacks.Count==0,"virtualized sale row releases listeners");
        ResetSales();Check(storeQuick.Count==0&&buyRow.onTouchBegin.Callbacks.Count==0,"closing shop releases remaining quick handlers");

        SetBag(Item(36000,7,11001));var land=new UILandExploreCtrl();var g=new Goods{GoodsID=36000,Heavy=2};
        land.Model.ListDeport.Add(g);land.Model.CurGoods=g;land.Model.ListLandBag.Add(null!);LandBuilt(land);
        var view=new UILandExploreView{_model=land.Model};var icon=new GObject();BindLandQuick(view,0,icon);
        opens=UICommonInputNumCtrl.Instance.Opens;icon.onTouchBegin.Call(Middle());
        Check(landRows[36000].Selected==3&&land.Model.ItemWeight==6&&popup==null&&UICommonInputNumCtrl.Instance.Opens==opens,"middle loads three expedition tools directly without popup");
        icon.onTouchBegin.Call(Middle());Check(landRows[36000].Selected==3&&land.Model.ItemWeight==6,"repeat quick max does not duplicate weight or remove tools");
        land.Model.MaxWeight=4;icon.onTouchBegin.Call(Middle());Check(landRows[36000].Selected==2&&land.Model.ItemWeight==4,"quick maximum respects available carry weight");
        bag!.Items[0].Number=1;icon.onTouchBegin.Call(Middle());Check(landRows[36000].Selected==1,"quick maximum rechecks live stock");
        land.Model.MaxWeight=100;bag.Items[0].Number=7;ChooseLand(land);
        var popupView=new UICommonInputNumView{_model=popup!.Model};SalePopupRendered(popupView);
        popupTrack!.MinButton!.onClick.Call();Check(popup.Model.CurNum==0&&landRows[36000].Selected==1,"Min drafts zero without applying");
        popupTrack.MaxButton!.onClick.Call();Check(popup.Model.CurNum==3&&landRows[36000].Selected==1,"Max drafts current maximum without applying");
        var staleButton=popupTrack.MaxButton;PopupCancelAction(popup);staleButton.onClick.Call();Check(popup==null&&landRows[36000].Selected==1,"cancelled endpoint callbacks cannot mutate carried quantity");
        ResetLand();icon.onTouchBegin.Call(Middle());Check(landQuick.Count==0&&popup==null,"land close clears middle handler");
        Reset();
    }
}

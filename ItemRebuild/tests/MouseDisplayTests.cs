using Il2CppClient.Manager;
using Il2CppClient.UILogic.UIPropStore;
using Il2CppClient.UILogic.UICommonInputNum;
using Il2CppClient.UILogic.UILandExplore;
using Il2CppClient.UILogic.UILandExploreSupply;
using Il2CppFairyGUI;
using UnityEngine;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    static EventContext Mouse(GSlider slider,double fraction,int id=1,int button=0)
        =>new(){inputEvent=new(){position=slider.LocalToGlobal(new Vector2((float)(slider.width*fraction),18)),touchId=id,button=button}};
    static void MouseDisplayTests()
    {
        SetBag(Item(10216,4,8001),Item(31000,1,8002),Item(31000,1,8003),Item(9999,1,8004));
        var eq=PlayerDataManager.Instance.Data.PlayerEquipData;eq.Items.Clear();
        foreach(var item in bag!.Items.Where(x=>x.ItemId==31000)) eq.Items[item.Guid]=new(){Equip=Item(item.ItemId,item.Number,item.Guid)};
        var shop=new UIPropStoreCtrl();shop.View._model=shop.Model;
        for(int i=0;i<4;i++) shop.Model.ListCurPlay.Add(new(){id=10216,guid=8001,price=50000});
        shop.Model.ListCurPlay.Add(new(){id=31000,guid=8002,price=5000});
        shop.Model.ListCurPlay.Add(new(){id=31000,guid=8003,price=5000});
        shop.Model.ListCurPlay.Add(new(){id=9999,guid=8004,price=100});
        var units=shop.Model.ListCurPlay.ToArray();
        SaleDisplayBegin(shop.View);SaleDisplayEnd(shop.View);
        Check(saleDisplay.Count==3&&shop.View.ListPlayer.numItems==3,"four consumables and two gear show as two grouped icons plus ordinary item");
        Check(shop.Model.ListCurPlay.SequenceEqual(units)&&bag.Items.Count==4,"display never collapses actual settlement slots or GUIDs");
        Check(saleDisplay.Select(x=>x.Index).SequenceEqual(new[]{0,4,6}),"visual rows map to original representative indices");
        var icon=new Il2CppUIPropStore.UIbtnSlot();icon.SetSize(90,90);icon.texAmount.text="50000";
        int index=0;Check(SaleRenderBegin(shop.View,ref index,icon,out var state)&&index==0,"native renderer gets first real index");
        SaleRenderEnd(icon,state);Check(saleLabels[icon.Pointer].text=="x4"&&icon.texAmount.text=="50000","quantity label preserves unit price");
        Check(saleLabels[icon.Pointer].textFormat.size==27&&saleLabels[icon.Pointer].height==36,"sale quantity is exactly 1.5 times prior font and has room for it");
        shop.Model.ListCurPlay[0].isSelect=shop.Model.ListCurPlay[1].isSelect=true;
        index=0;SaleRenderBegin(shop.View,ref index,icon,out state);SaleRenderEnd(icon,state);
        Check(saleLabels[icon.Pointer].text=="2/4"&&icon.ctrlMulti.selectedIndex==1,"partial selection is visible on grouped icon");
        index=1;SaleRenderBegin(shop.View,ref index,icon,out state);Check(index==4,"second displayed icon is real gear row four");
        SaleRenderEnd(icon,state);Check(saleLabels[icon.Pointer].text=="x2","pooled icon badge rebuilt for new group");
        index=2;SaleRenderBegin(shop.View,ref index,icon,out state);SaleRenderEnd(icon,state);
        Check(index==6&&!saleLabels.ContainsKey(icon.Pointer),"non-grouped pooled icon does not inherit quantity");
        shop.View.ListPlayer.selectedIndex=1;
        Check(!SaleSame(shop)&&popupLimit==2,"same-kind hotkey uses visible group index");
        popup!.Model.CurNum=1;PopupApplyAction(popup);
        Check(shop.Model.ListCurPlay[4].isSelect&&!shop.Model.ListCurPlay[5].isSelect,"gear selection is one real GUID");
        shop.View.ListPlayer.selectedIndex=2;
        Check(!SaleSame(shop)&&shop.Model.ListCurPlay[6].isSelect,"ordinary item after groups uses correct actual row");
        eq.Items[8003].WearRoleId=9;shop.Model.ListCurPlay[5].wearRole=9;
        Check(!SaleAll(shop)&&shop.Model.ListCurPlay.Take(5).All(x=>x.isSelect)&&!shop.Model.ListCurPlay[5].isSelect,"select all uses real unit rows and excludes worn gear");
        Check(!SaleAll(shop)&&shop.Model.ListCurPlay.All(x=>!x.isSelect),"select all toggles off despite excluded worn gear");
        eq.Items[8003].WearRoleId=0;shop.Model.ListCurPlay[5].wearRole=0;
        shop.Model.ListCurPlay[5].price=6000;SaleDisplayBegin(shop.View);SaleDisplayEnd(shop.View);
        Check(saleDisplay.Count==4,"different sale price remains separate");
        shop.Model.ListCurPlay.Clear();SaleDisplayBegin(shop.View);SaleDisplayEnd(shop.View);
        Check(saleDisplay.Count==0&&shop.View.ListPlayer.selectedIndex==-1,"empty inventory clears grouped selection");
        ResetSales();Check(saleLabels.Count==0&&saleDisplay.Count==0,"closing store releases display references");

        foreach(int max in new[]{0,1,3,12,127,999})
        {
            int current=-1;bool active=true;
            var parent=new GComponent();parent.SetSize(400,160);parent.SetXY(72,91);parent.scaleX=.63f;parent.scaleY=.63f;
            var track=new QuantityTrack(parent,"test",()=>active,n=>current=n);track.Sync(0,max);
            for(int i=0;i<=max;i++)
            {
                double fraction=max==0?0:i/(double)max;
                var e=Mouse(track.Slider,fraction);track.Slider.onTouchBegin.Call(e);track.Slider.onTouchEnd.Call(Mouse(track.Slider,fraction));
                Check(current==i&&e.Captured&&e.Stopped,"mouse selects nearest exact quantity under scaled parent");
            }
            if(max>1)
            {
                track.Slider.onTouchBegin.Call(Mouse(track.Slider,1.49/max));Check(current==1,"below rounding midpoint");
                track.Slider.onTouchMove.Call(Mouse(track.Slider,1.51/max));Check(current==2,"drag above rounding midpoint");
                track.Slider.onTouchMove.Call(Mouse(track.Slider,1,2));Check(current==2,"other pointer cannot move captured slider");
                track.Slider.onTouchMove.Call(Mouse(track.Slider,-.1));Check(current==0,"drag beyond left clamps zero");
                track.Slider.onTouchEnd.Call(Mouse(track.Slider,1.1));Check(current==max,"release beyond right clamps max");
                track.Slider.onTouchMove.Call(Mouse(track.Slider,0));Check(current==max,"released pointer no longer changes value");
                track.Slider.onTouchBegin.Call(Mouse(track.Slider,0,1,1));Check(current==max,"right click does not select quantity");
            }
            active=false;int before=current;track.Slider.onTouchBegin.Call(Mouse(track.Slider,0));Check(current==before,"inactive scope ignores mouse");
            parent.onRemovedFromStage.Call();Check(track.Panel.isDisposed&&track.Callbacks.Count==0,"screen removal disposes slider and delegates");
        }
        Check(NearestQuantity(1.5,3,3)==2&&NearestQuantity(2.5,3,3)==3,"exact half ties round upward consistently");

        SetBag(Item(36000,3,8101));var land=new UILandExploreCtrl();
        var goods=new Goods{GoodsID=36000,Heavy=2};land.Model.ListDeport.Add(goods);land.Model.ListLandBag.Add(null!);
        land.Model.CurGoods=goods;land.Model.MaxWeight=5;LandBuilt(land);ChooseLand(land);
        var view=new UICommonInputNumView{_model=popup!.Model};view.UIContent.SetSize(600,350);SalePopupRendered(view);
        Check(!salePopup&&saleSlider!=null&&popupLimit==2,"expedition slider respects remaining weight and three-item maximum");
        Check(view.UIContent.texInput.text=="수량 선택 (0~2)","quantity title replaces money title");
        var panel=popupTrack!.Panel;
        Check(panel.x>=0&&panel.y>=0&&panel.x+panel.actualWidth<=view.UIContent.width&&panel.y+panel.actualHeight<view.UIContent.btnReturn.y,"quantity slider fits INSIDE popup above actions, not outside clipped UIContent");
        Check(popupActions!.Cancel.x+popupActions.Cancel.width<popupActions.Apply.x,"cancel and apply have a positive gap");
        Check(Math.Abs((popupActions.Apply.x-popupActions.Cancel.width)/popupActions.Panel.width-.1f)<.0001f,"button gap uses ten percent of pair width");
        Check(Math.Abs(popupActions.Panel.x+popupActions.Panel.width/2-(view.UIContent.btnReturn.x+view.UIContent.btnReturn.width/2))<.001f,"action pair centered on original control");
        saleSlider!.onTouchBegin.Call(Mouse(saleSlider,.76));saleSlider.onTouchEnd.Call(Mouse(saleSlider,.76));
        Check(popup.Model.CurNum==2&&landRows[36000].Selected==0,"expedition mouse edits pending quantity");
        popupActions.Apply.onClick.Call();Check(landRows[36000].Selected==2&&land.Model.ItemWeight==4,"mouse apply commits rounded quantity and weight");
        Check(view.UIContent.texInput.text=="금액 입력"&&view.UIContent.btnReturn.visible,"closing owned popup restores native controls for unrelated money dialogs");
        ChooseLand(land);popup!.Model.CurNum=0;PopupCancelAction(popup);
        Check(popup==null&&landRows[36000].Selected==2&&land.Model.ItemWeight==4,"ESC cancels pending edits instead of decrement or commit");
        ChooseLand(land);popup!.Model.CurNum=0;SalePopupRendered(view);var oldActions=popupActions!;
        oldActions.Cancel.onClick.Call();Check(popup==null&&landRows[36000].Selected==2,"mouse cancel leaves prior carried quantity intact");
        oldActions.Apply.onClick.Call();Check(landRows[36000].Selected==2,"disposed apply cannot commit after cancel");
        ChooseLand(land);popup!.Model.CurNum=0;popup.OnClickBtnReturn();
        Check(popup==null&&landRows[36000].Selected==2,"native return is cancellation too");
        Check(PopupApplyAction(UICommonInputNumCtrl.Instance)&&PopupCancelAction(UICommonInputNumCtrl.Instance),"foreign money popup actions pass through untouched");
        ChooseLand(land);popupLimit=3;popup!.Model.CurNum=0;PopupReduce(popup);Check(popup.Model.CurNum==3,"requested 0 minus wraps to 3");PopupAdd(popup);Check(popup.Model.CurNum==0,"requested 3 plus wraps to 0");
        popupLimit=0;PopupAdd(popup);PopupReduce(popup);Check(popup.Model.CurNum==0,"zero capacity wraps safely to zero");CancelPopup();
        var supply=UILandExploreSupplyCtrl.Instance;supply.Model=new(){MaxNum=37,AddNum=1,Price=5};
        var supplyView=new UILandExploreSupplyView{_model=supply.Model};supplyView.UIContent.SetSize(650,500);
        SupplyRendered(supplyView);var slider=supplyTrack!.Slider;
        Check(supplyTrack.Panel.x==supplyView.UIContent.btnCut.x+supplyView.UIContent.btnCut.width&&supplyTrack.Panel.y==supplyView.UIContent.btnCut.y&&supplyTrack.Panel.x+supplyTrack.Panel.actualWidth==supplyView.UIContent.btnAdd.x,"supply hit surface covers the original visible bar between minus and plus");
        slider.onTouchBegin.Call(Mouse(slider,.51));slider.onTouchEnd.Call(Mouse(slider,.51));
        Check(supply.Model.AddNum==19&&supply.Model.CountPrice==95&&supply.Model.Dirty>0,"supply mouse rounds nearest and invokes original price refresh");
        supply.Model.MaxNum=5;SupplyRendered(supplyView);slider.onTouchBegin.Call(Mouse(slider,1));
        Check(supply.Model.AddNum==5&&supply.Model.BanBtnAdd,"supply mouse follows current native maximum");
        var oldSupplyActions=supplyActions!;oldSupplyActions.Cancel.onClick.Call();Check(supply.Closed==1&&supply.Applied==0&&supplyTrack==null,"supply cancel delegates native back without application and clears input immediately");
        oldSupplyActions.Apply.onClick.Call();Check(supply.Applied==0,"apply cannot race a closing supply dialog");
        SupplyRendered(supplyView);Check(!SupplyApplyAction(supply)&&supply.Applied==1&&supplyTrack==null,"supply Space invokes original OK despite hidden original button and clears scope");
        SupplyRendered(supplyView);var disposedTrack=supplyTrack!;SupplyCancelAction(supply);Check(supplyTrack==null&&disposedTrack.Panel.isDisposed,"supply keyboard cancel cleans up before native close");
        SupplyRendered(supplyView);supplyView.UIContent.onRemovedFromStage.Call();Check(supplyTrack!.Panel.isDisposed,"closing supply screen removes mouse control");
        Check(SupplyApplyAction(supply),"supply apply override expires on stage removal");
        Reset();
    }
}

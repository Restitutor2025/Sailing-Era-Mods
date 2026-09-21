using Il2CppClient.UILogic.UICommonInputNum;
using Il2CppClient.UILogic.UIPropStore;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
 // 0.1.22: tooltip slot, single-unit toggle, list position after popup.
 static void SaleTooltipTests()
 {
  var a=Item(71001,3,70001);var b=Item(71002,1,70002);
  SetBag(a,b);Compact(bag!);
  var shop=new UIPropStoreCtrl();var view=shop.View;view._model=shop.Model;quickStore=shop;
  for(int n=0;n<3;n++)shop.Model.ListCurPlay.Add(new Slot{id=71001,guid=70001,price=10});
  shop.Model.ListCurPlay.Add(new Slot{id=71002,guid=70002,price=20});
  var raw=shop.Model.ListCurPlay;
  SaleDisplayBegin(view);SaleDisplayEnd(view);
  Check(saleDisplay.Count==2&&view.ListPlayer.numItems==2,"three units of A and one B show as two rows");
  view.ListPlayer.selectedIndex=1;shop.SetCurGoods();
  Check(shop.Model.CurGoods==raw[1],"native double reproduces the raw-index choice (A)");
  CurGoodsSelected(shop);
  Check(shop.Model.CurGoods==raw[3],"visible row 1 tooltip is B");
  view.ListPlayer.selectedIndex=3;shop.SetCurGoods();CurGoodsSelected(shop);
  Check(view.ListPlayer.selectedIndex==1&&shop.Model.CurGoods==raw[3],"navigation past the last visible row clamps to it");
  view.ListPlayer.selectedIndex=0;shop.SetCurGoods();int dirty=shop.Model.Dirty;CurGoodsSelected(shop);
  Check(shop.Model.CurGoods==raw[0]&&shop.Model.Dirty==dirty,"already-correct choice causes no extra refresh");
  shop.Model.CurGoods=new Slot{id=1};CurGoodsSelected(shop);
  Check(shop.Model.CurGoods!.id==1,"shop-side goods are never replaced");
  int opens=UICommonInputNumCtrl.Instance.Opens;
  Check(OpenSale(shop,raw[3])&&popup==null&&raw[3].isSelect&&shop.Model.SelectNum==1,"single unit toggles on without popup");
  Check(OpenSale(shop,raw[3])&&!raw[3].isSelect&&shop.Model.SelectNum==0,"single unit toggles off");
  Check(UICommonInputNumCtrl.Instance.Opens==opens,"no quantity popup for a single unit");
  view.ListPlayer.selectedIndex=0;view.ListPlayer.scrollPane!.posY=120;view.ListPlayer.scrollPane.posX=4;
  Check(OpenSale(shop,raw[0])&&popup!=null&&popupLimit==3,"three units still use the popup");
  view.ListPlayer.selectedIndex=1;view.ListPlayer.scrollPane.posY=0;view.ListPlayer.scrollPane.posX=0; // focus return resets
  TestPopupSet(popup!.Model,2);PopupApplyAction(popup!);
  Check(view.ListPlayer.selectedIndex==0&&view.ListPlayer.scrollPane.posY==120&&view.ListPlayer.scrollPane.posX==4,"position restored when the popup closes");
  view.ListPlayer.selectedIndex=1;view.ListPlayer.scrollPane.posY=0; // reset again after close
  SaleDisplayBegin(view);SaleDisplayEnd(view);
  Check(view.ListPlayer.selectedIndex==0&&view.ListPlayer.scrollPane.posY==120,"restored once more on the next refresh");
  view.ListPlayer.selectedIndex=1;view.ListPlayer.scrollPane.posY=0;SaleDisplayBegin(view);SaleDisplayEnd(view);
  Check(view.ListPlayer.selectedIndex==1&&view.ListPlayer.scrollPane.posY==0,"restore is one-shot, later user movement kept");
  Check(raw.Count(x=>x.isSelect)==2,"popup selection applied");
  ResetSales();
 }
}

using Il2CppClient.UILogic.UIMarket;
namespace Restitutor.Contribution;
internal static class LiveViews {
    private static UIMarketCtrl? market;
    internal static void Reset()=>market=null;
    internal static void MarketBuilt(UIMarketCtrl value)=>market=value;
    internal static void RefreshMarket(int port) {
        // A cached controller can survive disposal of its view/model. UI refresh
        // is best effort and must never invalidate successfully granted licences.
        try {
            var current=market;
            if(current==null)return;
            var view=current.View; var model=current.Model;
            if(view==null || view._state==null || view.UIContent==null || view.UIContent.isDisposed || model==null) {
                market=null; return;
            }
            if(current.IsClose() || model.PortId!=port)return;
            current.SetMarketGoods();model.MarkDirty();
        } catch(Exception ex) {
            market=null;
            EntryPoint.Error("Market refresh city="+port,ex);
        }
    }
}


using Il2CppClient.Const;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIKnowledge;
using Il2CppClient.Utils;
using Il2CppGyyx.Template;

namespace Restitutor.Contribution;
internal static class CargoDiscovery
{
    private static readonly Dictionary<int,double> retryAt=new();
    internal static void Reset()=>retryAt.Clear();
    internal static void NaturalResourceAdded(PlayerData player,WorldPortHoldDB db,int port,int resource,bool succeeded)
    {
        if(!succeeded || player.WorldPort==null || player.WorldPort.Pointer!=db.Pointer)return;
        var data=db.GetPortData(port);
        if(data?._dictNaturalResources==null || !data._dictNaturalResources.ContainsKey(resource))return;
        EntryPoint.Journal.QueueGoodsDiscovery(port);
    }

    // Called only after every queued licence has been applied. No market UI is created.
    internal static bool TryApply(PlayerData player,PendingPort pending)
    {
        if(!pending.RevealGoods)return true;
        if(!player.PlayerPort.IsStayInPort || player.PlayerPort.StayInPortId!=pending.Port)return false;
        if(retryAt.TryGetValue(pending.Port,out var next) && EntryPoint.Now<next)return false;
        try {
            if(!TemplateUtils.PortContainsFacility(pending.Port,(int)EPortFacilityType.Market))return true;
            var knowledge=player.KnowledgeData;
            var market=player.MarketData?.FindMarketMessage(pending.Port);
            var manager=KnowledgeManager.Instance;
            if(knowledge==null || market?.ListGoodsMarket==null || market.DicGoods==null ||
                manager?._knowledgeData==null || manager._knowledgeData.Pointer!=knowledge.Pointer)return false;
            var facility=TemplateUtils.GetPortFacilityLineByType(pending.Port,(int)EPortFacilityType.Market);
            if(facility==null || TemplateManager.GetMarket(facility.tid)==null)return false;
            foreach(int id in market.ListGoodsMarket) {
                if(market.FindGoodsData(id)==null)return false;
                if(!knowledge.GetCargoPortIsUnlock(id,pending.Port) && !pending.UnknownGoods.ContainsKey(id)) {
                    var item=TemplateManager.GetItem(id);
                    if(item==null)return false;
                    pending.UnknownGoods[id]=TextLibUtils.Text(item.name,item.name);
                }
            }
            // Native implementation checks OutPutFinal and market conditions, and records cargo+port.
            manager.UnlockCargoDataByMarketPortId(pending.Port);
            foreach(var item in pending.UnknownGoods) {
                if(!knowledge.GetCargoPortIsUnlock(item.Key,pending.Port))continue;
                string line="교역품 확인 · "+item.Value;
                if(!pending.Lines.Contains(line))pending.Lines.Add(line);
            }
            retryAt.Remove(pending.Port);
            return true;
        } catch(Exception ex) {
            // Keep both partial discovery evidence and the combined notice across retries/saves.
            retryAt[pending.Port]=EntryPoint.Now+5;
            EntryPoint.Error("Cargo discovery city="+pending.Port,ex);
            return false;
        }
    }
}

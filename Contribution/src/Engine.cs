using Il2CppClient.Const;
using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.Utils;
using Il2CppClient.WorldLogic.GameEffect;
using Il2CppGyyx.Template;

namespace Restitutor.Contribution;
internal static class Engine
{
    private static readonly HashSet<int> failed = new();
    internal static int? GrantPort;
    internal static void Reset() { failed.Clear(); GrantPort = null; }
    internal static List<Benefit> Benefits(int port)
    {
        var result = new List<Benefit>();
        if (!TemplateUtils.PortContainsFacility(port, (int)EPortFacilityType.Govhouse)) return result;
        var facility = TemplateUtils.GetPortFacilityLineByType(port, (int)EPortFacilityType.Govhouse);
        var house = TemplateManager.GetGovHouseTpl(facility.tid);
        foreach (int id in house.licences)
        {
            if (id == 0) continue;
            var b = TemplateManager.GetGovHouseLicence(id) ?? throw new InvalidOperationException("Missing licence " + id);
            if (b.type < 1 || b.type > 6) throw new InvalidOperationException("Unknown licence kind " + b.type);
            int need = EffectCalculator.GetPropertyWithCommanderEffectPercentageChange(ECommanderPropertyType.PrivilegeCostReduce, b.needInfluence);
            string name = TextLibUtils.Text(b.name, b.name);
            if (b.type == 4) { var item = TemplateManager.GetItem(b.param); name = TextLibUtils.Text(item.name, item.name) + " 교역"; }
            result.Add(new Benefit(id, b.type, need, name, b.type == 4 ? b.param : 0));
        }
        if (result.Select(b => b.Id).Distinct().Count() != result.Count) throw new InvalidOperationException("Duplicate licence template");
        return result;
    }
    internal static HashSet<int> Opened(WorldPortData port)
    {
        var result = new HashSet<int>(); foreach (int id in port.OpenLicenceList) result.Add(id); return result;
    }
    internal static string City(int port)
    {
        var t = TemplateManager.GetPort(port); return TextLibUtils.Text(t.name, t.name);
    }
    internal static void Observe(WorldPortHoldDB db,int port,int before) {
        var data=db.GetPortData(port);
        if(data==null)return;
        EntryPoint.Journal.ObserveHundred(port,before,data.Influence);
        if(data.Influence<=before)return;
        var benefits=Benefits(port); var opened=Opened(data);
        EntryPoint.Journal.Changed(port,before,data.Influence,benefits,opened);
        failed.Remove(port); // Retry a real grant failure only on the next increase.
        EntryPoint.Log.Msg($"Contribution city={port}: {before}->{data.Influence}; " +
            string.Join(" / ",benefits.Select(b=>$"id={b.Id},kind={b.Kind},need={b.Required},open={opened.Contains(b.Id)}")));
    }
    internal static void Apply(PlayerData player,PendingPort pending) {
        if(failed.Contains(pending.Port))return;
        var notice=new Notice {Port=pending.Port,City=City(pending.Port)};
        try {
            var grants=Rules.GrantsOnly(pending);
            if(grants==null){EntryPoint.Journal.Ports.Remove(pending.Port);return;}
            var port=player.WorldPort.GetPortData(pending.Port) ?? throw new InvalidOperationException("Missing port");
            var benefits=Benefits(port.PortID);
            foreach(var entry in grants.Benefits.Values) {
                if(port.OpenLicenceList.Contains(entry.Id))continue;
                var b=benefits.Single(x=>x.Id==entry.Id);
                bool ok;
                try {
                    GrantPort=port.PortID;
                    ok=player.WorldPort.AddGovHouseLicenceData(port.PortID,b.Id,0,player.WorldTimeDataDB.TotalGameDay,(EGovHouseLicence)b.Kind);
                } finally {GrantPort=null;}
                if(!ok || !port.OpenLicenceList.Contains(b.Id))throw new InvalidOperationException("Licence grant failed: "+b.Id);
                pending.Lines.Add("해금 · "+b.Name);
            }
        } catch(Exception ex) {
            failed.Add(pending.Port);
            notice.Lines.AddRange(pending.Lines);pending.Lines.Clear();
            notice.Lines.Add("해금 처리 오류 · 로그를 확인해 주세요.");
            EntryPoint.Error("Grant city="+pending.Port,ex);
            EntryPoint.Journal.AddNotice(notice);
            return;
        }
        if(!CargoDiscovery.TryApply(player,pending))return;
        EntryPoint.Journal.Ports.Remove(pending.Port);
        notice.Lines.AddRange(pending.Lines);
        LiveViews.RefreshMarket(pending.Port);
        if(notice.Lines.Count>0)EntryPoint.Log.Msg($"Granted city={pending.Port}: "+string.Join(" / ",notice.Lines));
        EntryPoint.Journal.AddNotice(notice);
    }
    internal static string Status(WorldPortData port)
    {
        var benefits = Benefits(port.PortID); var opened = Opened(port);
        bool Has(int kind) => benefits.Any(b => b.Kind == kind && opened.Contains(b.Id));
        string Locked(int kind) { var b = benefits.Where(b => b.Kind == kind).ToArray(); return b.Length == 0 ? "없음" : $"잠김 ({b.Min(x => x.Required)})"; }
        var goods = benefits.Where(b => b.Kind == 4 && opened.Contains(b.Id)).Select(b => b.Name.Replace(" 교역", "")).ToArray();
        var lines = new List<string> { "도시 혜택", "교역 허가  " + (goods.Length == 0 ? Locked(4) : string.Join(", ", goods)) };
        if (TemplateUtils.PortContainsFacility(port.PortID, (int)EPortFacilityType.Station)) lines.Add("역참  " + (Has(3) ? "이용 가능" : Locked(3)));
        lines.Add("상회 설립  " + (Has(2) ? "가능" : Locked(2)));
        lines.Add("투자 가능  " + (!Has(2) ? "잠김" : Has(6) ? "상" : Has(5) ? "중" : "하"));
        lines.Add("세금  " + (Has(1) ? "면제" : "일반"));
        return string.Join("\n", lines);
    }
}





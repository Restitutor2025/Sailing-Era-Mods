namespace Restitutor.Contribution;
internal sealed record Benefit(int Id, int Kind, int Required, string Name, int Item = 0);
internal static class Rules
{
    internal static bool Crosses(int before,int after,int required) => after>before && before<required && after>=required;
    internal static PendingPort? GrantsOnly(PendingPort source) {
        var grants=source.Benefits.Where(x=>x.Key==x.Value.Id && x.Value.Target && !x.Value.Original)
            .ToDictionary(x=>x.Key,x=>new PendingBenefit {Id=x.Key,Original=false,Target=true});
        return grants.Count==0 && !source.RevealGoods && source.Lines.Count==0 ? null : new PendingPort {
            Port=source.Port,Contribution=source.Contribution,Benefits=grants,RevealGoods=source.RevealGoods,
            Lines=new(source.Lines), UnknownGoods=new(source.UnknownGoods)};
    }
}
internal sealed class PendingBenefit { public int Id {get;set;} public bool Original {get;set;} public bool Target {get;set;} }
internal sealed class PendingPort {
    public int Port {get;set;} public int Contribution {get;set;}
    public Dictionary<int,PendingBenefit> Benefits {get;set;}=new();
    public bool RevealGoods {get;set;}
    public List<string> Lines {get;set;}=new();
    public Dictionary<int,string> UnknownGoods {get;set;}=new();
}
internal sealed class Notice { public int Port {get;set;} public string City {get;set;}=""; public List<string> Lines {get;set;}=new(); }
internal sealed class Journal
{
    internal readonly Dictionary<int,PendingPort> Ports=new();
    internal readonly List<Notice> Notices=new();
    internal readonly HashSet<int> ReachedHundred=new();
    internal int Revision;
    internal void FirstVisit(int port)=>QueueGoodsDiscovery(port);
    internal void QueueGoodsDiscovery(int port) {
        if(!Ports.TryGetValue(port,out var p)) Ports[port]=p=new PendingPort {Port=port};
        p.RevealGoods=true;
    }
    internal void ObserveHundred(int port,int before,int after) {
        // Also remember a pre-existing >=100 value before a decrease.
        if(before>=100)ReachedHundred.Add(port);
        if(Rules.Crosses(before,after,100) && ReachedHundred.Add(port))FirstVisit(port);
    }
    internal void Changed(int port,int before,int after,IEnumerable<Benefit> benefits,ISet<int> opened) {
        if (after<=before) return;
        Ports.TryGetValue(port,out var pending);
        foreach(var b in benefits) {
            // A real increase also repairs grants missed during an earlier UI failure.
            if (after < b.Required || opened.Contains(b.Id)) continue;
            pending ??= new PendingPort {Port=port};
            pending.Contribution=after;
            pending.Benefits[b.Id]=new PendingBenefit {Id=b.Id,Original=false,Target=true};
        }
        if (pending != null && pending.Benefits.Count>0) Ports[port]=pending;
    }
    internal void AddNotice(Notice notice) {
        if(notice.Lines.Count==0)return;
        var previous=Notices.FirstOrDefault(n=>n.Port==notice.Port);
        if(previous==null)Notices.Add(notice);else previous.Lines.AddRange(notice.Lines);
        Revision++;
    }
    internal void Acknowledge(){Notices.Clear();Revision++;}
    internal void Clear(){Ports.Clear();Notices.Clear();ReachedHundred.Clear();Revision++;}
}

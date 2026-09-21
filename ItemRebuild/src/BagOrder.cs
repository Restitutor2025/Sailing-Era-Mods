using Il2CppClient.PlayerStore;

namespace Restitutor.ItemRebuild;

public sealed partial class EntryPoint
{
    // Run before the native model rebuild, so GlobalIndex and all category rows agree.
    static void GroupTools()
    {
        if(bag==null||!Own(bag)) return;
        var items=bag.Items;
        var tools=new List<PlayerItemData>();
        int first=-1;
        for(int i=0;i<items.Count;i++)
            if(Eligible(items[i])&&Rules.Target(items[i].ItemId)) {if(first<0) first=i; tools.Add(items[i]);}
        if(tools.Count<2) return;
        // Stable ordering preserves native sorting within tools and among all other items.
        var ordered=new List<PlayerItemData>(items.Count);
        for(int i=0;i<items.Count;i++)
        {
            if(i==first) ordered.AddRange(tools);
            if(!Eligible(items[i])||!Rules.Target(items[i].ItemId)) ordered.Add(items[i]);
        }
        bool changed=false;
        for(int i=0;i<items.Count;i++)
            if(items[i].Pointer!=ordered[i].Pointer) {changed=true;break;}
        if(!changed) return;
        ClearBagBadges();
        for(int i=0;i<ordered.Count;i++) items[i]=ordered[i];
        owner?.MarkDBDirty();
    }
}

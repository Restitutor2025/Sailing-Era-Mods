using Il2CppClient.PlayerStore;

namespace Restitutor.ItemRebuild;

public sealed partial class EntryPoint
{
    // Only the last unit delegates deletion to native code. Notify deletion after success.
    static bool RemoveIndexBegin(ItemBagData __instance,int __0,ref bool __result,out PlayerItemData? __state)
    {
        __state=null;
        if(Own(__instance)&&__0>=0&&__0<__instance.Items.Count)
        {
            var item=__instance.Items[__0];
            if(Eligible(item)&&item.Number<=1) __state=item;
        }
        return RemoveIndex(__instance,__0,ref __result);
    }
    static bool RemoveGuidBegin(ItemBagData __instance,long __0,ref bool __result,out PlayerItemData? __state)
    {
        __state=null;
        if(!Own(__instance)) return true;
        for(int i=0;i<__instance.Items.Count;i++)
            if(__instance.Items[i].Guid==__0) return RemoveIndexBegin(__instance,i,ref __result,out __state);
        return true;
    }
    static void RemoveOneEnd(bool __result,PlayerItemData? __state)
    {
        if(__result&&__state!=null) Changed(__state,true);
    }
    static void RemoveManyBegin(ItemBagData __instance,ref Il2CppSystem.Collections.Generic.List<int> __0,out List<PlayerItemData>? __state)
    {
        __state=null;
        if(!Own(__instance)) return;
        foreach(int index in __0)
            if(index>=0&&index<__instance.Items.Count)
            {
                var item=__instance.Items[index];
                if(Eligible(item)&&item.Number<=1) (__state??=new()).Add(item);
            }
        RemoveMany(__instance,ref __0);
    }
    static void RemoveManyEnd(bool __result,List<PlayerItemData>? __state)
    {
        if(__result&&__state!=null) foreach(var item in __state) Changed(item,true);
    }
}

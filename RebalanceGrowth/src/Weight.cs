using Il2CppClient.UILogic.UILandExplore;

namespace Restitutor.RebalanceGrowth;

// 0.2.0: land-exploration carry weight per navigator/seaman capped at 30 (user: final value incl. bonuses).
// PlayerRoleInfo.Weight is built in UILandExploreCtrl.InitPlayerModelList / InitSeamenModelList
// (round(physical/3,1) + property 22 + commander effects); the team maximum (model +0xE4) is the sum of the selected
// entries' Weight (RefreshLandRoleList) plus team-level effect sources added in ShowHook (not capped here).
public sealed partial class EntryPoint {
    private static void AfterRoleList(UILandExploreCtrl __instance) {
        if(!enabled) return;
        try {
            var model=__instance.Model;
            if(model==null) return;
            Cap(model.ListPlayerRole); Cap(model.ListSeamenRole);
        } catch(Exception ex) { Fail("carry weight",ex); }
    }
    private static void Cap(Il2CppSystem.Collections.Generic.List<PlayerRoleInfo>? list) {
        if(list==null) return;
        for(int i=0;i<list.Count;i++) {
            var e=list[i];
            if(e!=null && e.Weight>Rules.WeightMax) e.Weight=Rules.CapWeight(e.Weight);
        }
    }
}

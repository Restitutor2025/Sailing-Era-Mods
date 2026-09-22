using Il2CppClient.Manager;
using Il2CppClient.UILogic.UIHeroLevelUp;

namespace Restitutor.RebalanceGrowth;

// 10-level button: the original grants at most one skill point per click (IsSpecialLevelUp is a bool),
// so with an interval under 10 the remaining points were lost (user report: "5 per point" gave 1 per 10).
// Same logic as Cheats Skill 1.0.5, but the interval is the effective LevelGetSkill value, which already
// includes Cheats Skill's override when the cheats are on. Cheats Skill skips its own grant while this mod is loaded.
public sealed partial class EntryPoint {
    private sealed record Multi(IntPtr Ctrl,int Role,int From,int Count,int Interval,int Points);
    private static Multi? before,pending;
    private static bool rewardWasSet;

    private static void ClearPending(UIHeroLevelUpCtrl __instance) { before=null; pending=null; GrowthBefore(__instance); }

    private static void BeforeMulti(UIHeroLevelUpCtrl __instance) {
        before=pending=null;
        ApplyForClick(__instance);   // 0.3.0 slider amount (Slider.cs)
        GrowthBefore(__instance);
        if(!enabled) return;
        try {
            var model=__instance.Model; var role=model?.CurRole;
            if(model==null || role==null || model.BanTouch) return;
            int interval=UIHeroLevelUpModel.LevelGetSkill,from=role.Level,count=model.MaxContinueLevel;
            before=new(__instance.Pointer,role.RoleId,from,count,interval,Rules.PointsInRange(from,count,interval));
        } catch(Exception ex) { Fail("multi before",ex); }
    }

    private static void AfterMulti(UIHeroLevelUpCtrl __instance) {
        GrowthAfter(__instance,true);
        var b=before; before=null;
        if(b==null || !enabled) return;
        try {
            var model=__instance.Model;
            // Proceeded only if the original locked input and entered continue mode.
            if(model==null || __instance.Pointer!=b.Ctrl || !model.BanTouch || !model.IsClickContinueBtn || model.CurRole?.RoleId!=b.Role) return;
            if(model.IsSpecialLevelUp!=(b.Points>0)) { log.Warning($"Multi-level check mismatch: from={b.From} count={b.Count} interval={b.Interval} points={b.Points} special={model.IsSpecialLevelUp}; no extra"); return; }
            if(b.Points>1) pending=b;
        } catch(Exception ex) { Fail("multi after",ex); }
    }

    private static void RewardBefore(UIHeroLevelUpCtrl __instance) {
        try { rewardWasSet=__instance.Model?.IsRewardSkill!=false; } catch { rewardWasSet=true; }
    }

    private static void GrantExtra(UIHeroLevelUpCtrl __instance) {
        var p=pending; pending=null;
        if(p==null || !enabled || __instance.Pointer!=p.Ctrl) return;
        try {
            var model=__instance.Model;
            // The original point was awarded by this call only if IsRewardSkill went false -> true.
            if(model==null || rewardWasSet || !model.IsRewardSkill || model.CurRole?.RoleId!=p.Role) { log.Warning("Original reward not observed; no extra point"); return; }
            var db=PlayerDataManager.Instance?.Data?.PlayerRole;
            var data=db?.FindHoldRole(p.Role);
            if(db==null || data==null) { log.Warning("Role data not found; no extra point"); return; }
            int prev=data.SkillPoints,extra=p.Points-1;
            if(!db.UpdateRoleSkillPoint(p.Role,extra) || data.SkillPoints!=prev+extra) throw new InvalidOperationException($"UpdateRoleSkillPoint mismatch before={prev} extra={extra} after={data.SkillPoints}");
            log.Msg($"Multi-level role={p.Role} levels {p.From}..{p.From+p.Count-1} interval={p.Interval}: original +1, extra +{extra}; points {prev}->{data.SkillPoints}");
        } catch(Exception ex) { Fail("multi grant",ex); }
    }
}

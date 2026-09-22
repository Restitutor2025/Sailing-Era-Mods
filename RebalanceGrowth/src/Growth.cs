using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIHeroLevelUp;
using Il2CppGyyx.Template;

namespace Restitutor.RebalanceGrowth;

// 0.3.0 cumulative growth (user 2026-09-22). The level-up roll stays as the original runs it
// (1-level: GetResult x5 in OnClickBtnLevelUp 0xB7F540; n levels: inline in OnClickBtnLevelUpFive 0xB7EDC0);
// a postfix then overwrites the five result fields (model +0x80 physical, +0x84 perceive, +0x78 craft,
// +0x88 knowledge, +0x7C charm) with the cumulative result, so the level-up animation and AniResult
// (0xB7FAA0 -> SettlementAttributeValue x5) show and apply the same numbers. The new progress is written to
// PlayerRoleData.<Stat>_Exp only when AniResult applies the stats (the click can still be abandoned before).
// Navigators and hired seamen share this window and path (InitRoleData / InitSeamenData), both covered.
// Luck is not part of the roll (it grows by the fixed upValue per level, see Luck.cs).
public sealed partial class EntryPoint {
    private sealed record GrowthPending(IntPtr Ctrl,int Role,int FromLevel,int Levels,int[] Progress);
    private static GrowthPending? growthPending;
    private static bool growthBanBefore;
    // Order used everywhere below: physical, perceive, craft, knowledge, charm.
    private static readonly string[] StatNames={"physical","perceive","craft","knowledge","charm"};

    private static void GrowthBefore(UIHeroLevelUpCtrl ctrl) {
        growthPending=null;
        try { growthBanBefore=ctrl.Model?.BanTouch??true; } catch { growthBanBefore=true; }
    }

    // single: OnClickBtnLevelUp postfix (1 level). multi: OnClickBtnLevelUpFive postfix (MaxContinueLevel levels).
    private static void GrowthAfter(UIHeroLevelUpCtrl ctrl,bool multi) {
        if(!enabled) return;
        try {
            var model=ctrl.Model; var cur=model?.CurRole;
            // Proceeded only if the original locked input now (it was unlocked before) and took the expected branch.
            if(model==null || cur==null || growthBanBefore || !model.BanTouch || model.IsClickContinueBtn!=multi) return;
            int levels=multi ? model.MaxContinueLevel : 1;
            var role=PlayerDataManager.Instance?.Data?.PlayerRole?.FindHoldRole(cur.RoleId);
            if(role==null || levels<=0) { log.Warning($"Growth: role {cur.RoleId} data missing; original roll kept."); return; }
            var g=ReadGrowth(cur,role,out string? problem);
            if(g==null) { log.Warning($"Growth: role {cur.RoleId} {problem}; original roll kept."); return; }
            int[] gain=new int[5],after=new int[5];
            for(int s=0;s<5;s++) gain[s]=Rules.Grow(g.Value[s],g.Stored[s],g.Rate[s],levels,g.Max,out after[s]);
            int[] stored=g.Stored;
            model.PhysicalResult=gain[0]; model.PerceiveResult=gain[1]; model.CraftResult=gain[2];
            model.KnowledgeResult=gain[3]; model.CharmResult=gain[4];
            growthPending=new(ctrl.Pointer,cur.RoleId,cur.Level,levels,after);
            log.Msg($"Growth role {cur.RoleId} Lv {cur.Level}+{levels}: gain {string.Join("/",gain)} (phy/per/cra/kno/cha), progress {string.Join("/",stored)} -> {string.Join("/",after)} per-mille.");
        } catch(Exception ex) { Fail("growth roll",ex); }
    }

    // Inputs of the cumulative growth for the window's current role (also used by the slider preview).
    private sealed record GrowthInput(int[] Rate,int[] Value,int[] Stored,int Max);
    private static GrowthInput? ReadGrowth(CurRoleData cur,PlayerRoleData role,out string? problem) {
        problem=null;
        var hero=cur.HeroTemplate;
        if(hero==null) { problem="hero template missing"; return null; }
        int[] grade={hero.phycGrowth,hero.percGrowth,hero.craftGrowth,hero.knowGrowth,hero.chamGrowth};
        int[] rate=new int[5];
        for(int s=0;s<5;s++) {
            var type=TemplateManager.GetRoleGrowthType(grade[s]);
            rate[s]=Rules.RatePerMille(Rules.Letter(type?.code,grade[s]));
            if(rate[s]<0) { problem=$"{StatNames[s]} grade {grade[s]} unknown"; return null; }
        }
        return new(rate,new[]{cur.Physical,cur.Perceive,cur.Craft,cur.Knowledge,cur.Charm},
            new[]{role.Physical_Exp,role.Perceive_Exp,role.Craft_Exp,role.Knowledge_Exp,role.Charm_Exp},UIHeroLevelUpModel.MaxProp);
    }

    private static void AfterSingle(UIHeroLevelUpCtrl __instance)=>GrowthAfter(__instance,false);

    // AniResult postfix: the stats were applied; store the progress that goes with them.
    private static void AfterAniResult(UIHeroLevelUpCtrl __instance) {
        var p=growthPending; growthPending=null;
        if(p==null || !enabled || p.Ctrl!=__instance.Pointer) return;
        try {
            var db=PlayerDataManager.Instance?.Data?.PlayerRole;
            var role=db?.FindHoldRole(p.Role);
            if(db==null || role==null) { log.Warning($"Growth: role {p.Role} not found after the level-up; progress not stored."); return; }
            role.Physical_Exp=p.Progress[0]; role.Perceive_Exp=p.Progress[1]; role.Craft_Exp=p.Progress[2];
            role.Knowledge_Exp=p.Progress[3]; role.Charm_Exp=p.Progress[4];
            db.MarkDBDirty();
        } catch(Exception ex) { Fail("growth store",ex); }
    }
}

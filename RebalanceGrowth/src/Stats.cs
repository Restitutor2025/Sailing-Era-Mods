using Il2CppClient.PlayerStore;
using Il2CppGyyx.Template;

namespace Restitutor.RebalanceGrowth;

// 0.2.0: HP / attack gain divisor 100 -> 250 (user). The native functions add one level's gain
//   round(RoleLevel(level).hpRatio|attackRatio x (1 + physical/100))
// to max HP (ModifyProperty 8 .MaxData via UpdateBaseData) / attack (PointProperty 7 .BaseData).
// Postfix only: after the original ran, the difference (new gain - original gain) is applied the same way.
// Covers every caller: level-up (UpdateRoleLevelData) and physical rewards (AddRolePropPhysical, RewardUtils.AddRoleProperty).
// Player roles only (PlayerRoleData); NpcData has its own functions and is not changed.
public sealed partial class EntryPoint {
    private static void AfterMaxHp(PlayerRoleData __instance) {
        if(!enabled) return;
        try {
            if(!Inputs(__instance,out int physical,out var row)) return;
            int d=Rules.GainCorrection(physical,row!.hpRatio);
            if(d==0) return;
            var hp=__instance.GetModifyProperty(8);
            if(hp==null) return;
            hp.UpdateBaseData(d,false);        // same call the original makes; clamps current HP to the new max
        } catch(Exception ex) { Fail("max hp correction",ex); }
    }

    private static void AfterAttack(PlayerRoleData __instance) {
        if(!enabled) return;
        try {
            if(!Inputs(__instance,out int physical,out var row)) return;
            int d=Rules.GainCorrection(physical,row!.attackRatio);
            if(d==0) return;
            var atk=__instance.GetPointProperty(7);
            if(atk==null) return;
            int v=atk.BaseData+d;
            atk.BaseData=v<0?0:v;              // original writes BaseData the same way (floored at 0)
        } catch(Exception ex) { Fail("attack correction",ex); }
    }

    // Inputs exactly as the native code reads them: PointProperty(1).BaseData and RoleLevel(level).
    private static bool Inputs(PlayerRoleData role,out int physical,out RoleLevel? row) {
        physical=0; row=null;
        var p=role.GetPointProperty(1);
        if(p==null) return false;
        row=TemplateManager.GetRoleLevel(role.Level);
        if(row==null) return false;
        physical=p.BaseData;
        return true;
    }

    // 0.3.1: level-up window "+N" for HP / attack (UIHeroLevelUpView.GetAddPropByType 0xB86A20) still used the
    // original /100: sum over rows level+1 .. level+n of round((phys + PhysicalResult)/100 + 1) x hp|attackRatio,
    // n = MaxContinueLevel on the continue path else 1 (user saw +7 HP / +4 attack at physical 78, applied is +5 / +2).
    // Same inputs, divisor 250 = what UpdateMaxHpValue / UpdateAttack now add (Rules.Gain).
    private static void AfterAddProp(Il2CppClient.UILogic.UIHeroLevelUp.UIHeroLevelUpView __instance,Il2CppClient.Const.ERolePropertyType __0,ref int __result) {
        if(!enabled) return;
        int type=(int)__0; if(type!=7 && type!=8) return;
        try {
            var model=__instance._model; var cur=model?.CurRole;
            if(model==null || cur==null) return;
            var role=Il2CppClient.Manager.PlayerDataManager.Instance?.Data?.PlayerRole?.FindHoldRole(cur.RoleId);
            var pp=role?.GetPointProperty(1); if(pp==null) return;
            int n=model.IsClickContinueBtn ? model.MaxContinueLevel : 1;
            int phys=pp.BaseData+model.PhysicalResult, sum=0;
            for(int lv=cur.Level+1;lv<=cur.Level+n;lv++) {
                var row=TemplateManager.GetRoleLevel(lv); if(row==null) return;   // original returns early too
                sum+=Rules.Gain(phys,type==7?row.attackRatio:row.hpRatio,Rules.PhysicalDivisor);
            }
            __result=sum;
        } catch(Exception ex) { Fail("hp/attack display",ex); }
    }
}

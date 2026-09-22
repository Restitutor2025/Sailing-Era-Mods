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
}

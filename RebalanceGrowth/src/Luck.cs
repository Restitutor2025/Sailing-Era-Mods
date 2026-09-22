using Il2CppClient.PlayerStore;
using Il2CppGyyx.Template;

namespace Restitutor.RebalanceGrowth;

// Luck (id 6), 0.2.0:
//  - PlayerRoleData.GetLuckyValue (0x6798A0) = stored + (level-1) x upValue  -> capped at 99.
//  - PlayerRoleData.GetRolePropByID(6) (0x676B30) = stored only (used by land-exploration property checks,
//    the Fate Coin chance and the role list there, equipment conditions) -> same growth-included value, capped at 99.
// The growth part is computed here instead of calling the native GetLuckyValue, because the native one throws
// NullReferenceException for roles whose Role template is not a hero (type != 1) or whose hero row is missing.
public sealed partial class EntryPoint {
    private static void AfterLuckyValue(ref int __result) {
        if(!enabled) return;
        if(__result>Rules.LuckyMax) __result=Rules.LuckyMax;
    }

    private static void AfterRolePropById(PlayerRoleData __instance,int __0,ref int __result) {
        if(!enabled || __0!=Rules.LuckyId) return;
        try {
            int up=LuckyUpValue(__instance);
            __result=up<0 ? Rules.CapLucky(__result) : Rules.Lucky(__result,__instance.Level,up);
        } catch(Exception ex) { Fail("luck check value",ex); }
    }

    // Same template walk as the native GetLuckyValue: Role(roleId).type==1 -> Hero(role.tid).luckyGrowth -> RoleGrowthType.upValue.
    // -1 when any step is missing (then the stored value is only capped).
    private static int LuckyUpValue(PlayerRoleData role) {
        var r=TemplateManager.GetRole(role.RoleId);
        if(r==null || r.type!=1) return -1;
        var hero=TemplateManager.GetHero(r.tid);
        if(hero==null) return -1;
        var g=TemplateManager.GetRoleGrowthType(hero.luckyGrowth);
        return g==null ? -1 : g.upValue;
    }
}

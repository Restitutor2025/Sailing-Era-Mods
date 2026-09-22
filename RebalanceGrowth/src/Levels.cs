using Il2CppClient.PlayerStore;
using Il2CppGyyx.Template;

namespace Restitutor.RebalanceGrowth;

// 0.3.0 (user): the RoleLevel extension (rows 100..200) no longer depends on the GameAssembly check, so a role
// above 99 always has its row. Without the row the native code throws NullReferenceException in
// UpdateMaxHpValue / UpdateAttack (0x679E20 / 0x679F70) and get_MaxExp (0x676ED0). These checks only log
// (the original still runs unchanged): which role/level has no row, before the native throw happens.
public sealed partial class EntryPoint {
    private static bool tablesOn;
    private static readonly HashSet<long> rowMissingLogged=new();

    private static bool HasRow(int level)=>TemplateManager.GetRoleLevel(level)!=null;

    private static void RowMissing(string where,int role,int level) {
        if(!rowMissingLogged.Add(((long)role<<32)|(uint)level)) return;
        log.Error($"[level row] {where}: role {role} is level {level} but RoleLevel has no row {level} " +
                  $"(rows now {TemplateManager._roleLevel?.Count ?? -1}); the game's own code throws NullReferenceException here. " +
                  "Look for the 'RoleLevel' line at start-up (extension added / refused).");
    }

    // UpdateMaxHpValue / UpdateAttack prefix: log only, never skips the original.
    private static void CheckRow(PlayerRoleData __instance) {
        if(!tablesOn) return;
        try { int lv=__instance.Level; if(lv>0 && !HasRow(lv)) RowMissing("hp/attack update",__instance.RoleId,lv); }
        catch(Exception ex) { Fail("level row check",ex); }
    }

    // PlayerHoldRoleDB.Deserialize postfix: after a save loads, every held role / seaman must have its row.
    private static void AfterRolesLoaded(PlayerHoldRoleDB __instance) {
        if(!tablesOn) return;
        try {
            int bad=0,top=0;
            void Visit(Il2CppSystem.Collections.Generic.List<PlayerRoleData>? list) {
                if(list==null) return;
                for(int i=0;i<list.Count;i++) {
                    var r=list[i]; if(r==null) continue;
                    top=Math.Max(top,r.Level);
                    if(r.Level>0 && !HasRow(r.Level)) { bad++; RowMissing("save loaded",r.RoleId,r.Level); }
                }
            }
            Visit(__instance.Heros); Visit(__instance.LeaveHeroes);
            Visit(__instance.GetAllseamenByEmployed(true)); Visit(__instance.GetAllseamenByEmployed(false));   // hired seamen (argument meaning not checked: both read)
            if(bad==0 && top>Rules.OriginalMaxLevel) log.Msg($"[level row] save loaded: highest role level {top}, rows present.");
        } catch(Exception ex) { Fail("level row check (load)",ex); }
    }
}

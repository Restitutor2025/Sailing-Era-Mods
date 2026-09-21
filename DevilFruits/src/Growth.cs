using Il2CppClient.PlayerStore;
using Il2CppGyyx.Template;

namespace Restitutor.DevilFruits;

// Growth grades live only in the shared Hero template (not in the save). The save keeps one
// fruit tid per eaten fruit in PlayerRoleData.ListSkillBooks (native: Deserialize copies the
// list 1:1 with an identity Select, b__130_0 0x533AA0; native readers only ask Contains(bookId)
// for real skill-book ids). Resync rewrites every touched hero from original - eaten fruits,
// so loading another save (or a new game) restores heroes that have no record there.
public sealed partial class EntryPoint {
    private static readonly Dictionary<int,int[]> originals=new();

    internal static int GetGrade(Hero h,int stat)=>stat switch {
        0=>h.phycGrowth, 1=>h.percGrowth, 2=>h.craftGrowth, 3=>h.knowGrowth, 4=>h.chamGrowth, _=>0 };
    private static void SetGrade(Hero h,int stat,int v) {
        switch(stat){case 0:h.phycGrowth=v;break;case 1:h.percGrowth=v;break;case 2:h.craftGrowth=v;break;case 3:h.knowGrowth=v;break;case 4:h.chamGrowth=v;break;}
    }
    private static int[] Original(int heroTid,Hero h) {
        if(!originals.TryGetValue(heroTid,out var o)) {
            o=new int[Rules.Count];
            for(int s=0;s<Rules.Count;s++) o[s]=GetGrade(h,s);
            originals[heroTid]=o;
        }
        return o;
    }

    internal static int[] StepsOf(PlayerRoleData role) {
        var list=role.ListSkillBooks;
        if(list==null) return new int[Rules.Count];
        var ids=new List<int>(list.Count);
        for(int i=0;i<list.Count;i++) ids.Add(list[i]);
        return Rules.Steps(ids);
    }

    private static void Resync(PlayerHoldRoleDB? db,string why) {
        if(!enabled || db==null) return;
        try {
            EnsureTemplates(why);
            var seen=new HashSet<int>();
            int changed=0;
            void Visit(Il2CppSystem.Collections.Generic.List<PlayerRoleData>? roles) {
                if(roles==null) return;
                for(int i=0;i<roles.Count;i++) {
                    var role=roles[i];
                    if(role==null || role.IsSeaman) continue;
                    var steps=StepsOf(role);
                    bool any=steps.Any(x=>x>0);
                    if(!any && !originals.ContainsKey(role.RoleId)) continue;   // never touched
                    var h=role.GetHeroTemplate();
                    if(h==null) continue;
                    var o=Original(role.RoleId,h);
                    seen.Add(role.RoleId);
                    for(int s=0;s<Rules.Count;s++) {
                        int v=Rules.Applied(o[s],steps[s]);
                        if(o[s]-steps[s]<1) log.Warning($"role {role.RoleId} {Rules.StatNames[s]}: original {o[s]} with {steps[s]} fruit record(s) is below S; clamped to S.");
                        if(GetGrade(h,s)!=v){SetGrade(h,s,v);changed++;}
                    }
                }
            }
            Visit(db.Heros);
            Visit(db.LeaveHeroes);
            // Heroes touched in an earlier save but absent from this one: back to the table value.
            foreach(var kv in originals) {
                if(seen.Contains(kv.Key)) continue;
                var h=TemplateManager.GetHero(kv.Key);
                if(h==null) continue;
                for(int s=0;s<Rules.Count;s++) if(GetGrade(h,s)!=kv.Value[s]){SetGrade(h,s,kv.Value[s]);changed++;}
            }
            if(changed>0) log.Msg($"Growth resync ({why}): {changed} grade field(s) written; tracked heroes {originals.Count}.");
        } catch(Exception ex) { Fail("resync "+why,ex); }
    }

    // PlayerHoldRoleDB.Deserialize / InitHook postfixes: a save was loaded or a new game started.
    private static void AfterRolesLoaded(PlayerHoldRoleDB __instance)=>Resync(__instance,"roles loaded");
    private static void AfterRolesInit(PlayerHoldRoleDB __instance)=>Resync(__instance,"roles init");
    // UIHeroLevelUpCtrl.OnClickBtnLevelUp prefix: the level-up roll reads the template grades.
    private static void BeforeLevelUp() {
        try { Resync(Il2CppClient.UILogic.UICharacter.UICharacterCtrl.Data?.PlayerRole,"level up"); } catch(Exception ex) { Fail("level up",ex); }
    }
}

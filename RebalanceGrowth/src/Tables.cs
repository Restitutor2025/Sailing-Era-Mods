using Il2CppGyyx.Template;
using Il2CppInterop.Runtime;

namespace Restitutor.RebalanceGrowth;

// TemplateManager keeps each table in a static dictionary that every Get* caller reads, so the new
// values are set once per table load (InitGameConst / InitRoleLevel postfix) instead of hooking getters.
public sealed partial class EntryPoint {
    private static string? loggedConsts;
    private static int loggedLevels=-1;

    private static void AfterGameConst() { if(enabled) ApplyConsts("InitGameConst"); }
    // 0.3.0: installed without the GameAssembly check (data only; user), see Levels.cs.
    private static void AfterRoleLevel() { if(tablesOn) ApplyLevels("InitRoleLevel"); }

    private static void ApplyConsts(string why) {
        try {
            var dict=TemplateManager._gameConst;
            if(dict==null) return;
            string a=SetConst(dict,Rules.SkillIntervalKey,Rules.SkillInterval);
            string b=SetConst(dict,Rules.StatMaxKey,Rules.StatMax);
            string line=$"GameConst ({why}): {a}; {b}";
            if(loggedConsts!=a+b){ loggedConsts=a+b; log.Msg(line); }
        } catch(Exception ex) { Fail("game const",ex); }
    }

    private static string SetConst(Il2CppSystem.Collections.Generic.Dictionary<string,GameConst> dict,string key,int want) {
        if(!dict.ContainsKey(key)) return $"{key} missing (unchanged)";
        var row=dict[key];
        string before=row.value??"(null)",text=want.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if(before==text) return $"{key}={text}";
        row.value=text;
        return $"{key} {before}->{text}";
    }

    // Allocated like any il2cpp object; the native constructor (which reads a FlatBuffers row) is not run,
    // every field is set below (RoleLevel has only value fields: 5 int + 2 float, ctor 0xE6D4A0).
    private static RoleLevel NewRow(Rules.Row r) {
        var row=new RoleLevel(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<RoleLevel>.NativeClassPtr));
        row.tid=r.Tid; row.level=r.Level; row.exp=r.Exp; row.maxSkillCount=r.MaxSkillCount;
        row.skillPoint=r.SkillPoint; row.hpRatio=r.HpRatio; row.attackRatio=r.AttackRatio;
        return row;
    }

    private static void ApplyLevels(string why) {
        try {
            var dict=TemplateManager._roleLevel;
            if(dict==null || dict.Count==0) return;
            // The extension follows the original rule only if the loaded table still is that rule.
            for(int lv=2;lv<=Rules.OriginalMaxLevel;lv++) {
                if(!dict.ContainsKey(lv)) { Refuse(why,$"row {lv} missing"); return; }
                var o=dict[lv]; var w=Rules.RowFor(lv);
                if(o.tid!=w.Tid || o.level!=w.Level || o.exp!=w.Exp || o.maxSkillCount!=w.MaxSkillCount || o.skillPoint!=w.SkillPoint || o.hpRatio!=w.HpRatio || o.attackRatio!=w.AttackRatio)
                { Refuse(why,$"row {lv} = ({o.tid},{o.level},{o.exp},{o.maxSkillCount},{o.skillPoint},{o.hpRatio},{o.attackRatio}) differs from the known table"); return; }
            }
            int before=dict.Count,added=0;
            if(before!=Rules.OriginalMaxLevel && !(before==Rules.MaxLevel && dict.ContainsKey(Rules.MaxLevel))) { Refuse(why,$"{before} rows (expected {Rules.OriginalMaxLevel})"); return; }
            // Keys in ascending order: PlayerRoleData.get_MaxLevel (0x676D90) takes the last value's level.
            for(int lv=Rules.OriginalMaxLevel+1;lv<=Rules.MaxLevel;lv++)
                if(!dict.ContainsKey(lv)) { dict[lv]=NewRow(Rules.RowFor(lv)); added++; }
            if(loggedLevels!=dict.Count){ loggedLevels=dict.Count; log.Msg($"RoleLevel ({why}): {before} -> {dict.Count} rows (+{added}); max level = row count = {dict.Count}."); }
        } catch(Exception ex) { Fail("role level",ex); }
    }

    private static void Refuse(string why,string reason) {
        string key="levels:"+reason;
        if(lastError==key) return;
        lastError=key; log.Error($"RoleLevel ({why}): {reason}; levels above {Rules.OriginalMaxLevel} NOT added.");
    }
}

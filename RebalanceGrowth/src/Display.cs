using Il2CppClient.UILogic.UICharacter;
using Il2CppClient.UILogic.UIHeroLevelUp;
using Il2CppClient.Utils;
using Il2CppGyyx.Template;

namespace Restitutor.RebalanceGrowth;

// Two labels whose native code hardcodes the old level cap 99 (the rest reads the RoleLevel table):
//  - level-up window "next skill point in N levels" (UIHeroLevelUpView.RefreshRoleData inline loop 0xB84B3F):
//    searches only up to level 98, so from ~95 on it said "no more points" although levels continue to 200.
//  - Tab character tip (UICharacterView.RefreshTipsRoleInfo 0x102B8D8 / 0x102BD72): MAX mark when level==99,
//    need-exp text blank when level>=99.
// Postfixes rewrite only those cases; below level 99 the original result is left untouched.
public sealed partial class EntryPoint {
    private const string NextSkillKey="UIStatic_Character_TexTitleSkillDesc";   // literal at 0xB84D95

    private static void AfterLevelUpRefresh(UIHeroLevelUpView __instance) {
        if(!enabled) return;
        try {
            var role=__instance._model?.CurRole;
            if(role==null) return;
            int level=role.Level,max=UIHeroLevelUpModel.MaxLevel,interval=UIHeroLevelUpModel.LevelGetSkill;
            if(!Rules.FixNextLabel(level,max,interval)) return;
            var label=__instance.ComRole?.texTitleSkillDesc;
            if(label==null || label.isDisposed) return;
            label.text=string.Format(TextLibUtils.Text(NextSkillKey,""),Rules.NextDiff(level,max,interval));
        } catch(Exception ex) { Fail("level-up label",ex); }
    }

    private static void AfterTipRefresh(UICharacterView __instance) {
        if(!enabled) return;
        try {
            var model=__instance._model;
            var list=model?.ListRole;
            if(model==null || list==null) return;
            int index=model.RoleIndex;
            if(index<0 || index>=list.Count) return;
            var role=list[index]?.HeroData;
            if(role==null) return;
            int level=role.Level;
            if(!Rules.FixTip(level)) return;
            var tip=__instance.TipsRole;
            if(tip==null) return;
            int max=UIHeroLevelUpModel.MaxLevel;
            var mark=tip.isMax; if(mark!=null) mark.selectedIndex=Rules.TipMaxIndex(level,max);   // native uses the selectedIndex setter too
            var need=tip.texNeedExp;
            if(need!=null && !need.isDisposed)
                need.text=Rules.TipShowsExp(level,max) ? (TemplateManager.GetRoleLevel(level)?.exp.ToString(System.Globalization.CultureInfo.InvariantCulture)??"") : "";
        } catch(Exception ex) { Fail("character tip",ex); }
    }
}

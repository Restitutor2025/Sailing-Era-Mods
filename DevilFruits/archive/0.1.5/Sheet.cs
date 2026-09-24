using Il2CppCharacter;
using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using Il2CppGyyx.Template;
using UnityEngine;

namespace Restitutor.DevilFruits;

// Tab character sheet: a left click on a Stat Rank grade letter (S~D) opens the use dialog for
// that ability's fruit. The letters belong to Restitutor_Additional_Stat_Rank (GTextField named
// "RestitutorStatRank", placed beside UICom_Prop.TexPropTitle). UISheetCharacter.btnReturn
// covers the letters and is the hit target (Stat Rank 0.1.1 trace, Tab Characters 0.6.7 trace),
// so the press is taken on btnReturn itself, exactly like Tab Characters' language catcher:
// only a press AND release on the same letter cancel the native btnReturn click. Other presses
// pass through unchanged. 0.1.1: every navigator letter opens the panel, also with 0 fruits or at
// S, so the panel can say why the fruit cannot be used (0.1.0 passed those clicks through silently).
public sealed partial class EntryPoint {
    private const string StatRankLabel="RestitutorStatRank";
    private static UICharacterView? sheetView;
    private static GObject? catcher;
    private static EventCallback1? catchBegin,catchEnd;
    private static int armedStat=-1;
    private static bool warnedNoLetters;

    private static UICom_Prop?[] Props(UICharacterView v)=>new UICom_Prop?[]{v.Physical,v.Perceive,v.Craft,v.Knowledge,v.Charm};

    private static GTextField? Letter(UICom_Prop? prop) {
        var parent=prop==null || prop.isDisposed ? null : prop.TexPropTitle?.parent;
        if(parent==null) return null;
        for(int i=0;i<parent.numChildren;i++){var c=parent.GetChildAt(i);if(c!=null && c.name==StatRankLabel) return c.TryCast<GTextField>();}
        return null;
    }
    private static bool InsideAt(GObject? o,float x,float y) {
        if(o==null || o.isDisposed || !o.visible || !o.onStage) return false;
        var l=o.GlobalToLocal(new Rect(x,y,0,0));
        return l.x>=0 && l.y>=0 && l.x<=o.width && l.y<=o.height;
    }
    private static int StatAt(UICharacterView v,float x,float y) {
        var props=Props(v); bool anyLetter=false;
        for(int s=0;s<props.Length;s++){var l=Letter(props[s]);if(l!=null)anyLetter=true;if(InsideAt(l,x,y))return s;}
        if(!anyLetter && !warnedNoLetters){warnedNoLetters=true;log.Warning("Stat Rank grade letters not found on the sheet; fruits cannot be used (is Restitutor_Additional_Stat_Rank installed?).");}
        return -1;
    }

    // Selected navigator of the open sheet, or null for seamen / empty selection.
    private static Il2CppClient.PlayerStore.PlayerRoleData? SelectedRole(UICharacterView v,out CharacterData? data) {
        data=null;
        var model=v._model;
        if(model?.ListRole==null || model.RoleIndex<0 || model.RoleIndex>=model.ListRole.Count) return null;
        data=model.ListRole[model.RoleIndex];
        if(data==null || data.isSeaman) return null;
        return data.HeroData;
    }
    private static int Owned(int stat) {
        try { return UICharacterCtrl.Data?.PlayerBag?.GetItemAmount(Rules.ItemTid(stat)) ?? 0; } catch { return 0; }
    }

    // UICharacterView.RefreshTipsRoleInfo PREFIX: grades are resynced before Stat Rank's postfix reads them.
    private static void BeforeRoleInfo(UICharacterView __instance) {
        if(!enabled) return;
        try {
            Resync(UICharacterCtrl.Data?.PlayerRole,"sheet refresh");
            Bind(__instance);
        } catch(Exception ex) { Fail("sheet refresh",ex); }
    }
    // UICharacterView.HideHook postfix: the character window closed.
    private static void AfterHide() {
        try { CloseDialog(); Unbind(); } catch(Exception ex) { Fail("hide",ex); }
    }

    private static void Bind(UICharacterView view) {
        sheetView=view;
        // 0.1.1: the first RefreshTipsRoleInfo can run before the sheet content exists; native
        // get_SheetCharacter then throws NullReferenceException (user log 22:22:48). Bind later.
        var content=view._UIContent_k__BackingField;
        if(content==null || content.isDisposed) return;
        var target=view.SheetCharacter?.btnReturn;
        if(target==null || target.isDisposed || catcher?.Pointer==target.Pointer) return;
        Unbind(); sheetView=view;
        catchBegin=(EventCallback1)(Action<EventContext>)(e=>Guard("press",()=>{
            armedStat=-1;
            var v=sheetView;
            if(v==null || e.inputEvent.button!=0 || dialog!=null) return;
            int s=StatAt(v,e.inputEvent.x,e.inputEvent.y);
            if(s<0 || SelectedRole(v,out _)==null) return;                   // not a letter / seaman: native behaviour
            armedStat=s; e.CaptureTouch();
        }));
        catchEnd=(EventCallback1)(Action<EventContext>)(e=>Guard("release",()=>{
            int s=armedStat; armedStat=-1;
            var v=sheetView;
            if(s<0 || v==null || e.inputEvent.button!=0) return;
            e.StopPropagation(); Stage.inst.CancelClick(e.inputEvent.touchId);
            if(StatAt(v,e.inputEvent.x,e.inputEvent.y)!=s) return;           // released elsewhere
            pendingOpen=s;                                                    // open next frame, outside FairyGUI dispatch
        }));
        target.onTouchBegin.Add(catchBegin);
        target.onTouchEnd.Add(catchEnd);
        catcher=target;
    }
    private static void Unbind() {
        var t=catcher; catcher=null; armedStat=-1; sheetView=null;
        if(t!=null && !t.isDisposed){ if(catchBegin!=null)t.onTouchBegin.Remove(catchBegin); if(catchEnd!=null)t.onTouchEnd.Remove(catchEnd); }
        catchBegin=null; catchEnd=null;
    }

    internal static string GradeLetter(int grade) {
        try { var t=TemplateManager.GetRoleGrowthType(grade); return Rules.Letter(t?.code,grade); } catch { return Rules.Letter(null,grade); }
    }

    // Final checks and the only write path. Order: re-validate -> remove 1 fruit (must succeed)
    // -> append the record to ListSkillBooks + PlayerHoldRoleDB dirty (same as native
    // OnClickBtnBook 0x94BFEE/0x94BFF8) -> template -> refresh sheet -> native notice.
    private static void Eat(int stat) {
        var v=sheetView;
        if(v==null) return;
        var role=SelectedRole(v,out var data);
        var db=UICharacterCtrl.Data?.PlayerRole;
        var bag=UICharacterCtrl.Data?.PlayerBag;
        if(role==null || data==null || db==null || bag==null) return;
        var live=db.FindHoldRole(role.RoleId);
        if(live==null || live.Pointer!=role.Pointer){log.Warning("use cancelled: selected navigator is not the live role object.");return;}
        Resync(db,"before use");
        var hero=role.GetHeroTemplate();
        if(hero==null || role.ListSkillBooks==null){log.Warning($"use cancelled: role {role.RoleId} has no hero template or skill-book list.");return;}
        int before=GetGrade(hero,stat);
        if(Rules.IsTop(before)) return;
        int tid=Rules.ItemTid(stat);
        if(bag.GetItemAmount(tid)<=0) return;
        if(!bag.RemoveItem(tid,1)){log.Warning($"use cancelled: RemoveItem({tid},1) returned false.");return;}
        role.ListSkillBooks.Add(tid);
        db.MarkDBDirty();
        Resync(db,"after use");
        int after=GetGrade(hero,stat);
        log.Msg($"role {role.RoleId}: {Rules.ItemName(stat)} eaten, {Rules.StatNames[stat]} growth {before}->{after}; records {StepsOf(role)[stat]}.");
        try {
            var model=v._model;
            v.RefreshSheetCharacter(data); v.RefreshTipsRoleInfo(); model?.MarkDirty();
        } catch(Exception ex) { Fail("refresh after use",ex); }
        try { Il2CppClient.UILogic.UITips.UITipsCtrl.Instance.ShowBaseTips($"{Rules.StatNames[stat]} 성장 등급이 {GradeLetter(before)}에서 {GradeLetter(after)}로 올랐습니다.",""); }
        catch(Exception ex) { Fail("notice",ex); }
    }
}

using MelonLoader;
using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.PlayerStore.Property;
using Il2CppClient.UILogic.UICharacter;
using Il2CppClient.Utils;
using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
[assembly: MelonInfo(typeof(Restitutor.Cheats.Character.EntryPoint),"Restitutor Cheats Character","1.1.1","Restitutor")]
[assembly: MelonGame("bolingo","SailingEra")]
[assembly: MelonAdditionalDependencies("Restitutor_Cheats_Interface")]
namespace Restitutor.Cheats.Character;

// Editable values of the character selected in the Tab character sheet.
// Property ids = Client.Const.ERolePropertyType (Physical 1 .. Lucky 6, Attack 7, Hp 8).
internal enum Field { HpCur, HpMax, Attack, Physical, Craft, Perceive, Charm, Knowledge, Lucky }

internal sealed class Target {
    internal readonly UICharacterCtrl Ctrl; internal readonly UICharacterView View; internal readonly UICharacterModel Model;
    internal readonly int Index; internal readonly PlayerRoleData Role; internal readonly PlayerHoldRoleDB Db;
    internal Target(UICharacterCtrl c,UICharacterView v,UICharacterModel m,int i,PlayerRoleData r,PlayerHoldRoleDB d){Ctrl=c;View=v;Model=m;Index=i;Role=r;Db=d;}
}

public sealed class EntryPoint:MelonMod {
    private static bool loaded,applying;
    private static MelonLogger.Instance log=null!;
    private static readonly CharacterPanel panel=new();
    public override void OnInitializeMelon() {
        log=LoggerInstance;
        if(!Host.Enabled) { log.Error("Cheats Interface disabled; Character panel not registered.");return; }
        Host.Register(panel);loaded=true;
        log.Msg("Cheats Character 1.1.1 loaded (row text only on change); two-column 640 layout (needs Interface 1.4.0); no native hooks; Tab character sheet only (heroes, not seamen); HP/attack/6 abilities/owned skill levels.");
    }
    public override void OnDeinitializeMelon() { loaded=false;Host.Unregister(panel); }

    private static bool Visible(GObject? content) {
        if(content==null || content.isDisposed || !content.onStage) return false;
        for(int depth=0;content!=null;content=content.parent,depth++)
            if(depth>=128 || content.isDisposed || !content.internalVisible || !content.internalVisible2) return false;
        return true;
    }

    // Resolved once per frame: Host calls Refresh and Visible in the same frame.
    private static int cachedFrame=-1;
    private static Target? cached;
    internal static Target? Current() {
        int frame=UnityEngine.Time.frameCount;
        if(frame!=cachedFrame) { cachedFrame=frame;cached=null;try{cached=Resolve();}catch(Exception ex){log.Error(ex.ToString());} }
        return cached;
    }
    // Visible only while the Tab character sheet (ESheetType.Character) is open and shown,
    // the selected entry is a hero (not a seaman), and it is the same object held by the player's role DB.
    private static Target? Resolve() {
        var p=Host.Player;
        if(!loaded || !Host.Enabled || p==null || PlayerDataManager.Instance?.Data?.Pointer!=p.Pointer) return null;
        var opened=UIManager.Instance?._alreadyOpenedUICtrls;
        var db=p.PlayerRole;
        if(opened==null || db==null) return null;
        foreach(var entry in opened) {
            var ctrl=entry.Value?.TryCast<UICharacterCtrl>();
            if(ctrl==null) continue;
            var view=ctrl.View;var model=ctrl.Model;
            if(view?._state==null || model==null || !view.IsOpen() || !Visible(view.UIContent)) continue;
            if(model.SheetType!=ESheetType.Character) continue;
            var list=model.ListRole;int index=model.RoleIndex;
            if(list==null || index<0 || index>=list.Count) continue;
            var data=list[index];
            if(data==null || data.isSeaman) continue;
            var role=data.HeroData;
            if(role==null) continue;
            var live=db.FindHoldRole(role.RoleId);
            if(live==null || live.Pointer!=role.Pointer) continue;
            return new Target(ctrl,view,model,index,role,db);
        }
        return null;
    }

    internal static string Localize(string? key,string fallback) {
        if(string.IsNullOrEmpty(key)) return fallback;
        try { var s=TextLibUtils.Text(key,key); return string.IsNullOrWhiteSpace(s)?fallback:s; }
        catch { return fallback; }
    }
    internal static string RoleName(PlayerRoleData role) {
        string fallback="#"+role.RoleId;
        try { return Localize(role.GetRoleTemplate()?.name,fallback); } catch { return fallback; }
    }
    internal static string SkillName(RoleSkillData skill) {
        string fallback="#"+skill.Id;
        try { return Localize(skill.GetTemplate()?.name,fallback); } catch { return fallback; }
    }

    private static ModifyProperty Hp(PlayerRoleData r)=>r.GetModifyProperty(8) ?? throw new InvalidOperationException("HP property missing");
    private static int PropertyId(Field f)=>f switch {
        Field.Physical=>1, Field.Craft=>2, Field.Perceive=>3, Field.Charm=>4, Field.Knowledge=>5, Field.Lucky=>6, Field.Attack=>7,
        _=>throw new ArgumentOutOfRangeException(nameof(f)) };
    private static PointProperty Point(PlayerRoleData r,Field f)=>r.GetPointProperty(PropertyId(f)) ?? throw new InvalidOperationException(f+" property missing");

    // Base = the stored value the cheat writes. Shown = the value the game displays (base + equipment/growth effects).
    internal static int Base(PlayerRoleData r,Field f)=>f switch {
        Field.HpCur=>Hp(r).CurData, Field.HpMax=>Hp(r).MaxData, _=>Point(r,f).BaseData };
    internal static int Shown(PlayerRoleData r,Field f)=>f switch {
        Field.HpCur=>r.Hp, Field.HpMax=>r.MaxHp, Field.Attack=>r.Attack, Field.Physical=>r.Physical, Field.Craft=>r.Craft,
        Field.Perceive=>r.Perception, Field.Charm=>r.Charisma, Field.Knowledge=>r.Knowledge, Field.Lucky=>r.Lucky,
        _=>throw new ArgumentOutOfRangeException(nameof(f)) };
    internal static (int min,int max) Limits(PlayerRoleData r,Field f)=>f switch {
        Field.HpCur=>(0,Hp(r).MaxData), Field.HpMax=>(Rules.HpMaxMin,Rules.ValueMax), _=>(0,Rules.ValueMax) };

    private static void Write(PlayerRoleData r,Field f,int value) {
        switch(f) {
            case Field.HpCur: Hp(r).SetCurrentData(value); break;             // native clamps to [0, MaxData]
            case Field.HpMax: { var hp=Hp(r); hp.MaxData=value; hp.LimitCurData(); break; } // same fields GM ChangeRoleHp writes
            default: Point(r,f).UpdateBaseData(value); break;                   // native: BaseData = max(0,value)
        }
    }

    private static bool Same(Target t,IntPtr role)=>t.Role.Pointer==role;
    internal static void ApplyField(IntPtr shownRole,Field f,int target,string label) {
        if(applying) return;
        try {
            applying=true;
            var t=Current();
            if(t==null || !Same(t,shownRole)) { panel.Message("대상이 바뀌었습니다. 다시 적용하세요.");return; }
            var (min,max)=Limits(t.Role,f);
            if(target<min || target>max) { panel.Message($"{label}: {min:N0}~{max:N0}만 가능");return; }
            int before=Base(t.Role,f);
            int after=Rules.Apply(target,()=>Base(t.Role,f),v=>Write(t.Role,f,v));
            if(after!=before) t.Db.MarkDBDirty();
            RefreshGame(t);
            panel.Message(after==before?$"{label}: 이미 {after:N0}":$"{label}: {before:N0} → {after:N0}");
            log.Msg($"role={t.Role.RoleId} {f} before={before} target={target} actual={after} shown={Shown(t.Role,f)}");
        } catch(Exception ex) { log.Error(ex.ToString());panel.Message("적용 오류 · 값과 로그 확인"); }
        finally { applying=false; }
    }
    internal static void ApplySkill(IntPtr shownRole,int skillId,int target,string label) {
        if(applying) return;
        try {
            applying=true;
            var t=Current();
            if(t==null || !Same(t,shownRole)) { panel.Message("대상이 바뀌었습니다. 다시 적용하세요.");return; }
            var skill=t.Role.GetRoleSkillById(skillId);
            if(skill==null) { panel.Message("스킬을 찾을 수 없습니다.");return; }
            int max=skill.MaxLevel,before=skill.LevelBase;
            if(target<0 || target>max) { panel.Message($"{label}: 0~{max}만 가능");return; }
            if(before!=target) {
                var (raise,delta)=Rules.SkillPlan(before,target);
                if(raise) { if(!t.Role.InnerUpdateSkillLevel(skillId,delta)) throw new InvalidOperationException($"InnerUpdateSkillLevel rejected skill={skillId} delta={delta}; no retry"); }
                else { var lp=skill.LevelProperty ?? throw new InvalidOperationException("skill level property missing"); lp.UpdateBaseData(target); }
                int after=skill.LevelBase;
                if(after!=target) throw new InvalidOperationException($"skill={skillId} mismatch target={target} actual={after}; no retry");
                t.Db.MarkDBDirty();
            }
            RefreshGame(t);
            panel.Message(before==target?$"{label}: 이미 Lv{target}":$"{label}: Lv{before} → Lv{target}");
            log.Msg($"role={t.Role.RoleId} skill={skillId} before={before} target={target} max={max} final={skill.LevelFinal}");
        } catch(Exception ex) { log.Error(ex.ToString());panel.Message("적용 오류 · 값과 로그 확인"); }
        finally { applying=false; }
    }
    // Same redraw sequence Tab Characters uses after a skill-point spend.
    private static void RefreshGame(Target t) {
        try {
            t.Ctrl.InitSkillData();
            var list=t.Model.ListRole;
            if(list!=null && t.Index<list.Count) t.View.RefreshSheetCharacter(list[t.Index]);
            t.View.RefreshTipsRoleInfo();t.Model.MarkDirty();
        } catch(Exception ex) { log.Warning("Character sheet redraw failed (value already written): "+ex.Message); }
    }
    internal static string FieldLabel(Field f) {
        string S(Func<string> get,string fallback){try{return Localize(get(),fallback);}catch{return fallback;}}
        return f switch {
            Field.HpCur=>S(()=>UICharacterModel.TexTitleHp,"체력")+" 현재",
            Field.HpMax=>S(()=>UICharacterModel.TexTitleHp,"체력")+" 최대",
            Field.Attack=>S(()=>UICharacterModel.TexTitleAttack,"공격력"),
            Field.Physical=>S(()=>UICharacterModel.Physical,"체격"),
            Field.Craft=>S(()=>UICharacterModel.Craft,"기교"),
            Field.Perceive=>S(()=>UICharacterModel.Perceive,"감지"),
            Field.Charm=>S(()=>UICharacterModel.Charm,"매력"),
            Field.Knowledge=>S(()=>UICharacterModel.Knowledge,"지식"),
            Field.Lucky=>"행운",
            _=>f.ToString() };
    }
}

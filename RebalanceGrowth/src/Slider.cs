using Il2CppClient.Manager;
using Il2CppClient.UILogic.UIHeroLevelUp;
using Il2CppFairyGUI;
using Il2CppGyyx.Template;
using UnityEngine;

namespace Restitutor.RebalanceGrowth;

// 0.3.0 level-up slider (user 2026-09-22): the 1-level / 10-level buttons are replaced by "how much of the
// experience pool to spend". Both native buttons (1 level, 10 levels) are hidden (user); the panel's own button
// confirms. Pool = currency 2 (model.Exp +0x28). Whole levels go through the original continue path: the slider
// writes MaxContinueLevel (+0x5c), NeedExpFive (+0x54, raw exp the role receives via InnerAddExp) and
// NeedExpFiveAfterEffect (+0x58, what AniResult takes from the pool) and the panel button calls the original
// OnClickBtnLevelUpFive (its only 10-level limit is in InitMaxContinueLevel 0xB7DBD0, which this postfix follows).
// Gamepad X still fires the hidden BtnUpFive (original handler, not rewired) = the same level-up.
// A remainder below the next level goes into the role's own partial exp (PlayerRoleData.Exp +0x24) through the
// panel button (shown as "경험치 저장"): the pool pays the chosen amount, the role receives it converted to raw exp
// (floor). The role's stored partial exp is taken off the next level's cost everywhere in this window
// (the original window ignored it). Gamepad A still fires the hidden BtnUp (1 level, partial-aware).
// Work happens only on window events (InitMaxContinueLevel, RefreshNotMax/RefreshMax) and slider input.
public sealed partial class EntryPoint {
    private sealed record LevelPlan(IntPtr Ctrl,int Role,int Level,int Partial,long Pool,List<Rules.Step> Steps,bool Complete,GrowthInput? Growth) {
        public long ToMax=>Rules.ToMax(Steps,Complete);
        public long SliderMax=>Rules.SliderMax(Pool,ToMax);   // right end = max level (or the whole pool)
        public bool Affordable=>Steps.Count>0 && Steps[0].Cost<=Pool;
    }
    private static LevelPlan? plan;
    private static UIHeroLevelUpCtrl? planCtrl;
    private static long choice;
    private static float busyUntil;
    private static LevelSlider? slider;
    private static string? missingRowLogged;

    // UIHeroLevelUpCtrl.InitMaxContinueLevel(level, roleId) postfix: runs on window open, role change and every
    // data sync (AniResult -> MarkDBDirty -> SyncPlayerDataToModel -> InitRoleData/InitSeamenData).
    private static void AfterMaxContinue(UIHeroLevelUpCtrl __instance,int __0,int __1) {
        if(!enabled) return;
        try {
            var model=__instance.Model; var cur=model?.CurRole;
            if(model==null || cur==null || cur.RoleId!=__1 || cur.Level!=__0) return;
            var role=PlayerDataManager.Instance?.Data?.PlayerRole?.FindHoldRole(__1);
            int partial=Math.Max(0,role?.Exp??0);
            long pool=model.Exp; int maxLevel=UIHeroLevelUpModel.MaxLevel;
            var steps=new List<Rules.Step>(); bool complete=true; long sum=0;
            for(int lv=__0;lv<maxLevel;lv++) {
                var row=TemplateManager.GetRoleLevel(lv);
                if(row==null) {
                    complete=false;
                    string k=$"{__1}:{lv}";
                    if(missingRowLogged!=k){ missingRowLogged=k; log.Error($"[level slider] RoleLevel row {lv} missing (role {__1}); the slider stops at level {lv}. Check the 'RoleLevel' line at start-up."); }
                    break;
                }
                int raw=__instance.GetExp(row.exp);
                if(lv==__0) raw=Math.Max(0,raw-partial);
                int cost=__instance.GetExpAfterBuff(lv,raw,__1);
                steps.Add(new(raw,cost)); sum+=cost;
                if(sum>pool) { complete=false; break; }
            }
            var growth=role==null ? null : ReadGrowth(cur,role,out _);
            plan=new(__instance.Pointer,__1,__0,partial,pool,steps,complete,growth);
            planCtrl=__instance; busyUntil=0;
            choice=steps.Count>0 && steps[0].Cost<=pool ? steps[0].Cost : 0;   // default: one level (or nothing)
            ApplyChoice(model,cur);
            slider?.Sync();
        } catch(Exception ex) { Fail("level slider plan",ex); }
    }

    private static void ApplyChoice(UIHeroLevelUpModel model,CurRoleData cur) {
        var p=plan; if(p==null || p.Steps.Count==0) return;
        var s=Rules.Plan(choice,p.Steps);
        if(s.Levels>=1) { model.MaxContinueLevel=s.Levels; model.NeedExpFive=(int)s.RawToRole; model.NeedExpFiveAfterEffect=(int)s.Pay; }
        else {
            // Below one level nothing may level up (user): a count past the max level makes the original
            // OnClickBtnLevelUpFive return at its first check (level + count > MaxLevel, 0xB7EE90) before it
            // changes anything - covers gamepad X, which fires this button. The amount is stored with "경험치 저장".
            model.MaxContinueLevel=UIHeroLevelUpModel.MaxLevel-p.Level+1; model.NeedExpFive=0; model.NeedExpFiveAfterEffect=0;
        }
        // 1-level path (gamepad A -> BtnUp): AniResult gives CurRoleData.NeedExp to InnerAddExp.
        if(p.Partial>0) cur.NeedExp=p.Steps[0].Raw;
    }

    // CurRoleData.GetNeedExpAfterEffect postfix: next-level cost shown by RefreshNotMax, checked for BanTouch and
    // taken by AniResult on the 1-level path. Changed only when the role has stored partial exp.
    private static void AfterNeedExp(CurRoleData __instance,ref int __result) {
        var p=plan;
        if(!enabled || p==null || p.Partial<=0 || p.Steps.Count==0 || __instance.RoleId!=p.Role || __instance.Level!=p.Level) return;
        __result=p.Steps[0].Cost;
    }

    // UIHeroLevelUpView.RefreshNotMax / RefreshMax postfixes: build or show the panel, hide the 1-level button.
    private static void AfterRefreshNotMax(UIHeroLevelUpView __instance) {
        if(!enabled) return;
        try { EnsureSlider(__instance); slider?.Show(true); slider?.Sync(); } catch(Exception ex) { Fail("level slider ui",ex); }
    }
    private static void AfterRefreshMax(UIHeroLevelUpView __instance) {
        if(!enabled) return;
        try { EnsureSlider(__instance); slider?.Show(false); } catch(Exception ex) { Fail("level slider ui",ex); }
    }

    private static void EnsureSlider(UIHeroLevelUpView view) {
        var five=view.BtnUpFive; var up=view.BtnUp;
        if(five==null || five.isDisposed || five.parent==null) return;
        if(slider!=null && slider.Alive && slider.Five==five.Pointer) { if(up!=null && !up.isDisposed) up.visible=false; return; }
        slider?.Dispose();
        slider=new LevelSlider(view,five,up);
    }

    private static bool Ready(out UIHeroLevelUpModel? model) {
        model=null;
        var p=plan; var ctrl=planCtrl;
        if(!enabled || p==null || ctrl==null || ctrl.Pointer!=p.Ctrl || Time.realtimeSinceStartup<busyUntil) return false;
        model=ctrl.Model; var cur=model?.CurRole;
        if(model==null || cur==null || cur.RoleId!=p.Role || cur.Level!=p.Level) return false;
        if(model.ResultAniCount!=0 || model.IsRewardSkill) return false;
        // BanTouch is also set by RefreshNotMax when one level is not affordable; only then may it be ignored.
        if(model.BanTouch && p.Affordable) return false;
        return true;
    }

    private static void OnSlider(long value) {
        var p=plan; if(p==null) return;
        if(!Ready(out var model) || model==null) { slider?.Sync(); return; }   // locked while a level-up runs
        choice=Rules.ClampChoice(value,p.Pool,p.ToMax);   // past the max usable amount -> max usable (user)
        ApplyChoice(model,model.CurRole);
        try { planCtrl?.View?.RefreshNotMax(); } catch(Exception ex) { Fail("level slider refresh",ex); }
        slider?.Sync();
    }

    // Step to the previous / next level boundary.
    private static void OnStep(int dir) {
        var p=plan; if(p==null) return;
        long acc=0,target=dir<0?0:p.SliderMax;
        foreach(var st in p.Steps) {
            long next=acc+st.Cost;
            if(dir<0) { if(next<choice) target=next; else break; }
            else if(next>choice) { target=next; break; }
            acc=next;
        }
        OnSlider(Math.Min(target,p.SliderMax));
    }

    // OnClickBtnLevelUpFive prefix (via LevelUp.BeforeMulti): make sure the original click uses the slider amount.
    private static void ApplyForClick(UIHeroLevelUpCtrl ctrl) {
        var p=plan; var model=ctrl.Model; var cur=model?.CurRole;
        if(!enabled || p==null || model==null || cur==null || ctrl.Pointer!=p.Ctrl || cur.RoleId!=p.Role || cur.Level!=p.Level) return;
        ApplyChoice(model,cur);
    }

    // Keyboard Q / E (gamepad L1 / R1): the level-up window has no L1/R1 action of its own
    // (UIHeroLevelUpCtrl has no OnAction_L1/R1), so the key only moves this slider by one level; never swallowed.
    // Runs through Restitutor.Core's shared input gate (one prefix on InputSystemManager.OnEventCaptureInput).
    private static int keyFrame=-1;
    private static string? keyLogged;
    private static bool OnKey(UnityEngine.InputSystem.InputAction.CallbackContext c) {
        if(!enabled || slider==null || !slider.Visible) return true;
        string n=c.action?.name??"";
        int dir=n is "Action_L1" or "Action_LB" ? -1 : n is "Action_R1" or "Action_RB" ? 1 : 0;
        if(dir==0 || !c.performed || !c.ReadValueAsButton()) return true;
        int f=Time.frameCount*2+(dir>0?1:0);
        if(f==keyFrame) return true;            // one step per key press even if two action names fire
        keyFrame=f;
        if(keyLogged!=n) { keyLogged=n; log.Msg($"[level slider] {n} -> one level {(dir<0?"down":"up")}."); }
        OnStep(dir);
        return true;
    }

    // Panel confirm button (user: the 10-level button is removed; this is the only mouse level-up).
    // Whole levels: the slider values go into the model and the original OnClickBtnLevelUpFive runs as its
    // own button would have run it. Below one level: the amount is stored on the role (no level-up).
    private static void Confirm() {
        try {
            if(!Ready(out var model) || model==null) return;
            var p=plan!; var ctrl=planCtrl!;
            var s=Rules.Plan(choice,p.Steps);
            if(s.Levels>=1) { ApplyChoice(model,model.CurRole); ctrl.OnClickBtnLevelUpFive(); }
            else if(s.PartialRaw>0) SpendPartial(p,s);
        } catch(Exception ex) { Fail("level slider confirm",ex); }
    }

    // Remainder only: same writes as AniResult (InnerAddExp, pool -= amount, MarkDBDirty on both DBs).
    private static void SpendPartial(LevelPlan p,Rules.Spend s) {
        var data=PlayerDataManager.Instance?.Data;
        var roles=data?.PlayerRole; var money=data?.PlayerCurrency;
        var role=roles?.FindHoldRole(p.Role); var pool=money?.FindCurrency(2);
        if(roles==null || money==null || role==null || pool==null) { log.Warning("Partial exp: role or pool not found; nothing spent."); return; }
        if(pool.Amount<s.Pay) { log.Warning($"Partial exp: pool {pool.Amount} < {s.Pay}; nothing spent."); return; }
        int lv=role.Level,before=role.Exp;
        role.InnerAddExp(s.PartialRaw);
        if(role.Level!=lv || role.Exp!=before+s.PartialRaw) {
            log.Error($"Partial exp: role {p.Role} level {lv}->{role.Level}, exp {before}->{role.Exp} after +{s.PartialRaw} (expected level unchanged); pool not charged.");
            roles.MarkDBDirty(); return;
        }
        roles.MarkDBDirty();
        pool.Amount=pool.Amount-s.Pay; money.MarkDBDirty();
        busyUntil=Time.realtimeSinceStartup+3f;
        log.Msg($"Partial exp: role {p.Role} Lv {lv} stored {before:N0} -> {role.Exp:N0} (+{s.PartialRaw:N0}), pool -{s.Pay:N0}.");
    }

    // Panel above the level-up buttons (same slider build as Item Rebuild's QuantityTrack).
    private sealed class LevelSlider : IDisposable {
        public readonly IntPtr Five;
        readonly GButton five; readonly GButton? up; readonly GComponent host,panel;
        GTextField? pointsText;
        readonly GSlider bar; readonly GTextField title,detail; readonly GComponent confirm; GTextField confirmText=null!; GLoader keyQ=null!,keyE=null!;
        readonly GTextField?[] previews=new GTextField?[5];
        readonly Il2CppUIPub.UICom_Prop?[] props=new Il2CppUIPub.UICom_Prop?[5];
        // Callbacks stay referenced for the process lifetime: native listeners may outlive this object.
        static readonly List<Il2CppSystem.Object> keep=new();
        bool dragging,disposed; int touch=-1,max;
        public bool Alive=>!disposed && !panel.isDisposed && !five.isDisposed;

        public LevelSlider(UIHeroLevelUpView view,GButton five,GButton? up) {
            this.five=five; this.up=up; Five=five.Pointer; host=five.parent;
            // Both native buttons are hidden (user: 1-level and 10-level buttons removed); the panel sits on
            // their area, bottom-aligned with them.
            float left=five.x,right=five.x+five.width,bottom=five.y+five.height;
            if(up!=null && !up.isDisposed) { left=Math.Min(left,up.x); right=Math.Max(right,up.x+up.width); bottom=Math.Max(bottom,up.y+up.height); up.visible=false; }
            five.visible=false;
            float width=Math.Max(360,right-left),height=96;
            panel=new GComponent{sortingOrder=int.MaxValue}; panel.SetSize(width,height);
            panel.SetXY(left+(right-left-width)/2,bottom-height); host.AddChild(panel);
            log.Msg($"[level slider] host {host.width:0}x{host.height:0}; BtnUpFive ({five.x:0},{five.y:0},{five.width:0}x{five.height:0}); BtnUp {(up==null?"-":$"({up.x:0},{up.y:0},{up.width:0}x{up.height:0})")}; panel ({panel.x:0},{panel.y:0},{width:0}x{height:0}).");
            Rect(panel,0,0,width,height,new Color(.10f,.12f,.15f,.92f));
            title=Text(12,4,width-24,AlignType.Center,19);
            detail=Text(12,68,width-164,AlignType.Left,17);
            float sx=76,sw=width-152;
            bar=new GSlider{wholeNumbers=true,changeOnClick=false,canDrag=false};
            bar.SetXY(sx,32); bar.SetSize(sw,32); panel.AddChild(bar);
            Rect(bar,0,0,sw,32,new Color(.10f,.12f,.15f,1),true);
            Rect(bar,0,11,sw,10,new Color(.35f,.37f,.4f,1));
            var fill=Rect(bar,0,11,sw,10,new Color(.78f,.68f,.39f,1));
            var grip=Rect(bar,-8,1,16,30,Color.white,true);
            bar._barObjectH=fill; bar._gripObject=grip; bar._barMaxWidth=sw; bar._barMaxWidthDelta=0; bar._barStartX=0;
            // Q / E: one level down / up per press (same key icons as the shop quantity popup,
            // UICommonInputNumView.RefreshUIText: GetInputKeyIcon(5) = L1 / Q, GetInputKeyIcon(6) = R1 / E).
            KeyButton("−",6,32,62,32,5,true,()=>OnStep(-1),out keyQ); KeyButton("+",width-68,32,62,32,6,false,()=>OnStep(1),out keyE);
            confirm=Button("레벨업",width-148,66,140,26,Confirm,out confirmText);
            // Space / gamepad A fire the hidden 1-level button (UIHeroLevelUpCtrl.OnAction_A 0xB80220 -> BtnUp
            // FireClick). User: Space must use the slider amount, and do nothing when nothing is chosen, so that
            // button's click now runs the same confirm as the panel button (its EventBridge callback from
            // UIHeroLevelUpView.OnInit is replaced). Gamepad X (BtnUpFive) keeps the original handler, which also
            // uses the slider values written into the model.
            if(up!=null && !up.isDisposed) { up.onClick.Clear(); Listen(up.onClick,_=>Confirm()); }
            // Next to each ability row (UIPub.UICom_Prop): what the chosen amount does to it.
            var props=new[]{view.Physical,view.Perceive,view.Craft,view.Knowledge,view.Charm};
            for(int i=0;i<5;i++) { this.props[i]=props[i]; previews[i]=Preview(props[i],i==0); }
            pointsText=PointsLabel(view.ComRole);
            Listen(bar.onTouchBegin,Begin); Listen(bar.onTouchMove,Move); Listen(bar.onTouchEnd,End);
            Listen(grip.onTouchBegin,Begin); Listen(grip.onTouchMove,Move); Listen(grip.onTouchEnd,End);
            Sync();
        }
        static GGraph Rect(GComponent h,float x,float y,float w,float ht,Color c,bool touchable=false) {
            var g=new GGraph{touchable=touchable}; g.SetXY(x,y); g.SetSize(w,ht); g.DrawRect(w,ht,0,Color.clear,c); h.AddChild(g); return g;
        }
        GTextField Text(float x,float y,float w,AlignType align,int size) {
            var t=new GTextField{touchable=false,singleLine=true,autoSize=AutoSizeType.Shrink};
            t.textFormat=new TextFormat{size=size,color=Color.white,align=align};
            t.SetXY(x,y); t.SetSize(w,24); panel.AddChild(t); return t;
        }
        GComponent Button(string label,float x,float y,float w,float h,Action act)=>Button(label,x,y,w,h,act,out _);
        void KeyButton(string sign,float x,float y,float w,float h,int key,bool iconLeft,Action act,out GLoader icon) {
            var b=Button(sign,x,y,w,h,act,out var text);
            float iw=h-6;
            // The key icons are Addressables paths (UIInputKey table, e.g. .../keyboard/ui_common_keyboard_q.png);
            // only the game's loader class (Core.NewUISystem.MyGLoader, registered as FairyGUI's loader extension)
            // loads them - a plain GLoader showed nothing (user screenshot).
            icon=UIObjectFactory.NewObject(ObjectType.Loader)?.TryCast<GLoader>() ?? new Il2CppCore.NewUISystem.MyGLoader();
            icon.touchable=false; icon.autoSize=false; icon.fill=FillType.ScaleMatchHeight; icon.align=AlignType.Center; icon.verticalAlign=VertAlignType.Middle;
            icon.SetSize(iw,iw); icon.SetXY(iconLeft?3:w-iw-3,3); b.AddChild(icon);
            text.SetSize(w-iw-6,h); text.SetXY(iconLeft?iw+3:3,0);
            SetIcon(icon,key);
        }
        static void SetIcon(GLoader? icon,int key) {
            if(icon==null || icon.isDisposed) return;
            try { string url=Il2CppClient.Utils.IconUtils.GetInputKeyIcon(key); if(icon.url!=url) icon.url=url; } catch(Exception ex) { Fail("level slider key icon",ex); }
        }
        public bool Visible=>Alive && panel.visible && panel.onStage;
        GComponent Button(string label,float x,float y,float w,float h,Action act,out GTextField text) {
            var b=new GComponent(); b.SetXY(x,y); b.SetSize(w,h); panel.AddChild(b);
            Rect(b,0,0,w,h,new Color(.22f,.22f,.22f,1),true);
            var t=new GTextField{text=label,touchable=false,singleLine=true,autoSize=AutoSizeType.Shrink};
            t.textFormat=new TextFormat{size=18,color=Color.white,align=AlignType.Center}; t.verticalAlign=VertAlignType.Middle;
            t.SetSize(w,h); b.AddChild(t);
            Listen(b.onClick,e=>{ e.StopPropagation(); if(!disposed) act(); });
            text=t;
            return b;
        }
        // Left edge / right edge of the drawn text inside its box (alignment-aware).
        static float TextLeft(GTextField t) {
            var a=t.textFormat.align; float w=Math.Min(t.textWidth,t.width);
            return a==AlignType.Right ? t.x+t.width-w : a==AlignType.Center ? t.x+(t.width-w)/2 : t.x;
        }
        static float TextRight(GTextField t)=>TextLeft(t)+Math.Min(t.textWidth,t.width);

        // User: current value pulled left, right after the ability name; the native "+N" (texAddNum) and this
        // preview follow it tightly, so nothing runs into the next column. Positions are set once per window.
        static GTextField? Preview(Il2CppUIPub.UICom_Prop? prop,bool logIt) {
            var value=prop?.texProp; var host=value?.parent;
            if(prop==null || prop.isDisposed || value==null || host==null) return null;
            var title=prop.TexTitle;
            float oldX=value.x;
            if(title!=null && !title.isDisposed && title.parent?.Pointer==host.Pointer) {
                float gap=Math.Max(6,value.textFormat.size*.35f);
                float dx=TextRight(title)+gap-TextLeft(value);
                if(dx<0) {
                    value.x+=dx;
                    var add0=prop.texAddNum;
                    if(add0!=null && !add0.isDisposed && add0.parent?.Pointer==host.Pointer) add0.x+=dx;
                }
            }
            var t=new GTextField{touchable=false,singleLine=true,autoSize=AutoSizeType.Both};
            var src=value.textFormat;
            t.textFormat=new TextFormat{font=src.font,size=Math.Max(12,(int)(src.size*.72f)),color=new Color(1f,.86f,.45f,1f),align=AlignType.Left};
            t.text=" "; host.AddChild(t);
            t.SetXY(TextRight(value)+6,value.y+(value.height-t.height)/2);
            if(logIt) log.Msg($"[level slider] ability row host {host.width:0}x{host.height:0}; title right {(title==null?-1:TextRight(title)):0}; value x {oldX:0} -> {value.x:0} ({value.width:0}x{value.height:0}); preview at ({t.x:0},{t.y:0}).");
            return t;
        }
        // Next to each preview: after the value, or after the native "+N" while that one shows.
        static void PlacePreview(Il2CppUIPub.UICom_Prop? prop,GTextField t) {
            var value=prop?.texProp; if(prop==null || prop.isDisposed || value==null || value.isDisposed) return;
            float x=TextRight(value)+6;
            var add=prop.texAddNum;
            if(add!=null && !add.isDisposed && add.visible && add.parent?.Pointer==t.parent?.Pointer && !string.IsNullOrEmpty(add.text)) x=Math.Max(x,TextRight(add)+6);
            t.x=x;
        }

        // Skill points (UIPub.UICom_RoleInfo texTitleSkillPoint / texSkillPoint, top right): both pulled left and
        // "+N" (points the chosen amount grants) placed after the number.
        GTextField? PointsLabel(Il2CppUIPub.UICom_RoleInfo? info) {
            var num=info?.texSkillPoint; var cap=info?.texTitleSkillPoint;
            if(info==null || info.isDisposed || num==null || num.isDisposed || num.parent==null) return null;
            float shift=Math.Max(40,num.textFormat.size*2.4f);
            num.x-=shift;
            if(cap!=null && !cap.isDisposed && cap.parent?.Pointer==num.parent.Pointer) cap.x-=shift;
            var t=new GTextField{touchable=false,singleLine=true,autoSize=AutoSizeType.Both};
            var src=num.textFormat;
            t.textFormat=new TextFormat{font=src.font,size=src.size,color=new Color(1f,.86f,.45f,1f),align=AlignType.Left};
            t.text=" "; num.parent.AddChild(t);
            t.SetXY(TextRight(num)+6,num.y+(num.height-t.height)/2);
            log.Msg($"[level slider] skill points ({num.x:0},{num.y:0},{num.width:0}x{num.height:0}) shifted {shift:0}; preview at ({t.x:0},{t.y:0}).");
            return t;
        }
        void Listen(EventListener l,Action<EventContext> fn) { var cb=(EventCallback1)fn; keep.Add(cb); l.Add(cb); }
        void Apply(EventContext e) {
            if(disposed || e.inputEvent==null) return;
            var pt=bar.GlobalToLocal(e.inputEvent.position);
            double f=bar.width<=0?0:Math.Clamp(pt.x/bar.width,0,1);
            OnSlider((long)Math.Floor(f*max+.5));
        }
        void Begin(EventContext e) {
            if(disposed || e.inputEvent==null || e.inputEvent.button!=0) return;
            dragging=true; touch=e.inputEvent.touchId; e.StopPropagation(); e.CaptureTouch(); Apply(e);
        }
        void Move(EventContext e) { if(dragging && e.inputEvent?.touchId==touch) { e.StopPropagation(); Apply(e); } }
        void End(EventContext e) { if(dragging && e.inputEvent?.touchId==touch) { e.StopPropagation(); Apply(e); dragging=false; touch=-1; } }

        public void Show(bool on) {
            if(Alive) { panel.visible=on; five.visible=false; }
            foreach(var t in previews) if(t!=null && !t.isDisposed) t.visible=on;
            if(pointsText!=null && !pointsText.isDisposed) pointsText.visible=on;
            if(up!=null && !up.isDisposed) up.visible=false;
        }

        public void Sync() {
            if(!Alive) return;
            if(up!=null && !up.isDisposed) up.visible=false;
            var p=plan;
            if(p==null || p.Steps.Count==0) {
                title.text="경험치 사용"; detail.text=""; confirm.grayed=true; confirm.touchable=false; five.visible=false;
                foreach(var t in previews) if(t!=null && !t.isDisposed) t.text="";
                if(pointsText!=null && !pointsText.isDisposed) pointsText.text="";
                return;
            }
            max=(int)Math.Min(p.SliderMax,int.MaxValue);
            int v=(int)Math.Clamp(choice,0,max);
            bar.min=0; bar.max=Math.Max(1,max); bar.value=v;
            var grip=bar._gripObject;
            if(grip!=null) grip.SetXY((float)(bar.width*(double)v/Math.Max(1,max))-grip.width/2,1);
            var s=Rules.Plan(choice,p.Steps);
            title.text=$"경험치 사용  {s.Pay:N0} / {p.Pool:N0}";
            string lv=s.Levels>0?$"Lv {p.Level} → {p.Level+s.Levels}":$"Lv {p.Level}";
            string part=s.PartialRaw>0?$" · 인물 경험치 +{s.PartialRaw:N0}":"";
            string stored=p.Partial>0?$" · 저장된 경험치 {p.Partial:N0}":"";
            string cap=p.ToMax>=0?$" · 만렙까지 {p.ToMax:N0}":"";
            detail.text=lv+part+stored+cap;
            five.visible=false;
            SetIcon(keyQ,5); SetIcon(keyE,6);   // keyboard / gamepad icon follows the current device
            bool any=s.Levels>0 || s.PartialRaw>0;
            confirmText.text=s.Levels>0?$"레벨업 +{s.Levels}":s.PartialRaw>0?"경험치 저장":"레벨업";
            confirm.grayed=!any; confirm.touchable=any;
            var g=p.Growth;
            for(int i=0;i<5;i++) {
                var t=previews[i]; if(t==null || t.isDisposed) continue;
                t.text=g==null ? "" : Rules.Preview(g.Value[i],g.Stored[i],g.Rate[i],s.Levels,g.Max);
                PlacePreview(props[i],t);
            }
            if(pointsText!=null && !pointsText.isDisposed) {
                int pts=Rules.PointsInRange(p.Level,s.Levels,UIHeroLevelUpModel.LevelGetSkill);
                pointsText.text=pts>0?$"+{pts}":"";
                var num=pointsText.parent==null ? null : (planCtrl?.View?.ComRole?.texSkillPoint);
                if(num!=null && !num.isDisposed) pointsText.x=TextRight(num)+6;
            }
        }

        public void Dispose() {
            if(disposed) return; disposed=true; dragging=false;
            if(!panel.isDisposed) panel.Dispose();
            foreach(var t in previews) if(t!=null && !t.isDisposed) t.Dispose();
            if(pointsText!=null && !pointsText.isDisposed) pointsText.Dispose();
        }
    }
}

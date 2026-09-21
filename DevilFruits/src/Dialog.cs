using Il2CppClient.UILogic.UICharacter;
using Il2CppCore.NewUISystem;
using Il2CppFairyGUI;
using UnityEngine;

namespace Restitutor.DevilFruits;

// 0.1.1 fruit panel (user 2026-09-21: show the fruit like the skill-book panel, with the owned
// count, placed under Stat Rank's grade tip; explain why it cannot be used). Own GComponent under
// GRoot, sortingOrder 32100 (above Tab Characters' panel container 32000 and Stat Rank's tip
// 31950, below the temporarily raised native UITips 32760). A transparent full-screen catcher
// takes clicks outside the panel and closes it. Panel alpha 0.8 (user). Actions run on the next
// OnUpdate, never inside a FairyGUI event dispatch.
public sealed partial class EntryPoint {
    private const float PanelAlpha=.8f;
    private static GComponent? dialog;
    private static readonly List<(EventListener l,EventCallback1 c)> dialogListeners=new();
    private static int pendingOpen=-1,pendingEat=-1;
    private static bool pendingClose;
    private static int dialogStat=-1;

    private enum FruitState { Usable, NoFruit, Top }

    private static void OpenDialog(int stat) {
        CloseDialog();
        var v=sheetView;
        if(v==null) return;
        var role=SelectedRole(v,out _);
        var hero=role?.GetHeroTemplate();
        var letter=Letter(Props(v)[stat]);
        if(role==null || hero==null || letter==null || letter.isDisposed || !letter.onStage) return;
        Resync(UICharacterCtrl.Data?.PlayerRole,"panel");
        int grade=GetGrade(hero,stat), owned=Owned(stat);
        var state=Rules.IsTop(grade) ? FruitState.Top : owned<=0 ? FruitState.NoFruit : FruitState.Usable;
        dialogStat=stat;

        var root=GRoot.inst;
        var d=new GComponent{name="RestitutorDevilFruitPanel",sortingOrder=32100};
        d.SetSize(root.width,root.height);
        root.AddChild(d);
        dialog=d;

        // Outside catcher: any press outside the panel closes it (left or right button).
        var catcherG=new GGraph();
        catcherG.DrawRect(root.width,root.height,0,Color.clear,Color.clear);
        d.AddChild(catcherG);
        Listen(catcherG.onTouchBegin,e=>{e.StopPropagation();Stage.inst.CancelClick(e.inputEvent.touchId);pendingClose=true;});

        var format=v.SheetCharacter?.texSkillDesc?.textFormat;
        float fs=format!=null && format.size>0 ? format.size : 24;
        float pad=fs*.9f, gap=fs*.5f, slot=fs*3.6f, btnH=fs*2f, btnW=fs*5.5f;

        var panel=new GComponent{alpha=PanelAlpha};
        d.AddChild(panel);
        Listen(panel.onTouchBegin,e=>e.StopPropagation());   // presses inside never close

        var title=Text(panel,$"{Rules.StatNames[stat]} 성장 등급 {GradeLetter(grade)}",fs,format,new Color(1f,.86f,.55f,1));
        var name=Text(panel,Rules.ItemName(stat),fs,format,Color.white);
        string status=state switch {
            FruitState.Top=>$"{Rules.StatNames[stat]} 성장 등급이 이미 S입니다.",
            FruitState.NoFruit=>$"보유한 {Rules.ItemName(stat)}가 없습니다.",
            _=>$"{GradeLetter(grade)} → {GradeLetter(grade-1)}" };
        var statusText=Text(panel,status,fs*.9f,format,state==FruitState.Usable?Color.white:new Color(1f,.55f,.5f,1));

        float rightW=Math.Max(name.width,Math.Max(statusText.width,state==FruitState.Usable?btnW:0));
        float w=pad*2+Math.Max(title.width,slot+gap*2+rightW);
        float bodyH=Math.Max(slot,name.height+gap+statusText.height+(state==FruitState.Usable?gap+btnH:0));
        float h=pad*2+title.height+gap+bodyH;
        panel.SetSize(w,h);

        var bg=new GGraph();
        bg.DrawRect(w,h,2,new Color(.75f,.63f,.38f,1),new Color(.10f,.12f,.13f,1));
        panel.AddChildAt(bg,0);

        title.SetXY(pad,pad);
        float top=pad+title.height+gap;

        // Item slot: native frame (iconDeck) + native icon through the game's own loader
        // (Core.NewUISystem.MyGLoader.LoadExternal -> ResourcesManager.LoadAssetAsync), count x N.
        var item=Il2CppGyyx.Template.TemplateManager.GetItem(Rules.ItemTid(stat));
        var frame=Loader(panel,item?.iconDeck,pad,top,slot);
        var icon=Loader(panel,item?.icon ?? Rules.Icon,pad+slot*.1f,top+slot*.1f,slot*.8f);
        if(owned<=0 && icon!=null) icon.alpha=.35f;
        var count=Text(panel,"x"+owned,fs*.9f,format,owned>0?Color.white:new Color(.7f,.7f,.7f,1));
        count.stroke=1; count.strokeColor=new Color(0,0,0,.8f);
        count.SetXY(pad+slot-count.width-slot*.04f,top+slot-count.height);
        if(frame==null && icon==null){var ph=new GGraph();ph.DrawRect(slot,slot,1,Color.gray,new Color(.2f,.2f,.2f,1));ph.SetXY(pad,top);panel.AddChildAt(ph,1);}

        float x=pad+slot+gap*2;
        name.SetXY(x,top);
        statusText.SetXY(x,top+name.height+gap);
        if(state==FruitState.Usable)
            Button(panel,"사용",x,top+name.height+gap+statusText.height+gap,btnW,btnH,fs,()=>pendingEat=dialogStat);

        // 0.1.1 (user, screenshot 2026-09-21): the letters sit in the right-hand attribute column
        // and Stat Rank's hover tip opens left of the letter, so a panel beside the letter covered
        // the tip. Placement: directly BELOW the Stat Rank tip, right edges aligned (both end at
        // the letter). If there is no room below, directly above the tip. Without a tip on screen,
        // the same anchor Stat Rank uses: left of the letter, vertically centred on it.
        var a=root.GlobalToLocal(letter.LocalToGlobal(new Rect(0,0,letter.width,letter.height)));
        float offset=fs*.6f, stack=fs*.3f;
        var tip=RankTip();
        float px,py;
        if(tip!=null) {
            px=tip.x+tip.width-w;
            py=tip.y+tip.height+stack;
            if(py+h>root.height) py=tip.y-stack-h;
        } else {
            px=a.x-w-offset; if(px<0) px=a.x+a.width+offset;
            py=a.y+a.height/2-h/2;
        }
        panel.SetXY(Math.Clamp(px,0,Math.Max(0,root.width-w)),Math.Clamp(py,0,Math.Max(0,root.height-h)));
    }

    // Stat Rank's hover tip: GRoot child "RestitutorStatRankTip" (Restitutor_Additional_Stat_Rank).
    private static GObject? RankTip() {
        var root=GRoot.inst;
        for(int i=root.numChildren-1;i>=0;i--) {
            var c=root.GetChildAt(i);
            if(c!=null && !c.isDisposed && c.name=="RestitutorStatRankTip" && c.visible && c.onStage) return c;
        }
        return null;
    }

    private static GTextField Text(GComponent host,string s,float size,TextFormat? format,Color color) {
        var t=new GTextField{autoSize=AutoSizeType.Both,singleLine=true,touchable=false};
        var f=t.textFormat;
        f.font=format?.font ?? UIConfig.defaultFont; f.size=(int)size; f.color=color; f.align=AlignType.Left;
        t.textFormat=f; t.text=s; host.AddChild(t);
        return t;
    }
    private static GLoader? Loader(GComponent host,string? url,float x,float y,float size) {
        if(string.IsNullOrEmpty(url) || url=="0") return null;
        try {
            var l=new MyGLoader{touchable=false};
            l.SetSize(size,size); l.fill=FillType.ScaleFree; l.align=AlignType.Center; l.verticalAlign=VertAlignType.Middle;
            l.url=url; l.SetXY(x,y); host.AddChild(l);
            return l;
        } catch(Exception ex) { Fail("icon "+url,ex); return null; }
    }

    private static void Button(GComponent host,string label,float x,float y,float w,float h,float fs,Action act) {
        var g=new GGraph();
        g.DrawRect(w,h,2,new Color(.62f,.84f,1,1),new Color(.16f,.46f,.80f,1));
        g.SetXY(x,y); host.AddChild(g);
        var t=new GTextField{autoSize=AutoSizeType.None,singleLine=true,touchable=false};
        var f=t.textFormat; f.size=(int)fs; f.color=Color.white; f.align=AlignType.Center; t.textFormat=f;
        t.SetSize(w,h); t.align=AlignType.Center; t.verticalAlign=VertAlignType.Middle; t.text=label;
        t.SetXY(x,y); host.AddChild(t);
        Listen(g.onClick,e=>{e.StopPropagation();Stage.inst.CancelClick(e.inputEvent.touchId);act();});
    }

    private static void Listen(EventListener l,Action<EventContext> a) {
        var c=(EventCallback1)(Action<EventContext>)(e=>Guard("panel",()=>a(e)));
        dialogListeners.Add((l,c)); l.Add(c);
    }

    private static void CloseDialog() {
        foreach(var (l,c) in dialogListeners) { try { l.Remove(c); } catch { } }
        dialogListeners.Clear();
        if(dialog!=null && !dialog.isDisposed) dialog.Dispose();
        dialog=null; dialogStat=-1; pendingEat=-1; pendingClose=false;
    }

    // Called from OnUpdate.
    private static void TickDialog() {
        if(pendingOpen>=0){int s=pendingOpen;pendingOpen=-1;Guard("open",()=>OpenDialog(s));}
        if(pendingEat>=0){int s=pendingEat;pendingEat=-1;Guard("use",()=>{CloseDialog();Eat(s);if(sheetView!=null)OpenDialog(s);});}
        if(pendingClose){pendingClose=false;CloseDialog();}
        // Safety: the panel never outlives the character sheet it belongs to.
        if(dialog!=null) {
            var v=sheetView;
            if(v==null || v._UIContent_k__BackingField==null || v._UIContent_k__BackingField.isDisposed || !v._UIContent_k__BackingField.onStage) CloseDialog();
        }
    }
}

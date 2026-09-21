using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using Restitutor.Core;
using Il2CppCharacter;
using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using Il2CppGyyx.Template;
using UnityEngine;

[assembly: MelonInfo(typeof(Restitutor.StatRank.EntryPoint), "Restitutor Additional Stat Rank", "0.1.6", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.StatRank;

// Tab character sheet: growth grade (S~D) after each ability title; hovering the grade shows
// that grade's level-up odds. Read-only: no game state is written.
public sealed class EntryPoint : MelonMod {
    private const string Baseline="50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA";
    private const string LabelName="RestitutorStatRank";
    private const float TipAlpha=.8f; // user: whole window alpha 0.8 (0.4 too faint, 0.7 still too transparent, 2026-09-21)
    private static MelonLogger.Instance log=null!;
    private static bool enabled;
    private static GComponent? tip;
    private static GObject? tipAnchor;
    private static string? lastError;
    // Row data per grade label (keyed by native pointer; labels live under the native sheet).
    private static readonly Dictionary<IntPtr,(string letter,int success,int perfect)> rows=new();

    public override void OnInitializeMelon() {
        log=LoggerInstance;
        // Everything that touches Restitutor.Core sits in Install(): without Restitutor.Core.dll in
        // UserLibs the failure surfaces here and only this mod stays disabled.
        try { Install(); }
        catch(FileNotFoundException ex) when(ex.FileName?.StartsWith("Restitutor.Core",StringComparison.Ordinal)==true)
        { log.Error("Restitutor.Core.dll is missing from UserLibs; Stat Rank stays disabled."); }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install() {
        if(!CoreInfo.Require(log,"0.1.0")) return;
        var hooks=new HookSet(HarmonyInstance,typeof(EntryPoint));
        if(!hooks.InstallAll(log,"Stat Rank install",() => {
            using(var s=File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory,"GameAssembly.dll")))
                if(Convert.ToHexString(SHA256.Create().ComputeHash(s))!=Baseline) throw new InvalidOperationException("GameAssembly baseline differs; Stat Rank disabled.");
            hooks.Hook(typeof(UICharacterView),"RefreshTipsRoleInfo",postfix:nameof(AfterRoleInfo),args:Type.EmptyTypes);
            hooks.Hook(typeof(UICharacterView),"HideHook",postfix:nameof(AfterHide),args:Type.EmptyTypes);
        })) return;
        enabled=true;
        log.Msg("Stat Rank 0.1.6 loaded (Restitutor.Core "+CoreInfo.Version+"); 2 postfixes (UICharacterView.RefreshTipsRoleInfo, HideHook); read-only. Hover by cursor position while the sheet is open; tip alpha 0.8.");
    }
    private static void Fail(Exception ex) { if(lastError!=ex.Message){lastError=ex.Message;log.Error(ex.ToString());} }

    // Hero template growth id per ability, in the order the sheet shows them.
    private static (UICom_Prop prop,int growth)[] Rows(UICharacterView view,Hero hero)=>new[] {
        (view.Physical,hero.phycGrowth),(view.Perceive,hero.percGrowth),(view.Craft,hero.craftGrowth),
        (view.Knowledge,hero.knowGrowth),(view.Charm,hero.chamGrowth) };

    private static void AfterRoleInfo(UICharacterView __instance) {
        if(!enabled) return;
        try {
            HideTip(); hovered=IntPtr.Zero; // sheet refresh: re-evaluate hover on the next frame
            // 0.1.1: native RefreshTipsRoleInfo returns at once when content is null (RVA 0x102B517);
            // get_Physical reads content.roleInfo.physical and throws on either null.
            var content=__instance._UIContent_k__BackingField;
            if(content==null || content.isDisposed || content.roleInfo==null || content.roleInfo.isDisposed) return;
            sheetOpen=true;
            var model=__instance._model;
            Hero? hero=null;
            if(model!=null && model.ListRole!=null && model.RoleIndex>=0 && model.RoleIndex<model.ListRole.Count) {
                var data=model.ListRole[model.RoleIndex];
                if(data!=null && !data.isSeaman) hero=data.HeroData?.GetHeroTemplate();
            }
            foreach(var prop in new[]{__instance.Physical,__instance.Perceive,__instance.Craft,__instance.Knowledge,__instance.Charm})
                if(prop!=null && !prop.isDisposed) { var old=Find(prop); if(old!=null) old.visible=false; }
            if(hero==null) return;
            foreach(var (prop,growth) in Rows(__instance,hero)) {
                if(prop==null || prop.isDisposed) continue;
                var type=TemplateManager.GetRoleGrowthType(growth);
                if(type==null) continue;
                Place(prop,Rules.Letter(type.code,growth),type.successRate,type.perfectRate);
            }
            labels.RemoveAll(l=>l==null || l.isDisposed);
        } catch(Exception ex) { Fail(ex); }
    }
    // HideHook = the character window closing: stop per-frame polling (no native calls while closed).
    private static void AfterHide() {
        try { HideTip(); hovered=IntPtr.Zero; sheetOpen=false; } catch(Exception ex) { Fail(ex); }
    }

    private static GTextField? Find(UICom_Prop prop) {
        var title=prop.TexPropTitle; var parent=title?.parent;
        if(parent==null) return null;
        for(int i=0;i<parent.numChildren;i++) { var c=parent.GetChildAt(i); if(c.name==LabelName) return c.TryCast<GTextField>(); }
        return null;
    }
    private static void Place(UICom_Prop prop,string letter,int success,int perfect) {
        var title=prop.TexPropTitle; var parent=title?.parent;
        if(title==null || parent==null) return;
        var label=Find(prop);
        if(label==null) {
            label=new GTextField{name=LabelName,autoSize=AutoSizeType.Both,singleLine=true,touchable=true};
            parent.AddChild(label);
            var created=label;
            // Listeners are attached once per label; the row data is looked up by pointer at hover time.
            // 0.1.2: UISheetCharacter.btnReturn (full-sheet touchable GGraph) is the hit target over
            // the grades, so onRollOver never fires (0.1.1 diagnostics). Hover is driven from
            // OnUpdate by cursor position instead; btnReturn itself is not changed.
            labels.Add(created);
        }
        var src=title.textFormat; var f=label.textFormat;
        f.font=src.font;f.size=src.size;f.color=src.color;f.bold=src.bold;f.align=AlignType.Left;label.textFormat=f;
        label.text=letter;
        rows[label.Pointer]=(letter,success,perfect);
        // One space after the visible title text.
        float gap=Math.Max(4,src.size*.35f);
        label.SetXY(title.x+title.textWidth+gap,title.y+(title.height-label.height)/2);
        label.visible=true;
    }

    private static void ShowTip(GTextField anchor) {
        if(anchor.isDisposed || !anchor.onStage) return;
        if(!rows.TryGetValue(anchor.Pointer,out var info)) return;
        HideTip();
        var row=Rules.Row(info.letter,info.success,info.perfect);
        var (bg,format)=NativeStyle();
        var root=new GComponent{name="RestitutorStatRankTip",touchable=false,sortingOrder=31950};
        GRoot.inst.AddChild(root);
        // Spacing as a ratio of the tip font size (0.1.1 px values / 20), so it keeps its
        // proportion whatever the UI scale.
        float fs=format!=null && format.size>0 ? format.size : 20;
        float pad=fs*.9f,gap=fs*1.3f,lineGap=fs*.3f,offset=fs*.6f;
        var cells=new GTextField[2,5];
        for(int r=0;r<2;r++) for(int c=0;c<5;c++) {
            var t=new GTextField{autoSize=AutoSizeType.Both,singleLine=true,touchable=false};
            var f=t.textFormat;
            if(format!=null){f.font=format.font;f.size=format.size;f.color=format.color;} else {f.font=UIConfig.defaultFont;f.size=20;f.color=Color.white;}
            f.align=AlignType.Left;t.textFormat=f;t.text=r==0?Rules.Header[c]:row[c];
            cells[r,c]=t;
        }
        float x=pad,h=0;
        float[] widths=new float[5];
        for(int c=0;c<5;c++) widths[c]=Math.Max(cells[0,c].width,cells[1,c].width);
        for(int r=0;r<2;r++) h=Math.Max(h,cells[r,0].height);
        float width=pad*2+widths.Sum()+gap*4, height=pad*2+h*2+lineGap;
        if(bg!=null){bg.touchable=false;root.AddChild(bg);bg.SetSize(width,height);}
        else { var g=new GGraph{touchable=false};g.DrawRect(width,height,1,new Color(0,0,0,1),new Color(.12f,.14f,.15f,.94f));root.AddChild(g); }
        for(int c=0;c<5;c++) {
            for(int r=0;r<2;r++){ var t=cells[r,c];root.AddChild(t);t.SetXY(x,pad+r*(h+lineGap)); }
            x+=widths[c]+gap;
        }
        root.SetSize(width,height);
        root.alpha=TipAlpha;
        // Beside the grade: left of it (the attribute column is on the right edge), else right; clamped to the screen.
        var a=GRoot.inst.GlobalToLocal(anchor.LocalToGlobal(new Rect(0,0,anchor.width,anchor.height)));
        float px=a.x-width-offset; if(px<0) px=a.x+a.width+offset;
        float py=a.y+a.height/2-height/2;
        root.SetXY(Math.Clamp(px,0,Math.Max(0,GRoot.inst.width-width)),Math.Clamp(py,0,Math.Max(0,GRoot.inst.height-height)));
        tip=root;tipAnchor=anchor;
    }
    private static void HideTip() {
        if(tip!=null && !tip.isDisposed) tip.Dispose();
        tip=null;tipAnchor=null;
    }
    // Same look as the native skill tooltip (UISheetCharacter.groupTips): its largest image backdrop
    // (a fresh package instance; the native widget is only read) and texSkillDesc's text format.
    private static (GObject? bg,TextFormat? format) NativeStyle() {
        var view=UICharacterCtrl.Instance?._View_k__BackingField;
        var sheet=view?.SheetCharacter;
        if(sheet==null) return (null,null);
        var format=sheet.texSkillDesc?.textFormat;
        GObject? best=null;float area=0;
        for(int i=0;i<sheet.numChildren;i++) {
            var obj=sheet.GetChildAt(i);
            if(obj==null || obj.isDisposed || obj.TryCast<GImage>()==null || string.IsNullOrEmpty(obj.resourceURL)) continue;
            bool inTips=false;
            for(var g=obj.group;g!=null;g=g.group) if(g.Pointer==sheet.groupTips?.Pointer){inTips=true;break;}
            if(!inTips) continue;
            float s=obj.width*obj.height; if(s>area){area=s;best=obj;}
        }
        GObject? copy=null;
        if(best!=null) { copy=UIPackage.CreateObjectFromURL(best.resourceURL); var img=copy?.TryCast<GImage>(); if(img!=null) img.color=best.TryCast<GImage>()!.color; }
        return (copy,format);
    }
    public override void OnUpdate() {
        HoverProbe();
        // Safety: the tooltip never outlives its grade label (sheet change, disposal).
        if(tip==null) return;
        try { if(tipAnchor==null || tipAnchor.isDisposed || !tipAnchor.onStage || !tipAnchor.visible) HideTip(); } catch(Exception ex) { Fail(ex); HideTip(); }
    }
    // Hover state. 0.1.1 [HoverTrace] (removed in 0.1.4) showed btnReturn as the hit target over
    // every grade with rollOverFired=False; see docs/mods/stat-rank/0.1.4.md.
    private static readonly List<GTextField> labels=new();
    private static bool sheetOpen;
    private static IntPtr hovered;
    private static bool Inside(GObject obj,float x,float y) {
        var l=obj.GlobalToLocal(new Rect(x,y,0,0));
        return l.x>=0 && l.y>=0 && l.x<=obj.width && l.y<=obj.height;
    }
    // Cursor inside a visible grade -> show that grade's tip; leaving -> hide it. Shown only when
    // the topmost hit is btnReturn or the grade itself, so a panel opened over the sheet blocks it.
    private static void HoverProbe() {
        if(!sheetOpen || labels.Count==0) return;
        try {
            float x=Stage.inst.touchPosition.x, y=Stage.inst.touchPosition.y;
            GTextField? over=null;
            foreach(var l in labels) if(l!=null && !l.isDisposed && l.visible && l.onStage && Inside(l,x,y)) { over=l; break; }
            var ptr=over==null?IntPtr.Zero:over.Pointer;
            if(ptr==hovered) return;
            if(hovered!=IntPtr.Zero) HideTip();
            hovered=ptr;
            if(over==null) return;
            var target=Stage.inst.touchTarget?.gOwner;
            var btn=UICharacterCtrl.Instance?._View_k__BackingField?.SheetCharacter?.btnReturn;
            bool allowed=target!=null && (target.Pointer==over.Pointer || (btn!=null && target.Pointer==btn.Pointer));
            if(allowed) ShowTip(over);
        } catch(Exception ex) { Fail(ex); HideTip(); }
    }
    public override void OnDeinitializeMelon() { enabled=false; HideTip(); HarmonyInstance.UnpatchSelf(); }
}

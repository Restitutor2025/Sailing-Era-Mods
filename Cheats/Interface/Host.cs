using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;
using MelonLoader;
using Restitutor.Core;
using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppCore.InputSystem;
using Il2CppFairyGUI;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(Restitutor.Cheats.Interface.Host), "Restitutor Cheats Interface", "1.6.2", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.Cheats.Interface;
public sealed class Host : MelonMod {
    public static PlayerData? Player { get; private set; }
    public static bool Enabled { get; private set; }
    // 1.5.0: false after O. Feature DLLs with always-on hooks must return the original value while false.
    public static bool CheatsOn=>window.CheatsOn;
    private static readonly List<Panel> panels=new();
    private static readonly Dictionary<string,string> errors=new();
    private static GComponent? root;
    private static readonly WindowState window=new();
    private static GGraph? background;
    private static GTextField? arrow;
    private static GComponent? viewport,content,scrollbar;
    private static GGraph? thumb,scrollTrack,headerBox;
    private static GComponent? header,fold,close;
    // 1.4.0: window width follows the widest visible panel. Chrome = 8 left + content + 16 right (scrollbar).
    private const float MinContent=320,Chrome=24,WheelStep=60;
    private static float contentWidth=MinContent;
    private static float WindowWidth=>contentWidth+Chrome;
    private static bool scrolling;
    // 1.5.1: last drawn sizes. DrawRect rebuilds a mesh, so it runs only when the size changes.
    private static float drawnHeaderW=-1,drawnTrackH=-1,drawnThumbH=-1,drawnBgW=-1,drawnBgH=-1;
    private static float scrollY,scrollDragY,scrollStart;
    private static bool dragging;
    private static float dragX,dragY,startX,startY,viewScale=1;
    private static readonly List<EventCallback1> callbacks=new();
    private static MelonLogger.Instance log=null!;
    private static double swallowUntil;
    private static double Now=>System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency;
    public static void SwallowInput()=>swallowUntil=Now+.25;
    public static void Register(Panel panel) {
        if(panels.Any(p=>p.Id==panel.Id)) throw new InvalidOperationException("Duplicate cheat panel: "+panel.Id);
        ClearView(); panels.Add(panel); panels.Sort((a,b)=>a.Order.CompareTo(b.Order));
    }
    public static void Unregister(Panel panel) {
        if(!panels.Contains(panel)) return;
        panel.Reset(); ClearView(); panels.Remove(panel); errors.Remove(panel.Id);
    }
    // 1.6.0: registration through Restitutor.Core. Same lookup as 1.5.1 (inherited members included, name must be
    // unique; checked against the interop metadata for every caller). Feature DLLs keep calling this unchanged.
    public static void Hook(HarmonyLib.Harmony harmony,Type owner,Type target,string method,string? before=null,string? after=null,string? final=null)
        => new HookSet(harmony,owner).Hook(target,method,prefix:before,postfix:after,finalizer:final,declaredOnly:false);
    private static HookSet? hooks;
    public override void OnInitializeMelon() {
        log=LoggerInstance;
        // Everything that touches Restitutor.Core sits in Install(): without Restitutor.Core.dll in
        // UserLibs the failure surfaces here and only the cheats stay disabled.
        try { Install(); }
        catch(FileNotFoundException ex) when(ex.FileName?.StartsWith("Restitutor.Core",StringComparison.Ordinal)==true)
        { log.Error("Restitutor.Core.dll is missing from UserLibs; Cheats Interface stays disabled."); }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install() {
        if(!CoreInfo.Require(log,"0.2.0")) return;
        hooks=new HookSet(HarmonyInstance,typeof(Host));
        if(!hooks.InstallAll(log,"Cheats Interface install",() => {
            var path=Path.Combine(AppContext.BaseDirectory,"GameAssembly.dll");
            if(!File.Exists(path)) path=Path.Combine(Environment.CurrentDirectory,"GameAssembly.dll");
            using var file=File.OpenRead(path);
            if(Convert.ToHexString(SHA256.Create().ComputeHash(file))!="50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA")
                throw new InvalidOperationException("Unsupported GameAssembly baseline");
            // 1.6.2: no save hooks. The current save is read from PlayerDataManager.Data (see SyncSession):
            // new games never run PlayerData.Deserialize, ProcessArchiveInitialize is inlined into its callers
            // (never called), and WorldPortHoldDB.InitHook / other Deserialize calls (handbook reads) cleared or
            // replaced Player, so the window stayed hidden until a later reload.
            // 1.6.0: shared Core input gate instead of an own prefix on InputSystemManager.OnEventCaptureInput.
            hooks!.Input("Cheats Interface",_ => Capture());
            Enabled=true;
        })) return;
        log.Msg("Cheats Interface 1.6.2 loaded (in play only; save = PlayerDataManager.Data, no save hooks; Restitutor.Core "+CoreInfo.Version+", shared input gate; window meshes redrawn only when their size changes). O = all cheats off/on (process lifetime), H = fold/unfold; per-panel width and mouse-wheel scrolling.");
    }
    // 1.6.2: the save the game plays is PlayerDataManager.Data. The game replaces that object on a new game
    // (CreateNewArchive) and on a load (InitArchive); a changed pointer = a new session. This is the only
    // per-frame read of Data (1.6.1 read it twice: InGame and Current); PlayLocation uses Player instead.
    private static IntPtr session;
    private static void SyncSession() {
        var data=PlayerDataManager.Instance?.Data;
        var id=data?.Pointer ?? IntPtr.Zero;
        if(id==session) return;
        ResetSession(); session=id; Player=data;
        log?.Msg(id==IntPtr.Zero ? "Session cleared (no save data)." : "Session: current save data changed; cheat panels reset.");
    }
    private static void ResetSession() {
        Player=null; session=IntPtr.Zero; window.ResetLocation();
        ResetPanels();
        ClearView(); errors.Clear();
    }
    private static void ResetPanels() {
        foreach(var panel in panels.ToArray()) {
            try { panel.Reset(); } catch(Exception ex) { log?.Error(ex.ToString()); }
        }
    }
    private static bool Owns(GObject? value) {
        for(int i=0;value!=null && i<128;i++,value=value.parent) if(value.Pointer==root?.Pointer) return true;
        return false;
    }
    private static bool Capture()=>!Enabled || !window.CheatsOn || (Now>=swallowUntil &&
        (root==null || root.isDisposed || !root.visible || !(dragging||scrolling||Owns(GRoot.inst.focus)||Owns(GRoot.inst.touchTarget))));
    private static void ClearView() {
        foreach(var panel in panels) panel.ClearView();
        if(root!=null && !root.isDisposed) root.Dispose();
        root=viewport=content=scrollbar=null; background=thumb=scrollTrack=headerBox=null;header=fold=close=null;arrow=null;contentWidth=MinContent;dragging=scrolling=false;callbacks.Clear(); swallowUntil=0;
        drawnHeaderW=drawnTrackH=drawnThumbH=drawnBgW=drawnBgH=-1;
    }
    private static void Listen(EventListener e,Action<EventContext> action) {
        EventCallback1 callback=(EventCallback1)action; callbacks.Add(callback); e.Add(callback);
    }
    private static void ReleaseFocus() { if(Owns(GRoot.inst.focus))GRoot.inst.focus=null; }
    private static GComponent Button(GComponent parent,string name,string title,float x,float y,float width,Action action) {
        var b=new GComponent{name=name};b.SetXY(x,y);b.SetSize(width,34);parent.AddChild(b);
        var bg=Theme.Box(b,0,0,width,34,Theme.InkBlue,true);var text=Theme.Label(b,title,0,3,width,20,true);
        Listen(b.onTouchBegin,c=>{text.y=5;SwallowInput();c.StopPropagation();});
        Listen(b.onTouchEnd,c=>{text.y=3;SwallowInput();c.StopPropagation();});
        Listen(b.onRollOut,c=>text.y=3);
        Listen(b.onClick,c=>{SwallowInput();action();c.StopPropagation();});return b;
    }
    private static void BuildChrome() {
        background=Theme.Box(root!,0,0,344,440,Theme.Navy);
        var header=Host.header=new GComponent{name="CheatTitleDrag"};header.SetSize(246,48);root!.AddChild(header);
        headerBox=Theme.Box(header,3,3,243,44,Theme.InkBlue,true);
        Theme.Label(header,"✧  통합 치트",15,10,223,22);
        var fold=Host.fold=Button(root,"CheatMinimize","",250,7,38,()=>{ReleaseFocus();window.Toggle();});
        arrow=Theme.Label(fold,"∨",0,3,38,22,true);
        close=Button(root,"CheatClose","×",295,7,38,()=>{ReleaseFocus();window.Close();root.visible=false;dragging=false;});
        viewport=new GComponent{name="CheatViewport"};viewport.SetXY(8,54);viewport.SetSize(320,340);viewport.SetupOverflow(OverflowType.Hidden);root.AddChild(viewport);
        content=new GComponent{name="CheatContents"};viewport.AddChild(content);
        scrollbar=new GComponent{name="CheatScrollbar"};scrollbar.SetXY(331,54);scrollbar.SetSize(9,340);root.AddChild(scrollbar);
        scrollTrack=Theme.Box(scrollbar,0,0,9,340,Theme.InkBlue,true);
        thumb=Theme.Box(scrollbar,1,0,7,40,Theme.Gold);
        Listen(scrollbar.onTouchBegin,c=>{
            scrolling=true;scrollDragY=root.GlobalToLocal(new Vector2(c.inputEvent.x,c.inputEvent.y)).y;scrollStart=scrollY;
            SwallowInput();c.CaptureTouch();c.StopPropagation();
        });
        Listen(scrollbar.onTouchMove,c=>{
            if(!scrolling)return;
            float distance=root.GlobalToLocal(new Vector2(c.inputEvent.x,c.inputEvent.y)).y-scrollDragY;
            float range=Math.Max(0,content.height-viewport.height);
            scrollY=scrollStart+distance*range/Math.Max(1,viewport.height-thumb.height);LayoutScroll();SwallowInput();c.StopPropagation();
        });
        Listen(scrollbar.onTouchEnd,c=>{scrolling=false;SwallowInput();c.StopPropagation();});
        Listen(header.onTouchBegin,c=>{
            var point=GRoot.inst.GlobalToLocal(new Vector2(c.inputEvent.x,c.inputEvent.y));
            dragging=true;dragX=point.x;dragY=point.y;startX=window.X;startY=window.Y;
            ReleaseFocus();SwallowInput();c.CaptureTouch();c.StopPropagation();
        });
        Listen(header.onTouchMove,c=>{
            if(!dragging)return;
            var point=GRoot.inst.GlobalToLocal(new Vector2(c.inputEvent.x,c.inputEvent.y));
            window.Move(startX+point.x-dragX,startY+point.y-dragY,GRoot.inst.width,GRoot.inst.height,root.width*viewScale,root.height*viewScale);
            root.SetXY(window.X,window.Y);SwallowInput();c.StopPropagation();
        });
        Listen(header.onTouchEnd,c=>{dragging=false;SwallowInput();c.StopPropagation();});
    }
    // Mouse wheel over the window scrolls the panel list (1.3.0 had drag-only scrolling).
    private static void Wheel(EventContext c) {
        float delta=c.inputEvent.mouseWheelDelta;
        if(window.Shows && content!=null && viewport!=null && delta!=0) { scrollY+=Math.Sign(delta)*WheelStep;LayoutScroll(); }
        SwallowInput();c.StopPropagation();
    }
    private static void LayoutChrome() {
        float w=WindowWidth;
        header!.SetSize(w-98,48);
        if(drawnHeaderW!=w) { headerBox!.DrawRect(w-101,44,1,Theme.Gold,Theme.InkBlue); drawnHeaderW=w; }
        fold!.SetXY(w-94,7);close!.SetXY(w-49,7);scrollbar!.SetXY(w-13,54);
    }
    private static void LayoutScroll() {
        float range=Math.Max(0,content!.height-viewport!.height);
        scrollY=Math.Clamp(scrollY,0,range);content.y=-scrollY;
        scrollbar!.visible=window.Shows && range>0;
        scrollbar.SetSize(9,viewport.height);
        if(drawnTrackH!=viewport.height) { scrollTrack!.DrawRect(9,viewport.height,1,Theme.Gold,Theme.InkBlue); drawnTrackH=viewport.height; }
        float h=Math.Min(viewport.height,Math.Max(28,viewport.height*viewport.height/Math.Max(1,content.height)));
        if(drawnThumbH!=h) { thumb!.DrawRect(7,h,0,Theme.Gold,Theme.Gold); drawnThumbH=h; }
        thumb!.y=range==0?0:scrollY/range*(viewport.height-h);
    }
    public override void OnUpdate() {
        if(!Enabled) return;
        try {
            SyncSession();
            bool hDown=Keyboard.current?.hKey.isPressed==true;
            // 1.6.1: the window exists only in play (city, sea or land scene of the loaded save); never on the
            // title screen, while loading, or after returning to the title with a stale save reference.
            bool inGame=PlayLocation.InGame();
            bool canToggle=Application.isFocused && inGame && panels.Count>0 &&
                GRoot.inst.focus?.TryCast<GTextInput>()==null;
            bool oDown=Keyboard.current?.oKey.isPressed==true;
            bool canSwitch=Application.isFocused && inGame && panels.Count>0 && GRoot.inst.focus?.TryCast<GTextInput>()==null;
            if(window.PollO(oDown,canSwitch)) {
                if(!window.CheatsOn) { ReleaseFocus(); ResetPanels(); ClearView(); }
                log.Msg(window.CheatsOn ? "O: cheats ON (window restored; multipliers start at X1)." : "O: cheats OFF (window hidden; every panel reset to original).");
            }
            if(!window.CheatsOn) { if(root!=null) ClearView(); return; }
            if(window.PollH(hDown,canToggle)) { ReleaseFocus(); dragging=scrolling=false; SwallowInput(); }
            if(!inGame || panels.Count==0) { if(root!=null) ClearView(); return; }
            var stage=GRoot.inst;
            if(window.ObserveLocation(PlayLocation.Current())) { ReleaseFocus(); dragging=scrolling=false; }
            if(root==null || root.isDisposed || root.parent?.Pointer!=stage.Pointer) {
                ClearView(); root=new GComponent { name="RestitutorCheats",sortingOrder=25000 }; stage.AddChild(root);
                Listen(root.onTouchBegin,c=>{SwallowInput();c.StopPropagation();});
                Listen(root.onTouchEnd,c=>{SwallowInput();c.StopPropagation();});
                Listen(root.onKeyDown,c=>c.StopPropagation());
                if(root.displayObject!=null) Listen(root.displayObject.onMouseWheel,Wheel);
                BuildChrome();
            }
            float y=0,widest=MinContent;
            foreach(var panel in panels.ToArray()) {
                try {
                    if(panel.Container==null || panel.Container.isDisposed) panel.Mount(content!);
                    panel.Refresh();
                    bool eligible=panel.Visible;
                    bool shown=window.Shows && eligible;
                    if(!shown && panel.Owns(stage.focus))stage.focus=null;
                    panel.Container!.visible=shown;panel.Container.SetXY(0,y);
                    if(eligible) { y+=panel.Height+6;widest=Math.Max(widest,panel.Width); }
                    errors.Remove(panel.Id);
                } catch(Exception ex) {
                    try { panel.Reset(); } catch(Exception reset) { log.Error(reset.ToString()); }
                    panel.ClearView();
                    if(!errors.TryGetValue(panel.Id,out var old)||old!=ex.Message) { errors[panel.Id]=ex.Message; log.Error(panel.Id+": "+ex); }
                }
            }
            contentWidth=window.Shows?widest:MinContent;float width=WindowWidth;LayoutChrome();
            viewScale=Math.Max(.1f,Math.Min(1f,(stage.width-24)/width));
            float viewHeight=Math.Min(Math.Max(1,y),Math.Max(48,(stage.height-24)/viewScale-60));
            content!.SetSize(contentWidth,y);viewport!.SetSize(contentWidth,viewHeight);viewport.visible=window.Shows;
            float height=window.Minimized?48:viewHeight+60;
            root.SetSize(width,height);root.touchable=!window.Closed;root.visible=!window.Closed;
            arrow!.text=window.Minimized?"∧":"∨";LayoutScroll();
            if(drawnBgW!=width||drawnBgH!=height) { background!.DrawRect(width,height,2,Theme.Gold,Theme.Navy); drawnBgW=width; drawnBgH=height; }
            root.SetScale(viewScale,viewScale);window.Place(stage.width,stage.height,width*viewScale,height*viewScale);root.SetXY(window.X,window.Y);
        } catch(Exception ex) {
            // A transient UI error must not lose the current player until reload.
            foreach(var panel in panels.ToArray()) {
                try { panel.Reset(); } catch(Exception reset) { log.Error(reset.ToString()); }
            }
            ClearView(); log.Error(ex.ToString());
        }
    }
    public override void OnDeinitializeMelon() { Enabled=false; if(hooks!=null) hooks.RemoveAll(); else HarmonyInstance.UnpatchSelf(); ResetSession(); }
}


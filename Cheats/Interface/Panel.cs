using Il2CppFairyGUI;
using UnityEngine;
namespace Restitutor.Cheats.Interface;

// Feature DLLs own their contents; the host only arranges and routes input.
public abstract class Panel {
    protected static Color Surface=>Theme.InkBlue;
    protected static Color Foreground=>Theme.Cream;
    protected static Color Accent=>new(.43f,.32f,.14f,1);
    protected static Color Border=>Theme.Gold;
    public abstract string Id { get; }
    public abstract int Order { get; }
    public abstract float Height { get; }
    // 1.4.0: content width. The window widens to the widest visible panel (min 320).
    public virtual float Width => 320;
    public virtual bool Visible => true;
    public GComponent? Container { get; private set; }
    public bool Active { get; private set; }
    private readonly List<EventCallback1> callbacks = new();
    public abstract void Build();
    public abstract void Refresh();
    public abstract void Reset();
    protected virtual void ClearReferences() {}
    internal void Mount(GComponent root) {
        ClearView();
        float w=Math.Max(320,Width);
        Container=new GComponent { name=Id }; Container.SetSize(w,Height); root.AddChild(Container);
        Rect(Container,0,0,w,Height,Theme.Navy);
        Theme.Box(Container,1,1,w-2,35,Theme.InkBlue);
        Build();
    }
    internal void ClearView() {
        if (Container != null && !Container.isDisposed) {
            if (Owns(GRoot.inst.focus)) GRoot.inst.focus=null;
            Container.Dispose();
        }
        Container=null; Active=false; activeApplied=IntPtr.Zero; callbacks.Clear(); ClearReferences();
    }
    public bool Owns(GObject? value) {
        for(int i=0;value!=null && i<128;i++,value=value.parent)
            if(value.Pointer==Container?.Pointer) return true;
        return false;
    }
    private IntPtr activeApplied; // 1.5.1: container the current Active state was applied to
    protected void SetActive(bool value) {
        if(Active==value && Container!=null && activeApplied==Container.Pointer) return;
        Active=value;
        if(Container==null) return;
        activeApplied=Container.Pointer;
        Container.alpha=value ? 1f : .5f; Container.touchable=value;
        if(!value && Owns(GRoot.inst.focus)) GRoot.inst.focus=null;
    }
    protected void Listen(EventListener listener, Action<EventContext> action) {
        EventCallback1 callback=(EventCallback1)action; callbacks.Add(callback); listener.Add(callback);
    }
    protected void Consume(EventContext context) { Host.SwallowInput(); context.StopPropagation(); }
    protected GGraph ButtonFace(GComponent button,string title,float width) {
        var background=Theme.Box(button,0,0,width,34,Surface,true);
        var label=Theme.Label(button,title,0,3,width,19,true);
        Listen(button.onTouchBegin,c=>{label.y=5;Consume(c);});
        Listen(button.onTouchEnd,c=>{label.y=3;Consume(c);});
        Listen(button.onRollOut,c=>label.y=3);
        return background;
    }
    protected GTextField Text(string value,float y,int size=18,GComponent? parent=null) {
        var t=new GTextField { touchable=false,autoSize=AutoSizeType.None,singleLine=false };
        var f=t.textFormat; f.font=UIConfig.defaultFont; f.size=size; f.color=Theme.Cream; t.textFormat=f;
        t.SetXY(12,y); t.SetSize(296,48); t.text=value; (parent ?? Container)!.AddChild(t); return t;
    }
    protected static void Rect(GComponent parent,float x,float y,float w,float h,Color color) {
        var g=new GGraph(); g.SetXY(x,y); g.DrawRect(w,h,1,Theme.Gold,color); parent.AddChild(g);
    }
}

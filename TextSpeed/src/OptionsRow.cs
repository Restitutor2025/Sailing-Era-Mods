using Il2CppClient.UILogic.UISystemSetting;
using Il2CppFairyGUI;
using Il2CppUISystem;
using UnityEngine;

namespace Restitutor.TextSpeed;

internal static class OptionsRow
{
    private const string Anchor="RunningInBackground";
    private const string Name="__Restitutor_TextSpeed_v1";
    private sealed record Child(GObject Item,float X,float Y);
    private sealed class Layout
    {
        public UIButtonSettingItemOption Row=null!;
        public float Height;
        public Child[] Children=Array.Empty<Child>();
        public GComponent Panel=null!;
        public GGraph[] SlowTick=Array.Empty<GGraph>(), FastTick=Array.Empty<GGraph>();
        public GTextField Status=null!;
        public readonly List<EventCallback1> Handlers=new();
    }
    private static readonly Dictionary<IntPtr,Layout> layouts=new();
    private sealed record Deferred(UISettingView View,int Index,GObject Row);
    private static readonly Dictionary<IntPtr,Deferred> deferred=new();
    private static bool warned;
    private static readonly HashSet<string> diagnostics=new();
    private static void TraceOnce(string message)
    {
        if(diagnostics.Count<40 && diagnostics.Add(message))TextSpeedModule.Log.Msg("Options: "+message);
    }
    public static void BeforeRender(GObject __1)
    {
        deferred.Remove(__1.Pointer);
        if(!layouts.Remove(__1.Pointer,out var old))return;
        if(old.Row.isDisposed)return;
        try
        {
            if(old.Panel!=null)old.Row.RemoveChild(old.Panel,true);
            old.Row.SetSize(old.Row.width,old.Height,true);
            // Restore the unmodified row before letting its pooled renderer run again.
            foreach(var c in old.Children)c.Item.SetXY(c.X,c.Y);
        }
        catch(Exception ex){TextSpeedModule.Log.Error("Option row restoration: "+ex.Message);}
    }
    public static void AfterRender(UISettingView __instance,int __0,GObject __1)
    {
        try
        {
            var row=__1.TryCast<UIButtonSettingItemOption>();
            TraceOnce("item renderer reached");
            if(row==null){TraceOnce("Unexpected item type: "+__1.GetIl2CppType().FullName);return;}
            var data=row.parent?.parent?.data;
            if(data==null){TraceOnce("Missing parent group data");return;}
            // The native renderer reads a boxed System.Int32 here, not a text value.
            // The generated Int32 initializer registers NativeClassPtr for System.Int32.
            var group=data.Unbox<int>();
            var keys=__instance._model.GetSettingGroupsByType(__instance._model.viewPage);
            if(keys==null || !keys.ContainsKey(group+1)){TraceOnce("Missing setting group: "+group);return;}
            var list=keys[group+1];
            if(__0>=0 && __0<list.Count)TraceOnce("item key="+list[__0]);
            if(__0<0||__0>=list.Count||list[__0]!=Anchor)return;
            PruneLayouts(); // 0.1.5: pruned when a row renders, not every frame
            if(layouts.ContainsKey(row.Pointer))return;
            var height=row.height;
            if(height<=0 || row.width<=0)
            {
                deferred[row.Pointer]=new(__instance,__0,row);
                TraceOnce("Anchor layout pending; waiting for nonzero dimensions.");
                return;
            }
            deferred.Remove(row.Pointer);
            var l=new Layout{Row=row,Height=height};
            var children=new List<Child>();
            for(int i=0;i<row.numChildren;i++){var c=row.GetChildAt(i);children.Add(new(c,c.x,c.y));}
            l.Children=children.ToArray();
            layouts.Add(row.Pointer,l);
            row.SetSize(row.width,height*2,true);
            // Setting rows use gears. Shift every native child together, retaining original
            // coordinates for the next renderer invocation rather than accumulating offsets.
            foreach(var c in l.Children)c.Item.SetXY(c.X,c.Y+height);
            var panel=new GComponent{name=Name};l.Panel=panel;panel.SetSize(row.width,height);
            row.AddChild(panel);
            var source=row.txtTitle;
            var title=Text("텍스트 출력",source,source.x,0,row.width*.32f,height);
            panel.AddChild(title);l.Status=title;
            l.SlowTick=Choice(l,panel,"느리게",TextMode.Slow,row.width*.36f,height,source);
            l.FastTick=Choice(l,panel,"빠르게",TextMode.Instant,row.width*.56f,height,source);
            EventCallback1 stop=(Action<EventContext>)(e=>e.StopPropagation());
            l.Handlers.Add(stop);panel.onClick.Add(stop);
            Sync(l);
            row.SetBoundsChangedFlag();
            TextSpeedModule.Log.Msg("Text-speed choices inserted immediately above RunningInBackground.");
        }
        catch(Exception ex)
        {
            BeforeRender(__1);
            if(!warned){warned=true;TextSpeedModule.Log.Error("Cannot insert text-speed options: "+ex);}
        }
    }
    public static void LateUpdate()
    {
        if(deferred.Count==0)return; // 0.1.5: nothing waits for a layout; no per-frame scan
        try { RetryLayout(); }
        catch(Exception ex)
        {
            deferred.Clear();
            if(!warned){warned=true;TextSpeedModule.Log.Error("Option layout retry failed: "+ex);}
        }
    }
    private static void RetryLayout()
    {
        PruneLayouts();
        foreach(var item in deferred.Values.ToArray())
        {
            if(item.Row.isDisposed || item.Row.parent==null){deferred.Remove(item.Row.Pointer);continue;}
            if(item.Row.width<=0 || item.Row.height<=0)continue;
            deferred.Remove(item.Row.Pointer);
            AfterRender(item.View,item.Index,item.Row);
            if(layouts.ContainsKey(item.Row.Pointer))
            {
                var list=item.Row.parent.TryCast<GList>();
                list?.ResizeToFit();
                item.Row.parent.parent?.SetBoundsChangedFlag();
                item.Row.parent.parent?.parent?.SetBoundsChangedFlag();
            }
        }
    }
    private static readonly List<IntPtr> stale=new();
    private static void PruneLayouts()
    {
        if(layouts.Count==0)return;
        stale.Clear();
        foreach(var p in layouts) if(p.Value.Row.isDisposed) stale.Add(p.Key);
        foreach(var k in stale) layouts.Remove(k);
    }
    private static GTextField Text(string value,GTextField source,float x,float y,float w,float h)
    {
        var t=new GTextField();var format=new TextFormat();format.CopyFrom(source.textFormat);
        format.align=AlignType.Left;
        t.textFormat=format;t.autoSize=AutoSizeType.None;t.text=value;t.touchable=false;t.verticalAlign=VertAlignType.Middle;
        t.SetXY(x,y);t.SetSize(w,h);return t;
    }
    private static GGraph[] Choice(Layout l,GComponent panel,string label,TextMode mode,float x,float height,GTextField source)
    {
        var button=new GComponent();button.SetXY(x,0);button.SetSize(l.Row.width*.18f,height);panel.AddChild(button);
        var size=Math.Min(26f,height*.42f);var y=(height-size)/2;
        var hit=new GGraph();hit.DrawRect(button.width,height,0,Color.clear,new Color(0,0,0,0.001f));button.AddChild(hit);
        var box=new GGraph();box.DrawRect(size,size,2,Color.white,Color.clear);box.SetXY(0,y);box.touchable=false;button.AddChild(box);
        var a=new GGraph();a.DrawRect(size*.34f,3,0,Color.clear,new Color(.5f,.85f,.86f));a.SetXY(size*.18f,y+size*.5f);a.rotation=45;a.touchable=false;button.AddChild(a);
        var b=new GGraph();b.DrawRect(size*.65f,3,0,Color.clear,new Color(.5f,.85f,.86f));b.SetXY(size*.37f,y+size*.72f);b.rotation=-48;b.touchable=false;button.AddChild(b);
        button.AddChild(Text(label,source,size+12,0,button.width-size-12,height));
        EventCallback1 click=(Action<EventContext>)(e=>
        {
            e.StopPropagation();
            if(!TextSpeedModule.Store.SelectFromOptions(mode))TextSpeedModule.Log.Error("Preference unchanged: "+TextSpeedModule.Store.Error);
            foreach(var item in layouts.Values)if(!item.Row.isDisposed)Sync(item);
            TextSpeedModule.Log.Msg("User option selection: "+TextSpeedModule.Store.Current);
        });
        l.Handlers.Add(click);button.onClick.Add(click);
        return new[]{a,b};
    }
    private static void Sync(Layout l)
    {
        var store=TextSpeedModule.Store;
        foreach(var g in l.SlowTick)g.visible=store.Current==TextMode.Slow;
        foreach(var g in l.FastTick)g.visible=store.Current==TextMode.Instant;
        l.Status.text=store.Ready?"텍스트 출력":"텍스트 출력 (저장 오류)";
    }
}

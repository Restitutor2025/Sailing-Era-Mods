using System.Text;
using Il2CppFairyGUI;
using UnityEngine;

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint
{
    // Read-only, bounded capture. Never change visibility, focus or sibling order.
    static float traceUntil;
    static int traceSequence, traceRows;
    static string traceLast="";
    static GObject? traceTipContent;
    static partial void ClearUiTrace()
    { traceUntil=0;traceRows=0;traceLast="";traceTipContent=null; }
    static partial void TickUiTrace() { TraceUi("frame"); }
    static partial void TraceUi(string reason, bool begin)
    {
        try
        {
            if(begin){traceSequence++;traceRows=0;traceLast="";traceUntil=Time.unscaledTime+15;}
            if(traceUntil<=0)return;
            if(Time.unscaledTime>traceUntil){ClearUiTrace();return;}
            if(traceRows>=180)return;
            var root=GRoot.inst;
            if(tipOwner?._View_k__BackingField?._UIContent_k__BackingField is GObject tip)traceTipContent=tip;
            var s=new StringBuilder();
            s.Append("active=").Append(pointOwner?.IsInputActive).Append(" playing=").Append(fastTip?.playing)
             .Append(" originalOrder=").Append(originalTipOrder).Append(" books=").Append(books!=null);
            books?.AppendPanels(s);
            AppendUi(s,"footer",pointFooter);AppendUi(s,"borrowed-layer",tipLayer);
            GObject? ancestor=traceTipContent;
            for(int depth=0;ancestor!=null&&depth<12;depth++)
            {AppendUi(s,"tip-ancestor"+depth,ancestor);if(ancestor.isDisposed)break;ancestor=ancestor.parent;}
            // Root sibling indices reveal occlusion even when visible remains true.
            for(int i=0;i<Math.Min(root.numChildren,64);i++)AppendUi(s,"root["+i+"]",root.GetChildAt(i));
            string state=s.ToString();
            if(reason=="frame"&&state==traceLast)return;
            traceLast=state;traceRows++;
            host?.LoggerInstance.Msg($"[CharactersUITrace] seq={traceSequence} frame={Time.frameCount} time={Time.unscaledTime:F3} event={reason} {state}");
            if(traceRows==180)host?.LoggerInstance.Msg("[CharactersUITrace] Capture limit reached; next point starts a new capture.");
        }
        catch(Exception ex)
        { ClearUiTrace();host?.LoggerInstance.Warning("Characters UI trace stopped: "+ex.Message); }
    }
    private sealed partial class BookPopup
    {
        internal void AppendPanels(StringBuilder s)
        { AppendUi(s,"overlay",overlay);AppendUi(s,"info",infoPane);AppendUi(s,"panel",panel); }
    }
    static void AppendUi(StringBuilder s,string label,GObject? o)
    {
        s.Append(" | ").Append(label).Append('=');
        if(o==null){s.Append("null");return;}
        s.Append(o.Pointer.ToInt64().ToString("X"));
        if(o.isDisposed){s.Append(" disposed");return;}
        s.Append('(').Append(o.name).Append(") parent=").Append(o.parent?.Pointer.ToInt64().ToString("X"))
         .Append(" visible=").Append(o.visible).Append(" alpha=").Append(o.alpha.ToString("F2"))
         .Append(" order=").Append(o.sortingOrder).Append(" xy=").Append(o.x.ToString("F1")).Append(',').Append(o.y.ToString("F1"))
         .Append(" size=").Append(o.width.ToString("F1")).Append(',').Append(o.height.ToString("F1"));
        var d=o.displayObject;
        if(d==null)s.Append(" display=null");
        else s.Append(" displayVisible=").Append(d.visible).Append(" displayAlpha=").Append(d.alpha.ToString("F2"));
    }
}

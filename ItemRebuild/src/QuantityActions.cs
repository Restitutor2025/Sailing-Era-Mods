using Il2CppFairyGUI;
using UnityEngine;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    sealed class QuantityActions : IDisposable
    {
        public readonly GComponent Panel=new(){sortingOrder=int.MaxValue};
        public readonly GComponent Cancel=new(),Apply=new();
        readonly GComponent parent;
        readonly GObject original;
        readonly bool visible;
        readonly EventCallback1 removed;
        readonly List<EventCallback1> callbacks=new();
        bool disposed;
        public QuantityActions(GComponent parent,GObject original,Func<bool> active,Action cancel,Action apply)
        {
            this.parent=parent;this.original=original;visible=original.visible;
            parent.AddChild(Panel);
            void Button(GComponent button,string caption,Action action)
            {
                Panel.AddChild(button);
                var bg=new GGraph();button.AddChild(bg);
                var text=new GTextField{touchable=false,singleLine=true,autoSize=AutoSizeType.Shrink,text=caption};button.AddChild(text);
                var cb=(EventCallback1)(Action<EventContext>)(e=>{e.StopPropagation();if(!disposed&&active()) action();});
                button.onClick.Add(cb);callbacks.Add(cb);
            }
            Button(Cancel,"(ESC) 취소",cancel);Button(Apply,"(Space) 적용",apply);
            removed=(EventCallback1)(Action<EventContext>)(_=>Dispose());parent.onRemovedFromStage.Add(removed);
            Layout();
        }
        public void Layout()
        {
            if(disposed) return;
            original.visible=false;
            // Symmetric pair: 45% button / 10% gap / 45% button, centered on the native button.
            float width=Math.Min(parent.width*.84f,original.width*2.4f),height=original.height;
            Panel.SetXY(original.x+original.width/2-width/2,original.y);Panel.SetSize(width,height);
            void Size(GComponent b,float x)
            {
                b.SetXY(x,0);b.SetSize(width*.45f,height);
                var bg=b.GetChildAt(0).TryCast<GGraph>()!;bg.SetSize(b.width,height);
                bg.DrawRect(b.width,height,1,new Color(.78f,.68f,.39f,1),new Color(.19f,.19f,.19f,1));
                var label=b.GetChildAt(1).TryCast<GTextField>()!;
                label.textFormat=new TextFormat{size=Math.Max(1,(int)(height*.48f)),color=Color.white,align=AlignType.Center};
                label.verticalAlign=VertAlignType.Middle;label.SetSize(b.width,height);
            }
            Size(Cancel,0);Size(Apply,width*.55f);
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            if(!parent.isDisposed)parent.onRemovedFromStage.Remove(removed);
            if(!original.isDisposed)original.visible=visible;
            Panel.Dispose();callbacks.Clear();
        }
    }
}

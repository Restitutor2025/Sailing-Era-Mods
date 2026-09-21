using Il2CppFairyGUI;
using UnityEngine;
namespace Restitutor.ItemRebuild;
public sealed partial class EntryPoint
{
    internal static int NearestQuantity(double x,double width,int maximum)
        => maximum<=0||width<=0||double.IsNaN(x)?0:(int)Math.Floor(Math.Clamp(x/width,0,1)*maximum+.5);
    sealed class QuantityTrack : IDisposable
    {
        public readonly GComponent Parent,Panel;
        public readonly GSlider Slider;
        public GComponent? MinButton,MaxButton;
        public readonly List<EventCallback1> Callbacks=new();
        readonly GTextField title,maxLabel;
        readonly string caption;
        readonly Action<int> changed;
        readonly Func<bool> active;
        readonly EventCallback1 removed;
        bool dragging,disposed;
        int maximum,touch=-1;
        public QuantityTrack(GComponent parent,string caption,Func<bool> active,Action<int> changed,bool compact=false,bool endpoints=false)
        {
            Parent=parent;this.caption=caption;this.active=active;this.changed=changed;
            float width=Math.Max(320,parent.width);
            Panel=new GComponent {sortingOrder=int.MaxValue};Panel.SetSize(width,108);
            Panel.SetXY(0,0);parent.AddChild(Panel);
            GGraph Rect(GComponent host,float x,float y,float w,float h,Color color,bool touchable=false)
            {
                var g=new GGraph {touchable=touchable};g.SetXY(x,y);g.SetSize(w,h);
                g.DrawRect(w,h,0,Color.clear,color);host.AddChild(g);return g;
            }
            var background=Rect(Panel,0,0,width,108,new Color(.10f,.12f,.15f,.97f));
            GTextField Text(float x,float y,float w,AlignType align)
            {
                var t=new GTextField {touchable=false,singleLine=true,autoSize=AutoSizeType.Shrink};
                t.textFormat=new TextFormat {size=20,color=Color.white,align=align};
                t.SetXY(x,y);t.SetSize(w,26);Panel.AddChild(t);return t;
            }
            title=Text(16,8,width-32,AlignType.Center);var minLabel=Text(16,78,80,AlignType.Left);minLabel.text="0";
            maxLabel=Text(width-116,78,100,AlignType.Right);
            Slider=new GSlider {wholeNumbers=true,changeOnClick=false,canDrag=false};
            Slider.SetXY(24,38);Slider.SetSize(width-48,36);Panel.AddChild(Slider);
            Rect(Slider,0,0,Slider.width,36,new Color(.10f,.12f,.15f,1),true);
            Rect(Slider,0,13,Slider.width,10,new Color(.35f,.37f,.4f,1));
            var bar=Rect(Slider,0,13,Slider.width,10,new Color(.78f,.68f,.39f,1));
            var grip=Rect(Slider,-9,1,18,34,Color.white,true);
            Slider._barObjectH=bar;Slider._gripObject=grip;
            Slider._barMaxWidth=bar.width;Slider._barMaxWidthDelta=0;Slider._barStartX=0;
            if(compact)
            {
                background.visible=title.visible=minLabel.visible=maxLabel.visible=false;
                Slider.SetXY(0,0);Panel.SetSize(Slider.width,Slider.height);
            }
            if(endpoints)
            {
                float w=Panel.width,h=Slider.height;
                Slider.SetXY(w*.18f,Slider.y);Slider.scaleX=.64f;
                GComponent Endpoint(string label,float x,bool max)
                {
                    var b=new GComponent();b.SetXY(x,Slider.y);b.SetSize(w*.16f,h);Panel.AddChild(b);
                    Rect(b,0,0,b.width,h,new Color(.19f,.19f,.19f,1),true);
                    var text=new GTextField {text=label,touchable=false,singleLine=true,autoSize=AutoSizeType.Shrink};
                    text.textFormat=new TextFormat{size=20,color=Color.white,align=AlignType.Center};text.verticalAlign=VertAlignType.Middle;
                    text.SetSize(b.width,h);b.AddChild(text);
                    Listen(b.onClick,e=>{e.StopPropagation();if(!disposed&&active()){int n=max?maximum:0;changed(n);Sync(n,maximum);}});
                    return b;
                }
                MinButton=Endpoint("Min",0,false);MaxButton=Endpoint("Max",w*.84f,true);
            }
            Listen(Slider.onTouchBegin,Begin);Listen(Slider.onTouchMove,Move);Listen(Slider.onTouchEnd,End);
            Listen(grip.onTouchBegin,Begin);Listen(grip.onTouchMove,Move);Listen(grip.onTouchEnd,End);
            Listen(Slider.onChanged,_=> {if(!disposed&&active()) changed((int)Math.Clamp(Math.Floor(Slider.value+.5),0,maximum));});
            removed=(EventCallback1)(Action<EventContext>)(_=>Dispose());
            parent.onRemovedFromStage.Add(removed);Callbacks.Add(removed);
        }
        // Keep the interactive surface inside the existing native dialog, never below UIContent.
        public void Place(float x,float y,float width,float height)
        {Panel.SetXY(x,y);Panel.scaleX=Math.Max(1,width)/Panel.width;Panel.scaleY=Math.Max(1,height)/Panel.height;}
        void Listen(EventListener listener,Action<EventContext> fn)
        {var cb=(EventCallback1)fn;Callbacks.Add(cb);listener.Add(cb);}
        void Apply(EventContext e)
        {
            if(disposed||!active()||e.inputEvent==null) return;
            var point=Slider.GlobalToLocal(e.inputEvent.position);
            int n=NearestQuantity(point.x,Slider.width,maximum);
            changed(n);Sync(n,maximum);
        }
        void Begin(EventContext e)
        {
            if(disposed||!active()||e.inputEvent==null||e.inputEvent.button!=0) return;
            dragging=true;touch=e.inputEvent.touchId;e.StopPropagation();e.CaptureTouch();Apply(e);
        }
        void Move(EventContext e) {if(dragging&&e.inputEvent?.touchId==touch) {e.StopPropagation();Apply(e);}}
        void End(EventContext e) {if(dragging&&e.inputEvent?.touchId==touch) {e.StopPropagation();Apply(e);dragging=false;touch=-1;}}
        public void Sync(long value,int max)
        {
            if(disposed) return;
            maximum=Math.Max(0,max);int n=(int)Math.Clamp(value,0,maximum);
            Slider.min=0;Slider.max=maximum;Slider.value=n;
            var grip=Slider._gripObject;
            if(grip!=null) grip.SetXY((float)(Slider.width*(double)n/Math.Max(1,maximum))-grip.width/2,1);
            title.text=$"{caption}  {n} / {maximum}";maxLabel.text=maximum.ToString();
        }
        public void Dispose()
        {
            if(disposed) return;disposed=true;dragging=false;
            if(!Parent.isDisposed) Parent.onRemovedFromStage.Remove(removed);
            if(!Panel.isDisposed) Panel.Dispose();Callbacks.Clear();
        }
    }
}

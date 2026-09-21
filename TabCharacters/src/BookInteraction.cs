using Il2CppFairyGUI;
using UnityEngine;
namespace Restitutor.TabCharacters;
public sealed partial class EntryPoint
{
    private sealed partial class BookPopup
    {
        GGraph? outsideDismiss;
        readonly List<GGraph> pointGradient=new();
        bool pointHovered,pointArmed,outsideArmed;
        float outsidePressX,outsidePressY;
        int pointRequestFrame=-1;
        internal void DrainPointRequest()
        {
            if(pointRequestFrame<0||Time.frameCount<=pointRequestFrame)return;
            pointRequestFrame=-1;SpendSkillPoint();
        }
        static bool Inside(GObject? obj) => InsideAt(obj,Stage.inst.touchPosition.x,Stage.inst.touchPosition.y);
        static bool InsideAt(GObject? obj,float x,float y)
        {
            if(obj==null||obj.isDisposed||!obj.visible)return false;
            var local=obj.GlobalToLocal(new Rect(x,y,0,0));
            return local.x>=0&&local.y>=0&&local.x<=obj.width&&local.y<=obj.height;
        }
        void CreateOutsideDismiss()
        {
            outsideDismiss=Rect(overlay!,0,0,GRoot.inst.width,GRoot.inst.height,Color.clear);
            Listen(outsideDismiss.onTouchBegin,e=>
            {
                outsideArmed=false;
                if(e.inputEvent.button!=0)return;
                outsidePressX=e.inputEvent.x;outsidePressY=e.inputEvent.y;
                if(InsideAt(infoPane,outsidePressX,outsidePressY)||InsideAt(panel,outsidePressX,outsidePressY))return;
                outsideArmed=true;e.CaptureTouch();e.StopPropagation();
                Stage.inst.CancelClick(e.inputEvent.touchId);
            });
            Listen(outsideDismiss.onTouchEnd,e=>
            {
                bool armed=outsideArmed;outsideArmed=false;
                if(!armed||e.inputEvent.button!=0)return;
                e.StopPropagation();Stage.inst.CancelClick(e.inputEvent.touchId);
                float x=e.inputEvent.x,y=e.inputEvent.y;
                if(InsideAt(infoPane,x,y)||InsideAt(panel,x,y))return;
                GuardBook(()=>CloseBooks($"outside left release press=({outsidePressX:F1},{outsidePressY:F1}) release=({x:F1},{y:F1}) info=({infoPane?.x:F1},{infoPane?.y:F1},{infoPane?.width:F1},{infoPane?.height:F1}) panel=({panel?.x:F1},{panel?.y:F1},{panel?.width:F1},{panel?.height:F1}) scale={panel?.scaleX:F3} notice={fastTip?.playing}"));
            });
        }

        void CreatePointInteraction()
        {
            for(int i=0;i<16;i++)
            {
                var stripe=Rect(leftPane!,0,0,1,1,Color.clear);stripe.touchable=false;pointGradient.Add(stripe);
            }
            leftPane!.AddChild(pointButtonText!);
            Listen(pointButton!.onRollOver,e=>{pointHovered=CanSpendPoint;PaintPointButton();});
            Listen(pointButton.onRollOut,e=>{pointHovered=false;pointArmed=false;PaintPointButton();});
            Listen(pointButton.onTouchBegin,e=>
            {
                if(e.inputEvent.button!=0||!CanSpendPoint)return;
                e.StopPropagation();e.CaptureTouch();pointArmed=true;pointHovered=true;PaintPointButton();
            });
            Listen(pointButton.onTouchMove,e=>
            {
                if(!Inside(pointButton)){pointArmed=false;pointHovered=false;PaintPointButton();}
            });
            Listen(pointButton.onTouchEnd,e=>
            {
                bool apply=e.inputEvent.button==0&&pointArmed&&Inside(pointButton)&&CanSpendPoint;
                pointArmed=false;pointHovered=Inside(pointButton);PaintPointButton();
                e.StopPropagation();
                if(apply)
                {
                    Stage.inst.CancelClick(e.inputEvent.touchId);
                    if(pointRequestFrame<0)pointRequestFrame=Time.frameCount;
                }
            });
            Listen(pointButton.onClick,e=>e.StopPropagation());
        }
        void PaintPointButton()
        {
            if(pointButton==null||pointButtonText==null)return;
            bool enabled=CanSpendPoint;
            if(!enabled){pointArmed=false;pointHovered=false;}
            var border=!enabled?new Color(.4f,.4f,.4f,1):pointArmed?new Color(1,1,1,1):pointHovered?new Color(.85f,.95f,1,1):new Color(.62f,.84f,1,1);
            pointButton.DrawRect(440,PointButtonHeight,enabled?3:1,border,enabled?new Color(.16f,.46f,.80f,1):new Color(.18f,.19f,.19f,1));
            {var f=pointButtonText.textFormat;f.color=enabled?Color.white:new Color(.62f,.62f,.62f,1);pointButtonText.textFormat=f;}
            pointButtonText.stroke=enabled?1:0;pointButtonText.strokeColor=new Color(0,0,0,.6f);
            for(int i=0;i<pointGradient.Count;i++)
            {
                var stripe=pointGradient[i];stripe.visible=pointButton.visible&&enabled&&(pointHovered||pointArmed);
                float t=(float)i/(pointGradient.Count-1),brightness=pointArmed?.14f:0;
                float inner=PointButtonHeight-6f;
                stripe.SetXY(pointButton.x+3,pointButton.y+3+i*inner/pointGradient.Count);
                stripe.DrawRect(434,inner/pointGradient.Count+.15f,0,Color.clear,new Color(.14f+brightness,.42f+.2f*(1-t)+brightness,.74f+.2f*(1-t)+brightness,1));
            }
            pointButtonText.SetXY(0,pointButton.y+(pointArmed?2:0));
        }
    }
}
public sealed partial class EntryPoint
{
    private sealed partial class BookPopup
    {
        // 0.5.6: the native learn button keeps its own art and size (0.5.4 forced
        // 440x60 and its label overflowed; 0.5.5 replaced the art). Only a press
        // response is added: 95% scale around the centre while held. The native
        // click still reaches OnClickBtnBook -> BookUse -> Commit unchanged.
        const float LearnPressScale=.95f;
        float learnX,learnY,learnW,learnH,learnScale=1;
        bool learnPressed;
        void CreateLearnInteraction()
        {
            var button=sheet.btnSkillUp;
            Listen(button.onTouchBegin,e=>
            {
                if(e.inputEvent.button!=0||!button.touchable||!button.visible)return;
                e.CaptureTouch();SetLearnPressed(true);
            });
            Listen(button.onTouchMove,e=>SetLearnPressed(Inside(button)&&learnPressed));
            Listen(button.onTouchEnd,e=>SetLearnPressed(false));
            Listen(button.onRollOut,e=>SetLearnPressed(false));
        }
        float PlaceLearnButton(float y)
        {
            var button=sheet.btnSkillUp;
            var native=nativeLayout.FirstOrDefault(x=>x.Widget.Pointer==button.Pointer);
            learnW=Math.Max(1,native?.Width??button.width);learnH=Math.Max(1,native?.Height??button.height);
            learnScale=Math.Min(1,440/learnW);
            learnX=(440-learnW*learnScale)/2;learnY=y;
            var pane=rightPane!;
            if(button.parent?.Pointer!=pane.Pointer)pane.AddChild(button);
            button.visible=true;
            Geometry(button,()=>button.SetSize(learnW,learnH));
            learnPressed=false;ApplyLearnPress();
            return y+learnH*learnScale;
        }
        void SetLearnPressed(bool pressed)
        {
            if(learnPressed==pressed)return;
            learnPressed=pressed;ApplyLearnPress();
        }
        void ApplyLearnPress()
        {
            var button=sheet.btnSkillUp;
            if(button.parent?.Pointer!=rightPane?.Pointer)return;
            float scale=learnScale*(learnPressed?LearnPressScale:1);
            float x=learnX+learnW*(learnScale-scale)/2,y=learnY+learnH*(learnScale-scale)/2;
            Geometry(button,()=>{button.SetScale(scale,scale);button.SetXY(x,y);});
        }
    }
}

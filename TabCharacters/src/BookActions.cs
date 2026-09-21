using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint
{
    // 0.6.10: the physical key that closed a panel. Only Action_B from that key is swallowed,
    // and only until the key is released (checked inside the input callback, never polled).
    // 0.6.9 used a frame window that every held right-click kept extending, which blocked
    // all game input while the right mouse button was held after any panel close.
    private static CloseKey closeKey;
    private enum CloseKey { None, RightMouse, Escape }
    private static bool CloseKeyHeld()=>closeKey switch {
        CloseKey.RightMouse => Mouse.current?.rightButton.isPressed==true,
        CloseKey.Escape => Keyboard.current?.escapeKey.isPressed==true,
        _ => false };
    private static UICharacterView? pointOwner;
    private static GTextField? pointFooter;
    private static Il2CppClient.UILogic.UITips.UITipsCtrl? tipOwner;
    private static Transition? fastTip;
    private static GObject? tipLayer;
    private static float originalTipSpeed, tipWaitUntil;
    private static int originalTipOrder, originalTipIndex;
    private static GComponent? originalTipParent;
    private static bool originalTipTouchable;
    private static bool tipStarted;

    private static void RefreshPointFooter(UICharacterView view)
    {
        if(pointOwner?.Pointer!=view.Pointer){ClearPointFooter();pointOwner=view;}
        TickPointFooter();
    }
    private static void ClearPointFooter()
    {pointFooter?.Dispose();pointFooter=null;pointOwner=null;}
    private static void TickPointFooter()
    {
        var view=pointOwner;
        if(view==null)return;
        var model=view._model;
        if(view._UIContent_k__BackingField==null||view._UIContent_k__BackingField.isDisposed||!IsCharacter(view))
        {ClearPointFooter();return;}
        int points=model.RoleIndex>=0&&model.RoleIndex<model.ListRole.Count&&!model.ListRole[model.RoleIndex].isSeaman
            ?model.ListRole[model.RoleIndex].HeroData.SkillPoints:0;
        if(pointFooter==null)
        {
            pointFooter=new GTextField{touchable=false,autoSize=AutoSizeType.Shrink,singleLine=true,sortingOrder=31900};
            var f=pointFooter.textFormat;f.font=view.SheetCharacter.texSkillDesc.textFormat.font;
            f.color=new Color(.3f,.24f,.12f,1);pointFooter.textFormat=f;
            GRoot.inst.AddChild(pointFooter);
        }
        pointFooter.visible=points>0&&view.IsInputActive;
        pointFooter.text="사용 가능한 스킬 포인트: "+points;
        var root=GRoot.inst;
        pointFooter.SetXY(root.width*.58f,root.height*.955f);pointFooter.SetSize(root.width*.23f,root.height*.032f);
        var format=pointFooter.textFormat;format.size=Math.Max(1,(int)(root.height*.024f));pointFooter.textFormat=format;
    }

    // Keep the original view, text, queue and completion callback. Only the
    // learning-scoped native transition and its own content are borrowed.
    private static bool BookTips(Il2CppClient.UILogic.UITips.UITipsCtrl __instance)
    {
        TraceUi("tip-prefix");
        ClearLearningNotice();
        if(books?.Committing==true){tipOwner=__instance;tipWaitUntil=Time.unscaledTime+2;}
        return true;
    }
    private static void BookTipsAfter(){TickLearningNotice();TraceUi("tip-postfix");}
    private static void TickLearningNotice()
    {
        if(tipOwner==null)return;
        try
        {
            var view=tipOwner._View_k__BackingField;
            var content=view?._UIContent_k__BackingField;
            if(content==null||content.isDisposed){if(Time.unscaledTime>tipWaitUntil)ClearLearningNotice();return;}
            if(view!._model.CtrlSelect!=6||view._model.CtrlBaseSelect!=1){ClearLearningNotice();return;}
            if(fastTip==null)
            {
                fastTip=view.AniTips;originalTipSpeed=fastTip.timeScale;
                fastTip.timeScale=originalTipSpeed*2;

            }
            if(fastTip.playing)
            {
                tipStarted=true;
                // ShowBaseTips can return before the native view is attached. Never
                // promote a detached object or climb into the shared FeatureUI.
                if(tipLayer==null&&content.parent!=null)
                {
                    tipLayer=content;originalTipParent=content.parent;
                    originalTipIndex=originalTipParent.GetChildIndex(content);
                    originalTipOrder=content.sortingOrder;originalTipTouchable=content.touchable;
                    TraceUi("before-raise");
                    content.touchable=false;content.sortingOrder=32760;
                    MoveTip(content,GRoot.inst);
                    TraceUi("after-raise");
                }
            }
            else if(tipStarted||Time.unscaledTime>tipWaitUntil)ClearLearningNotice();
        }
        catch(Exception ex){host?.LoggerInstance.Error("Native learning tip: "+ex);ClearLearningNotice();}
    }
    private static void ClearLearningNotice()
    {
        if(tipOwner!=null)TraceUi("before-restore");
        if(fastTip!=null)fastTip.timeScale=originalTipSpeed;
        if(tipLayer!=null&&!tipLayer.isDisposed)
        {
            tipLayer.sortingOrder=originalTipOrder;tipLayer.touchable=originalTipTouchable;
            // Native Hide owns detachment. Do not resurrect a completed/removed view.
            if(tipLayer.parent?.Pointer==GRoot.inst.Pointer&&originalTipParent!=null&&originalTipParent.Pointer!=GRoot.inst.Pointer)
            {
                if(!originalTipParent.isDisposed)
                {
                    MoveTip(tipLayer,originalTipParent);
                    originalTipParent.SetChildIndex(tipLayer,Math.Min(originalTipIndex,originalTipParent.numChildren-1));
                }
                else tipLayer.RemoveFromParent();
            }
        }
        if(tipOwner!=null)TraceUi("after-restore");
        fastTip=null;tipLayer=null;originalTipParent=null;tipOwner=null;tipStarted=false;
    }

    private static void MoveTip(GObject content,GComponent destination)
    {
        if(content.parent?.Pointer==destination.Pointer)return;
        // Confirmed native OnOwnerRemovedFromStage tests bit 2 before Stop.
        // Preserve the running native transition and its original completion callback.
        var transition=fastTip;
        int options=transition?._options??0;
        try
        {
            if(transition!=null)transition._options=options|Transition.OPTION_AUTO_STOP_DISABLED;
            destination.AddChild(content);
        }
        finally { if(transition!=null)transition._options=options; }
    }

    private sealed partial class BookPopup
    {
        const int PointButtonHeight=84,PointButtonFont=38;
        GTextField? pointText,pointButtonText;
        GGraph? pointButton;
        int lastPointFrame=-1;
        void CreateSkillPointControls()
        {
            pointText=Text(leftPane!,"",0,0,440,40,23);
            // 0.6.6 (user request): the +1 button was hard to see. Taller, larger centred
            // label, saturated fill; colours are set per state in PaintPointButton.
            pointButton=Rect(leftPane!,0,0,440,PointButtonHeight,new Color(.74f,.82f,.78f,1));
            pointButtonText=Text(leftPane!,"",0,0,440,PointButtonHeight,PointButtonFont);
            pointButtonText.singleLine=true;pointButtonText.autoSize=AutoSizeType.None;pointButtonText.SetSize(440,PointButtonHeight);
            {var f=pointButtonText.textFormat;f.align=AlignType.Center;pointButtonText.textFormat=f;}
            pointButtonText.align=AlignType.Center;pointButtonText.verticalAlign=VertAlignType.Middle;
            CreatePointInteraction();
        }
        bool CanSpendPoint => Valid&&skillId>=0&&Role.SkillPoints>0&&Role.GetRoleSkillById(skillId)?.IsMax==false;
        float LayoutSkillPoints(float y)
        {
            if(pointText==null||pointButton==null||pointButtonText==null)return y;
            bool show=skillId>=0;pointText.visible=pointButton.visible=pointButtonText.visible=show;
            if(!show)return y;
            pointText.text="사용 가능한 스킬 포인트: "+Role.SkillPoints;
            y=PlaceText(pointText,0,y,440);
            bool enabled=CanSpendPoint;pointButton.touchable=enabled;

            pointButton.SetXY(0,y);pointButtonText.SetXY(0,y);
            pointButtonText.text=enabled?"스킬 레벨 +1":Role.SkillPoints<=0?"사용할 스킬 포인트가 없습니다.":"이미 최대 스킬 레벨입니다.";
            {var f=pointButtonText.textFormat;f.size=enabled?PointButtonFont:PointButtonFont*2/3;pointButtonText.textFormat=f;}
            PaintPointButton();
            return y+PointButtonHeight+24;
        }
        internal void SpendSkillPoint()
        {
            if(Committing||lastPointFrame==Time.frameCount||!CanSpendPoint)return;
            // DB function does not reject insufficient points before changing the skill.
            // Recheck both the selected role and the point balance immediately before it.
            var db=UICharacterCtrl.Data.PlayerRole;
            var live=db.FindHoldRole(Role.RoleId);
            if(live==null||live.Pointer!=Role.Pointer)return;
            TraceUi("point-before",true);
            lastPointFrame=Time.frameCount;Committing=true;
            try
            {
                if(!db.UpdateRoleSkillLevelBySkillPoints(Role.RoleId,skillId,1))return;
                Ctrl.InitSkillData();View.RefreshSheetCharacter(model.ListRole[roleIndex]);View.RefreshTipsRoleInfo();model.MarkDirty();
                Il2CppClient.UILogic.UITips.UITipsCtrl.Instance.ShowBaseTips("스킬 레벨이 1 상승했습니다.","");
            }
            finally{Committing=false;}
            Refresh();RefreshPointFooter(View);TraceUi("point-after");
        }
    }
}

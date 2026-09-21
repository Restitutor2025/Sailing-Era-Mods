using Il2CppFairyGUI;
using UnityEngine;

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint
{
    private sealed partial class BookPopup
    {
        GComponent? infoPane;
        GObject? companionBackdrop;
        GGraph? languageBackdrop;
        Rect anchor;
        float anchorRootWidth,anchorRootHeight;
        float layoutHeightFloor;
        bool toRight=true;
        // 0.5.6: the native tooltip is only read. Borrowing its widgets (0.4.0-0.5.5)
        // left the native group/gear state out of sync after every close, so the
        // tooltip drifted left and grew on each reopen (user log 2026-09-20 17:51-17:53).
        bool pinnedNativeInfo,styledFromNative;
        readonly Dictionary<IntPtr,(GTextField Text,Color Color)> themedTexts=new();
        Color panelColor=new(.12f,.14f,.15f,.94f);
        // 0.5.5: only the learning panel's background is translucent; text,
        // icons and buttons keep full opacity. The info pane is unchanged.
        const float PanelBackgroundAlpha=.8f;
        Rect RootBounds(GObject obj)
        {
            // GGroup is a layout helper without a DisplayObject. Its rectangle
            // is in its parent component's coordinates, not a display transform.
            if(obj.displayObject==null)
            {
                var owner=obj.parent??throw new InvalidOperationException("Detached UI helper has no coordinate owner.");
                return GRoot.inst.GlobalToLocal(owner.LocalToGlobal(new Rect(obj.x,obj.y,obj.width,obj.height)));
            }
            return GRoot.inst.GlobalToLocal(obj.LocalToGlobal(new Rect(0,0,obj.width,obj.height)));
        }
        bool InTips(GObject obj)
        {
            for(var g=obj.group;g!=null;g=g.group)
                if(g.Pointer==View.SheetCharacter.groupTips.Pointer)return true;
            var c=View.SheetCharacter;
            return obj.Pointer==c.texSkillDesc.Pointer||obj.Pointer==c.TexTitleSkillLevel.Pointer||obj.Pointer==c.TexMax.Pointer
                ||obj.Pointer==c.texLevel.Pointer||obj.Pointer==c.texAddLevel.Pointer||obj.Pointer==c.listItem.Pointer;
        }
        void PrepareNativeInfo()
        {
            if(infoPane==null)return;
            var c=View.SheetCharacter;
            // Read-only: parent, group, position, size, scale, relations, gears and
            // visibility of the native tooltip widgets are never written.
            // 0.5.10: the side position is fixed from the first read. Re-reading after each
            // book change moved the panel away from the tooltip (user report 2026-09-20:
            // gap ~5px at open, ~15px after switching books, never returning).
            if(!pinnedNativeInfo)ReadNativeAnchor();
            else TraceAnchorDrift();
            pinnedNativeInfo=true;
            if(styledFromNative)return;
            styledFromNative=true;
            if(selectedSkillIndex>=0&&selectedSkillIndex<c.listSkill.numChildren)
                toRight=anchor.center.x>=RootBounds(c.listSkill.GetChildAt(selectedSkillIndex)).center.x;
            else toRight=c.tipsType.selectedIndex==1;
            var members=new List<(GObject Widget,Rect Bounds)>();
            for(int i=0;i<c.numChildren;i++)
            {
                var obj=c.GetChildAt(i);
                if(!obj.isDisposed&&obj.displayObject!=null&&InTips(obj))members.Add((obj,RootBounds(obj)));
            }
            foreach(var member in members.OrderByDescending(x=>x.Bounds.width*x.Bounds.height))
            {
                var graph=member.Widget.TryCast<GGraph>();
                if(graph!=null){panelColor=graph.color;break;}
            }
            var backdrop=members.Where(x=>x.Widget.TryCast<GImage>()!=null&&x.Bounds.width>=anchor.width*.75f)
                .OrderByDescending(x=>x.Bounds.width*x.Bounds.height).FirstOrDefault().Widget;
            if(backdrop!=null&&!string.IsNullOrEmpty(backdrop.resourceURL))
            {
                // A separate package instance styles the learning panel; the native image is untouched.
                companionBackdrop=UIPackage.CreateObjectFromURL(backdrop.resourceURL);
                companionBackdrop.touchable=false;panel!.AddChildAt(companionBackdrop,0);
                var copy=companionBackdrop.TryCast<GImage>();if(copy!=null)copy.color=backdrop.TryCast<GImage>()!.color;
            }
        }
        bool anchorDriftLogged;
        // Read-only diagnostic: report once if the native tooltip bounds change while pinned.
        void TraceAnchorDrift()
        {
            if(anchorDriftLogged)return;
            var c=View.SheetCharacter;
            c.groupTips.EnsureBoundsCorrect();
            var now=GRoot.inst.GlobalToLocal(c.LocalToGlobal(new Rect(c.groupTips.x,c.groupTips.y,c.groupTips.width,c.groupTips.height)));
            if(Math.Abs(now.x-anchor.x)<=1&&Math.Abs(now.width-anchor.width)<=1&&Math.Abs(now.y-anchor.y)<=1)return;
            anchorDriftLogged=true;
            host?.LoggerInstance.Msg($"Characters tooltip bounds changed after open (panel kept at first position): open=({anchor.x:F1},{anchor.y:F1},{anchor.width:F1},{anchor.height:F1}) now=({now.x:F1},{now.y:F1},{now.width:F1},{now.height:F1})");
        }
        void ReadNativeAnchor()
        {
            var c=View.SheetCharacter;
            c.groupTips.EnsureBoundsCorrect();
            var bounds=GRoot.inst.GlobalToLocal(c.LocalToGlobal(new Rect(c.groupTips.x,c.groupTips.y,c.groupTips.width,c.groupTips.height)));
            if(bounds.width<=0||bounds.height<=0)throw new InvalidOperationException("Native skill tooltip has no usable bounds.");
            anchor=bounds;anchorRootWidth=GRoot.inst.width;anchorRootHeight=GRoot.inst.height;
            // Empty, input-transparent stand-in: only marks the native tooltip area
            // so clicks on it do not count as outside clicks.
            var pane=infoPane!;pane.SetScale(1,1);pane.SetXY(anchor.x,anchor.y);pane.SetSize(anchor.width,anchor.height);
        }
        void PrepareLanguageInfo()
        {
            if(infoPane==null||summary==null)return;
            if(anchorRootWidth==0)
            {
                var language=RootBounds(View.SheetCharacter.texLanguage);
                anchor=new Rect(language.x,language.y+language.height,488,1);
                anchorRootWidth=GRoot.inst.width;anchorRootHeight=GRoot.inst.height;
                languageBackdrop=Rect(infoPane,0,0,488,1,panelColor);
                infoPane.AddChild(summary);
            }
            summary.SetSize(440,summary.textHeight+8);infoPane.SetSize(488,summary.height+48);
            languageBackdrop!.DrawRect(infoPane.width,infoPane.height,1,new Color(0,0,0,1),panelColor);
        }
        void StyleCompanion()
        {
            if(panel==null||background==null)return;
            if(companionBackdrop!=null){companionBackdrop.SetSize(panel.width,panel.height);companionBackdrop.alpha=PanelBackgroundAlpha;background.visible=false;}
            else background.DrawRect(panel.width,panel.height,1,new Color(0,0,0,1),new Color(panelColor.r,panelColor.g,panelColor.b,PanelBackgroundAlpha));
            ThemeTexts(panel);
        }
        void ThemeTexts(GObject obj)
        {
            if(badges.Values.Any(b=>b.Pointer==obj.Pointer))return;
            if(obj.Pointer==pointButtonText?.Pointer)return; // 0.6.6: state colours set by PaintPointButton
            var text=obj.TryCast<GTextField>();
            if(text!=null)
            {
                var f=text.textFormat;var color=f.color;
                // Keep red failed requirements and green bonuses; match neutral text
                // to the original tooltip's foreground instead of a new white theme.
                bool accent=(color.r>color.g*1.3f&&color.r>color.b*1.3f)||(color.g>color.r*1.3f&&color.g>color.b*1.15f);
                if(!accent)
                {
                    themedTexts.TryAdd(text.Pointer,(text,color));
                    f.color=View.SheetCharacter.texSkillDesc.textFormat.color;text.textFormat=f;
                }
            }
            var component=obj.TryCast<GComponent>();
            if(component!=null)for(int i=0;i<component.numChildren;i++)ThemeTexts(component.GetChildAt(i));
        }
        void LayoutSidePanels()
        {
            if(overlay==null||panel==null||infoPane==null||anchorRootWidth<=0)return;
            var root=GRoot.inst;overlay.SetSize(root.width,root.height);outsideDismiss?.SetSize(root.width,root.height);
            layoutHeightFloor=Math.Max(layoutHeightFloor,panel.height);
            if(pinnedNativeInfo)
            {
                if(anchorRootWidth!=root.width||anchorRootHeight!=root.height)ReadNativeAnchor();
                var beside=SidePanelPlacement.Beside(root.width,root.height,anchor.x,anchor.y,anchor.width,panel.width,layoutHeightFloor,toRight);
                panel.SetScale(beside.Scale,beside.Scale);panel.SetXY(beside.X,beside.Y);
                return;
            }
            var place=SidePanelPlacement.Calculate(root.width,root.height,anchor.x*root.width/anchorRootWidth,
                anchor.y*root.height/anchorRootHeight,infoPane.width,infoPane.height,panel.width,layoutHeightFloor,toRight);
            infoPane.SetScale(place.Scale,place.Scale);panel.SetScale(place.Scale,place.Scale);
            infoPane.SetXY(place.InfoX,place.Y);panel.SetXY(place.ActionsX,place.Y);
        }
        void RestoreNativeInfo()
        {
            pinnedNativeInfo=false;
            foreach(var entry in themedTexts.Values)
                if(!entry.Text.isDisposed){var format=entry.Text.textFormat;format.color=entry.Color;entry.Text.textFormat=format;}
            themedTexts.Clear();
        }
    }
}

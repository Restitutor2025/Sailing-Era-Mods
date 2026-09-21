using Il2CppFairyGUI;
using Il2CppClient.UILogic.UICharacter;
using UnityEngine;

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint
{
    private sealed partial class BookPopup
    {
        GList? previewSkills;
        GGraph? background,closeButton,divider;
        GTextField? status, effect,closeLabel;
        GComponent? leftPane,rightPane;
        readonly List<WidgetLayout> nativeLayout = new();

        // RefreshSkillBookTips writes a level preview into content.listSkill. Supply
        // unrendered package rows only for this synchronous call: no 3D holder registration.
        void RefreshBookDetails(SkillBook book)
        {
            var content=View._UIContent_k__BackingField;
            var original=content.listSkill;
            if(previewSkills==null) previewSkills=new GList();
            while(previewSkills.numChildren<model.DictSkill.Count)
                previewSkills.AddChild(Il2CppCharacter.UICom_SkillItem.CreateInstance());
            if(skillId<0) View.RefreshTipsRoleInfo();
            try
            {
                if(skillId<0) LanguagePreviewBegin(View,Role);
                content.listSkill=previewSkills;
                View.RefreshSkillBookTips(book,Role);
                // These controls now live in clipped columns, so the old sheet's
                // child traversal cannot apply their native display/color gears.
                foreach(var state in new[]{sheet.isSeaman,sheet.noBook,sheet.isExpNotEnough,sheet.bookType,sheet.btnType})
                    foreach(var item in nativeLayout)
                        if(item.Widget.Pointer!=sheet.Pointer&&item.Widget.parent?.Pointer!=sheet.Pointer)
                            item.Widget.HandleControllerChanged(state);
            }
            finally {content.listSkill=original;if(skillId<0) LanguagePreviewEnd();}
        }

        sealed class WidgetLayout
        {
            internal readonly GObject Widget;
            internal float Width=>w;
            internal float Height=>h;
            readonly float x,y,w,h;
            readonly float scaleX,scaleY;
            readonly bool visible;
            readonly GGroup? group;
            readonly GComponent? parent;
            readonly int index;
            readonly GTextField? text;
            readonly AutoSizeType autoSize;
            readonly bool singleLine;
            readonly GObject relationStorage=new GObject();
            internal WidgetLayout(GObject widget)
            {
                Widget=widget;x=widget.x;y=widget.y;w=widget.width;h=widget.height;
                scaleX=widget.scaleX;scaleY=widget.scaleY;
                visible=widget.visible;group=widget.group;
                parent=widget.parent;index=parent?.GetChildIndex(widget)??0;
                text=widget.TryCast<GTextField>();
                if(text!=null){autoSize=text.autoSize;singleLine=text.singleLine;}
                // A detached owner stores the relation definitions without applying
                // their resize/move notifications to the borrowed native widgets.
                relationStorage.relations.CopyFrom(widget.relations);
                widget.relations.ClearAll();widget.group=null;
            }
            internal void RestoreGeometry()
            {
                if(Widget.isDisposed)return;
                if(parent!=null&&!parent.isDisposed&&Widget.parent?.Pointer!=parent.Pointer)
                    parent.AddChildAt(Widget,Math.Min(index,parent.numChildren));
                else if(parent==null||parent.isDisposed)Widget.RemoveFromParent();
                if(text!=null)Geometry(text,()=>{text.autoSize=autoSize;text.singleLine=singleLine;});
                Place(Widget,x,y,w,h);Widget.visible=visible;
                Geometry(Widget,()=>Widget.SetScale(scaleX,scaleY));
            }
            internal void RestoreRelations()
            {
                if(!Widget.isDisposed){Widget.group=group;Widget.relations.CopyFrom(relationStorage.relations);}
                relationStorage.Dispose();
            }
        }
        void CaptureLayout()
        {
            nativeLayout.Add(new WidgetLayout(sheet));
            for(int i=0;i<sheet.numChildren;i++)nativeLayout.Add(new WidgetLayout(sheet.GetChildAt(i)));
        }
        void RestoreLayout()
        {
            foreach(var item in nativeLayout)item.RestoreGeometry();
            foreach(var item in nativeLayout)item.RestoreRelations();
            nativeLayout.Clear();
        }
        static void Place(GObject item,float x,float y,float width,float height)
        {item.visible=true;Geometry(item,()=>{item.SetSize(width,height);item.SetXY(x,y);});}
        static void Geometry(GObject item,Action action)
        {
            bool locked=item._gearLocked;item._gearLocked=true;
            try{action();}finally{item._gearLocked=locked;}
        }
        void Column(GObject item,float x,float y,float width,float height)
        {
            var target=x>=480?rightPane!:leftPane!;
            if(item.parent?.Pointer!=target.Pointer)target.AddChild(item);
            Place(item,x>=480?x-480:x,y,width,height);
        }
        float PlaceText(GTextField text,float x,float y,float width)
        {
            Geometry(text,()=>{text.singleLine=false;text.autoSize=AutoSizeType.Height;text.SetSize(width,40);});
            float height=Math.Max(32,text.textHeight+6);
            Column(text,x,y,width,height);return y+height+10;
        }
        void CompactLayout(int chosen)
        {
            if(panel==null||summary==null||status==null||background==null||effect==null||leftPane==null||rightPane==null||divider==null)return;
            // Only actual book controls are shown; the old full-page background,
            // empty placeholders and unused filter decoration stay hidden.
            foreach(var item in nativeLayout)if(item.Widget.Pointer!=sheet.Pointer)item.Widget.visible=false;
            float left=0;
            left=LayoutSkillPoints(left);
            if(chosen<0)
                left=PlaceText(sheet.TexNoBook,0,left,440);
            else
            {
                left=PlaceText(sheet.TexTitleBook,0,left,440);
                // Keep native scroll behavior, but limit the viewport to three rows.
                float cellW=112,cellH=112;
                if(sheet.listBook.numChildren>0)
                {var row=sheet.listBook.GetChildAt(0);cellW=Math.Max(1,row.width+sheet.listBook.columnGap);cellH=Math.Max(1,row.height+sheet.listBook.lineGap);}
                int columns=Math.Max(1,(int)((440+sheet.listBook.columnGap)/cellW));
                int rows=Math.Min(3,(filtered.Count+columns-1)/columns);
                float gridHeight=rows*cellH;
                Column(sheet.listBook,0,left,440,gridHeight);
                left+=gridHeight;
            }
            float right=0;
            status.visible=chosen>=0;
            learnPressed=false;
            effect.visible=chosen>=0&&skillId>=0;
            if(chosen>=0)
            {
                right=PlaceText(sheet.texName,480,right,440);
                right=PlaceText(sheet.TexTitleBookCondition,480,right,440);
                // Native renderer retains condition icons, red requirements and values.
                Column(sheet.listCondition,480,right,440,sheet.listCondition.height);
                right+=sheet.listCondition.height+12;
                float expTitle=PlaceText(sheet.TexTitleBookExp,480,right,250);
                float expValue=PlaceText(sheet.texExp,740,right,180);right=Math.Max(expTitle,expValue);
                right=PlaceText(sheet.texBookDesc,480,right,440);
                right=PlaceText(sheet.TexTitleBookSkill,480,right,440);
                if(skillId>=0)
                {
                    Column(sheet.loaderIconSkill,480,right,36,36);
                    Column(effect,820,right,100,36);
                    right=PlaceText(sheet.texSkillName,526,right,280);
                }
                else right=PlaceText(sheet.texLanguageName,480,right,440);
                float currentTitle=PlaceText(sheet.texTitleExp,480,right,270);
                float currentValue=PlaceText(sheet.texCurExp,750,right,170);right=Math.Max(currentTitle,currentValue);
                int state=Ctrl.GetBookStateByParam(filtered[chosen],Role);
                status.text=state switch
                {
                    0=>"",
                    1=>skillId<0?"이미 습득한 언어입니다.":"이미 학습한 책입니다.",
                    2=>"습득 가능한 언어 수가 가득 찼습니다.",
                    3=>"이미 최대 스킬 레벨입니다.",
                    _=>"학습 조건 또는 필요 경험치를 충족하지 못했습니다."
                };
                if(state==0)
                right=PlaceLearnButton(right);
                else
                {
                    var format=status.textFormat;format.color=new Color(.75f,.2f,.22f,1);status.textFormat=format;
                    right=PlaceText(status,480,right,440);
                }
            }
            float contentHeight=left+(chosen>=0?24+right:0);
            float width=488;
            Geometry(sheet,()=>sheet.SetSize(width-48,contentHeight));
            leftPane.SetSize(440,left);rightPane.SetSize(440,right);
            rightPane.SetXY(24,80+left+24);divider.SetXY(24,80+left+12);
            rightPane.visible=chosen>=0;divider.visible=chosen>=0;
            divider.DrawRect(440,1,0,Color.clear,new Color(.55f,.55f,.55f,1));
            panel.SetSize(width,80+contentHeight+24);
            StyleCompanion();
            Layout();
        }
    }
}

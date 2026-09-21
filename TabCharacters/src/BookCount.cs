using Il2CppFairyGUI;
using UnityEngine;

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint
{
    private sealed partial class BookPopup
    {
        // 0.5.10: the book count uses the bag's "x3" badge style. The bag badge is drawn by
        // Item Rebuild (ItemRebuild/src/BagBadge.cs) from the native Common.UIcompSlotCommon
        // title: its font, size round(round(size/2)*1.1), white, right/bottom aligned,
        // 1px black stroke, inset min(14, min(w,h)/5) inside the illustration, text "x"+count.
        // The same rules are reproduced here; there is no reference to the Item Rebuild DLL.
        static TextFormat? countFormat;
        static bool countStyleWarned;
        TextFormat CountFormat()
        {
            if(countFormat!=null)return countFormat;
            var source=new TextFormat();
            Il2CppCommon.UIcompSlotCommon? template=null;
            try
            {
                template=Il2CppCommon.UIcompSlotCommon.CreateInstance();
                if(template?.title!=null)source.CopyFrom(template.title.textFormat);
                else throw new InvalidOperationException("Common slot template has no title.");
            }
            catch(Exception ex)
            {
                // Package unavailable: keep the panel's font; the next popup retries.
                if(!countStyleWarned){countStyleWarned=true;host?.LoggerInstance.Warning("Book count style fallback: "+ex.Message);}
                source.CopyFrom(View.SheetCharacter.texSkillDesc.textFormat);
                source.size=36; // Finish() maps 36 to 20.
                template?.Dispose();
                return Finish(source);
            }
            template?.Dispose();
            countFormat=Finish(source);
            return countFormat;
        }
        static TextFormat Finish(TextFormat format)
        {
            format.size=Math.Max(1,(int)Math.Round(Math.Round(format.size/2.0)*1.1));
            format.color=Color.white;format.gradientColor=null;format.align=AlignType.Right;
            return format;
        }
        GTextField CreateCountBadge(GComponent slot)
        {
            var format=new TextFormat();format.CopyFrom(CountFormat());
            var label=new GTextField{touchable=false,sortingOrder=int.MaxValue,autoSize=AutoSizeType.Shrink,singleLine=true};
            label.textFormat=format;label.align=AlignType.Right;label.verticalAlign=VertAlignType.Bottom;
            label.stroke=1;label.strokeColor=Color.black;
            slot.AddChild(label);
            return label;
        }
        static void PlaceCountBadge(GTextField label,GLoader? icon,GObject slot,int count)
        {
            if(count<=0){label.visible=false;return;}
            float left,top,w,h;
            if(icon!=null&&!icon.isDisposed){left=icon.xMin;top=icon.yMin;w=icon.actualWidth;h=icon.actualHeight;}
            else {left=0;top=0;w=slot.width;h=slot.height;}
            if(w<=0||h<=0){label.visible=false;return;}
            float inset=Math.Min(14,Math.Min(w,h)/5);
            float height=Math.Min(h-2*inset,label.textFormat.size*1.6f+4);
            label.SetSize(w-2*inset,height);
            label.SetXY(left+inset,top+h-inset-height);
            string text="x"+count;
            if(label.text!=text)label.text=text;
            label.visible=true;
        }
    }
}

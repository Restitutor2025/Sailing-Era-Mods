using Il2CppFairyGUI;
using UnityEngine;
namespace Restitutor.Cheats.Interface;

internal static class Theme {
    internal static readonly Color Gold=new(.75f,.57f,.29f,1);
    internal static readonly Color Navy=new(.035f,.075f,.10f,.97f);
    internal static readonly Color InkBlue=new(.07f,.13f,.17f,.98f);
    internal static readonly Color Cream=new(.96f,.91f,.78f,1);
    internal static GGraph Box(GComponent parent,float x,float y,float w,float h,Color fill,bool touch=false) {
        var g=new GGraph{touchable=touch};g.SetXY(x,y);g.DrawRect(w,h,1,Gold,fill);parent.AddChild(g);return g;
    }
    internal static GTextField Label(GComponent parent,string text,float x,float y,float w,int size=20,bool center=false) {
        var t=new GTextField{touchable=false,autoSize=AutoSizeType.None,singleLine=true};
        var f=t.textFormat;f.font=UIConfig.defaultFont;f.size=size;f.color=Cream;f.align=center?AlignType.Center:AlignType.Left;
        t.textFormat=f;t.SetXY(x,y);t.SetSize(w,32);t.text=text;parent.AddChild(t);return t;
    }
}

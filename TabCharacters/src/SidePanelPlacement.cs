namespace Restitutor.TabCharacters;

internal readonly record struct SidePanelPosition(float InfoX,float ActionsX,float Y,float Scale);
internal readonly record struct BesidePosition(float X,float Y,float Scale,bool Right,bool Overlaps);
internal static class SidePanelPlacement
{
    internal static SidePanelPosition Calculate(float screenWidth,float screenHeight,float anchorX,float anchorY,
        float infoWidth,float infoHeight,float actionWidth,float actionHeight,bool toRight)
    {
        float margin=Math.Min(screenWidth,screenHeight)*.012f;
        float gap=Math.Min(infoWidth,actionWidth)*.025f;
        float scale=Math.Min(1,Math.Min((screenWidth-margin*2)/(infoWidth+actionWidth+gap),
            (screenHeight-margin*2)/Math.Max(infoHeight,actionHeight)));
        scale=Math.Max(.001f,scale);
        float total=(infoWidth+actionWidth+gap)*scale;
        float start=toRight?anchorX:anchorX-(actionWidth+gap)*scale;
        start=Math.Clamp(start,margin,Math.Max(margin,screenWidth-margin-total));
        float y=Math.Clamp(anchorY,margin,Math.Max(margin,screenHeight-margin-Math.Max(infoHeight,actionHeight)*scale));
        return toRight?new(start,start+(infoWidth+gap)*scale,y,scale):new(start+(actionWidth+gap)*scale,start,y,scale);
    }
    // 0.5.6: the native tooltip stays where the game drew it. Only the learning
    // panel moves/scales: preferred side first, the other side if it fits better,
    // and an on-screen overlap only when neither side has half-scale room.
    internal static BesidePosition Beside(float screenWidth,float screenHeight,float anchorX,float anchorY,float anchorWidth,
        float panelWidth,float panelHeight,bool preferRight)
    {
        float margin=Math.Min(screenWidth,screenHeight)*.012f;
        float gap=Math.Min(anchorWidth,panelWidth)*.025f;
        float heightScale=(screenHeight-margin*2)/Math.Max(1,panelHeight);
        float rightSpace=screenWidth-margin-(anchorX+anchorWidth+gap),leftSpace=anchorX-gap-margin;
        float Fit(float space)=>Math.Min(1,Math.Min(space/Math.Max(1,panelWidth),heightScale));
        bool right=preferRight;
        float preferred=Fit(preferRight?rightSpace:leftSpace),other=Fit(preferRight?leftSpace:rightSpace);
        if(preferred<1&&other>preferred)right=!preferRight;
        float scale=Fit(right?rightSpace:leftSpace);
        bool overlaps=scale<.5f;
        if(overlaps)scale=Math.Min(1,heightScale);
        scale=Math.Max(.001f,scale);
        float x=right?anchorX+anchorWidth+gap:anchorX-gap-panelWidth*scale;
        x=Math.Clamp(x,margin,Math.Max(margin,screenWidth-margin-panelWidth*scale));
        float y=Math.Clamp(anchorY,margin,Math.Max(margin,screenHeight-margin-panelHeight*scale));
        return new(x,y,scale,right,overlaps);
    }
}

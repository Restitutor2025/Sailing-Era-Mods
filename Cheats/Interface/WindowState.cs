namespace Restitutor.Cheats.Interface;

// Process-lifetime presentation state. Save loads must not reopen a dismissed window.
internal sealed class WindowState {
    internal bool Closed { get; private set; }
    internal bool IsOpen { get; private set; }
    internal bool Minimized=>!IsOpen;
    private bool hHeld,oHeld;
    // 1.5.0: O turns every cheat off/on. Process lifetime: kept across locations and save loads,
    // true again after a game restart. Off = window hidden and all panel effects reset.
    internal bool CheatsOn { get; private set; }=true;
    internal bool PollO(bool down,bool allowed) {
        bool pressed=down && !oHeld; oHeld=down;
        if(!pressed || !allowed)return false;
        CheatsOn=!CheatsOn; if(CheatsOn)Closed=false; return true;
    }
    internal bool PollH(bool down,bool allowed) {
        bool pressed=down && !hHeld; hHeld=down;
        if(!pressed || !allowed || !CheatsOn)return false;
        IsOpen=!Shows; Closed=false; return true;
    }
    private int location;
    internal void ResetLocation() { location=0; }
    internal bool ObserveLocation(int current) {
        bool entered=current!=0 && current!=location;
        location=current;
        // Location changes release input ownership without changing user presentation.
        return entered;
    }
    internal float X, Y;
    private bool positioned;
    internal void Toggle() { if(!Closed) IsOpen=!IsOpen; }
    internal void Close()=>Closed=true;
    internal bool Shows=>!Closed && !Minimized;
    internal void Place(float screenWidth,float screenHeight,float width,float height) {
        // Keep the initial window clear of the left and bottom HUD edges.
        if(!positioned) { X=screenWidth*.09f;Y=screenHeight-height-screenHeight*.065f;positioned=true; }
        Move(X,Y,screenWidth,screenHeight,width,height);
    }
    internal void Move(float x,float y,float screenWidth,float screenHeight,float width,float height) {
        X=Math.Clamp(x,0,Math.Max(0,screenWidth-width));Y=Math.Clamp(y,0,Math.Max(0,screenHeight-height));
    }
}

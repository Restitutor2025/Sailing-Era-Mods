using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
namespace Restitutor.Cheats.Battle;

internal sealed class BattlePanel : Panel {
    public override string Id=>"battle";
    public override int Order=>19;
    public override float Height=>246;
    public override bool Visible=>Active;
    private readonly List<GGraph> melee=new(),cannon=new();
    private GGraph? boardBox,hullBox;
    private GTextField? boardMark,hullMark;
    private int drawnMelee=-1,drawnCannon=-1; // 1.0.1: redraw only on change
    private int drawnBoard=-1,drawnHull=-1;   // 1.1.0: same rule for the checkboxes
    public override void Build() {
        Text("백병전",7,20);
        Row(true,40,melee);
        Text("포격전",85,20);
        Row(false,118,cannon);
        Check(true,162,"백병전 즉시 돌입",out boardBox,out boardMark);
        Check(false,202,"함선 체력 고정",out hullBox,out hullMark);
    }
    private void Row(bool isMelee,float y,List<GGraph> faces) {
        for(int n=1;n<=5;n++) {
            int value=n;
            var button=new GComponent();button.SetXY(12+(n-1)*60,y);button.SetSize(56,34);
            Container!.AddChild(button);faces.Add(ButtonFace(button,"X"+n,56));
            Listen(button.onClick,c=>{Consume(c);BattleRuntime.Select(isMelee,value);});
        }
    }
    // Whole row is the click target; the box shows ✓ when on.
    private void Check(bool isBoard,float y,string title,out GGraph box,out GTextField mark) {
        var row=new GComponent{opaque=true};row.SetXY(12,y);row.SetSize(296,34);Container!.AddChild(row); // whole row clickable
        box=new GGraph{touchable=false};box.SetXY(0,3);row.AddChild(box);
        mark=new GTextField{touchable=false,autoSize=AutoSizeType.None,singleLine=true};
        var f=mark.textFormat;f.font=UIConfig.defaultFont;f.size=20;f.color=Foreground;f.align=AlignType.Center;mark.textFormat=f;
        mark.SetXY(0,3);mark.SetSize(28,28);row.AddChild(mark);
        var label=new GTextField{touchable=false,autoSize=AutoSizeType.None,singleLine=true};
        f=label.textFormat;f.font=UIConfig.defaultFont;f.size=18;f.color=Foreground;label.textFormat=f;
        label.SetXY(38,5);label.SetSize(250,28);label.text=title;row.AddChild(label);
        Listen(row.onTouchBegin,c=>Consume(c));
        Listen(row.onTouchEnd,c=>Consume(c));
        Listen(row.onClick,c=>{Consume(c);BattleRuntime.SetToggle(isBoard,!(isBoard?BattleRuntime.Toggle.Board:BattleRuntime.Toggle.Hull));});
    }
    public override void Refresh() {
        BattleRuntime.Update();SetActive(BattleRuntime.Active);
        int m=BattleRuntime.Choice.Melee,c=BattleRuntime.Choice.Cannon;
        if(drawnMelee!=m) { for(int n=0;n<5;n++) melee[n].DrawRect(56,34,1,Border,n+1==m?Accent:Surface); drawnMelee=m; }
        if(drawnCannon!=c) { for(int n=0;n<5;n++) cannon[n].DrawRect(56,34,1,Border,n+1==c?Accent:Surface); drawnCannon=c; }
        int b=BattleRuntime.Toggle.Board?1:0,h=BattleRuntime.Toggle.Hull?1:0;
        if(drawnBoard!=b) { Draw(boardBox,boardMark,b==1); drawnBoard=b; }
        if(drawnHull!=h) { Draw(hullBox,hullMark,h==1); drawnHull=h; }
    }
    private static void Draw(GGraph? box,GTextField? mark,bool on) {
        box?.DrawRect(28,28,1,Border,on?Accent:Surface);
        if(mark!=null)mark.text=on?"✓":"";
    }
    public override void Reset()=>BattleRuntime.ResetSession();
    protected override void ClearReferences(){melee.Clear();cannon.Clear();boardBox=hullBox=null;boardMark=hullMark=null;drawnMelee=drawnCannon=drawnBoard=drawnHull=-1;}
}

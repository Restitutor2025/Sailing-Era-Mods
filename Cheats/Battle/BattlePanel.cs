using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
namespace Restitutor.Cheats.Battle;

internal sealed class BattlePanel : Panel {
    public override string Id=>"battle";
    public override int Order=>19;
    public override float Height=>164;
    public override bool Visible=>Active;
    private readonly List<GGraph> melee=new(),cannon=new();
    private int drawnMelee=-1,drawnCannon=-1; // 1.0.1: redraw only on change
    public override void Build() {
        Text("백병전",7,20);
        Row(true,40,melee);
        Text("포격전",85,20);
        Row(false,118,cannon);
    }
    private void Row(bool isMelee,float y,List<GGraph> faces) {
        for(int n=1;n<=5;n++) {
            int value=n;
            var button=new GComponent();button.SetXY(12+(n-1)*60,y);button.SetSize(56,34);
            Container!.AddChild(button);faces.Add(ButtonFace(button,"X"+n,56));
            Listen(button.onClick,c=>{Consume(c);BattleRuntime.Select(isMelee,value);});
        }
    }
    public override void Refresh() {
        BattleRuntime.Update();SetActive(BattleRuntime.Active);
        int m=BattleRuntime.Choice.Melee,c=BattleRuntime.Choice.Cannon;
        if(drawnMelee!=m) { for(int n=0;n<5;n++) melee[n].DrawRect(56,34,1,Border,n+1==m?Accent:Surface); drawnMelee=m; }
        if(drawnCannon!=c) { for(int n=0;n<5;n++) cannon[n].DrawRect(56,34,1,Border,n+1==c?Accent:Surface); drawnCannon=c; }
    }
    public override void Reset()=>BattleRuntime.Reset();
    protected override void ClearReferences(){melee.Clear();cannon.Clear();drawnMelee=drawnCannon=-1;}
}

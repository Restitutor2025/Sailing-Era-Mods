using Il2CppFairyGUI;
using UnityEngine;
using Restitutor.Cheats.Interface;
namespace Restitutor.Cheats.Speed;
internal sealed class SpeedPanel : Panel {
    public override string Id=>"speed";
    public override int Order=>20;
    public override float Height=>132;
    public override bool Visible=>Active;
    private GTextField? label;
    private readonly List<GGraph> buttons=new();
    private int drawn=-1; private string? shownLabel; // 1.1.2: redraw only on change
    public override void Build() {
        Text("항해 · 이동 속도",7,20);
        label=Text("이동 속도만 변경",80,14);
        for(int value=1;value<=5;value++) {
            int choice=value;
            var b=new GComponent(); b.SetXY(12+(value-1)*60,40); b.SetSize(56,32); Container!.AddChild(b);
            buttons.Add(ButtonFace(b,"×"+value,56));
            Listen(b.onClick,c=>{Consume(c);SailingSpeed.Select(choice);});
        }
    }
    public override void Refresh() {
        SailingSpeed.Update(); SetActive(SailingSpeed.CanUse());
        string text=Active ? "이동 속도만 변경 · 시간/소모 유지" : SailingSpeed.Unavailable;
        if(!ReferenceEquals(text,shownLabel) && text!=shownLabel) { label!.text=text; shownLabel=text; }
        int m=SailingSpeed.Multiplier;
        if(drawn!=m) { for(int i=0;i<buttons.Count;i++) buttons[i].DrawRect(56,34,1,Border,i+1==m?Accent:Surface); drawn=m; }
    }
    public override void Reset()=>SailingSpeed.Reset();
    protected override void ClearReferences() { label=null; buttons.Clear(); drawn=-1; shownLabel=null; }
}

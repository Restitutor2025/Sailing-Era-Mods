using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
namespace Restitutor.Cheats.Skill;
internal sealed class SkillPanel:Panel {
    public override string Id=>"skill";
    public override int Order=>19;
    public override float Height=>150;
    public override bool Visible=>EntryPoint.InTavern();
    private GTextField? status,message;
    private int drawn;
    private readonly List<(int value,GGraph face)> buttons=new();
    internal void Message(string text){if(message!=null)message.text=text;}
    public override void Build() {
        Text("스킬 포인트 간격",7,20);status=Text("",42,16);
        float x=12;
        foreach(int value in Rules.Choices) {
            var b=new GComponent();b.SetXY(x,76);b.SetSize(92,34);Container!.AddChild(b);
            var face=ButtonFace(b,value==Rules.Default?$"Lv{value}(기본)":$"Lv{value}",92);
            int chosen=value;
            Listen(b.onClick,c=>{Consume(c);EntryPoint.Select(chosen);Refresh();});
            buttons.Add((value,face));x+=102;
        }
        message=Text("다음 레벨업부터 적용 · 이미 받은 포인트는 유지",116,14);
    }
    public override void Refresh() {
        SetActive(true);
        int current=Settings.Interval;
        if(drawn==current) return; // 1.0.4: status and buttons change only with the interval
        status!.text=$"현재: Lv{current}마다 1포인트";
        foreach(var (value,face) in buttons) face.DrawRect(92,34,1,Border,value==current?Accent:Surface);
        drawn=current;
    }
    public override void Reset(){}
    protected override void ClearReferences(){status=message=null;buttons.Clear();drawn=0;}
}

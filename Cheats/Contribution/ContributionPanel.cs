using Il2CppFairyGUI;
using Il2CppGyyx.Template;
using Il2CppClient.Utils;
using UnityEngine;
using Restitutor.Cheats.Interface;
namespace Restitutor.Cheats.Contribution;
internal sealed class ContributionPanel : Panel {
    public override string Id=>"contribution";
    public override int Order=>10;
    public override float Height=>174;
    public override bool Visible=>EntryPoint.IsInCity();
    private GTextInput? input;
    private GTextField? label,message;
    private GGraph? strike;
    private int? shown;
    private string? shownMessage; private int styled=-1; private (int id,int influence)? shownLabel; // 1.1.2
    internal void Message(string value) { if(message!=null && value!=shownMessage) { message.text=value; shownMessage=value; } }
    public override void Build() {
        Text("도시 · 공헌도",7,20); label=Text("현재 체류 도시 없음",43,18);
        var target=Text("목표값",84,17);target.SetSize(64,30);
        Rect(Container!,78,79,122,36,Surface);
        input=new GTextInput { singleLine=true,restrict=Rules.InputRestriction,maxLength=10,keyboardInput=true,editable=true };
        var f=input.textFormat; f.font=UIConfig.defaultFont; f.size=21; f.color=Foreground; input.textFormat=f;
        input.SetXY(84,82); input.SetSize(110,30); Container!.AddChild(input);
        var button=new GComponent(); button.SetXY(212,80); button.SetSize(96,34); Container.AddChild(button);
        ButtonFace(button,"적용",96).DrawRect(96,34,1,Border,Accent);
        strike=new GGraph { touchable=false,visible=false };
        strike.SetXY(78,97); strike.DrawRect(122,1,0,Foreground,Foreground); Container.AddChild(strike);
        message=Text("0~1000 · 적용 버튼으로 확정",124,15);
        Listen(button.onClick,c=>{Consume(c);EntryPoint.Apply(shown,input.text);});
        Listen(input.onChanged,c=>{string current=input.text;string clamped=Rules.ClampInput(current);if(current!=clamped) input.text=clamped;});
        Listen(input.onFocusOut,Consume);
    }
    public override void Refresh() {
        int? id=EntryPoint.Resolve(); bool wasActive=Active; SetActive(id.HasValue);
        if(styled!=(Active?1:0)) {
            strike!.visible=!Active; input!.editable=Active;
            var f=input.textFormat; f.strikethrough=!Active; input.textFormat=f;
            styled=Active?1:0;
        }
        if(!Active) { Message(EntryPoint.Unavailable); return; }
        if(!wasActive) Message("0~1000 · 적용 버튼으로 확정");
        if(shown!=id) { shown=id; input!.text=""; Message("도시 변경 · 목표값을 입력하세요."); }
        var port=EntryPoint.Player!.WorldPort.GetPortData(id!.Value);
        if(port==null) { if(shownLabel!=(id.Value,int.MinValue)) { label!.text="도시 데이터 없음"; shownLabel=(id.Value,int.MinValue); } return; }
        int influence=port.Influence;
        if(shownLabel==(id.Value,influence)) return;
        var tpl=TemplateManager.GetPort(id.Value);
        string name=TextLibUtils.Text(tpl.name,tpl.name);
        label!.text=$"{name}  |  현재 {influence}"; shownLabel=(id.Value,influence);
    }
    public override void Reset() { EntryPoint.ResetState(); shown=null; }
    protected override void ClearReferences() { input=null; label=message=null; strike=null; shown=null; shownMessage=null; styled=-1; shownLabel=null; }
}

using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
namespace Restitutor.Cheats.Bargirls;
internal sealed class BargirlsPanel:Panel {
    public override string Id=>"bargirls";
    public override int Order=>18;
    public override float Height=>172;
    // 1.1.2: hidden (not struck through) unless inside a tavern that has a barmaid.
    public override bool Visible=>shown!=null;
    private GTextField? title,status,message;
    private GComponent? button;
    private EntryPoint.Target? shown;
    private string blocked="";
    private string? shownMessage,shownStatus; private int styled=-1; // 1.1.3: write UI only on change
    internal void Message(string text){if(message!=null && text!=shownMessage){message.text=text;shownMessage=text;}}
    public override void Build(){
        title=Text("여급 호감도",7,20);status=Text("",42,16);message=Text("",122,14);
        button=new GComponent();button.SetXY(12,80);button.SetSize(296,34);Container!.AddChild(button);
        ButtonFace(button,"상승",296);
        Listen(button.onClick,c=>{Consume(c);EntryPoint.Apply(shown);Refresh();});
    }
    public override void Refresh(){
        var previous=shown;shown=EntryPoint.Resolve();
        string reason=EntryPoint.Reason;
        bool allowed=shown!=null && EntryPoint.CanRaise(shown,out _,out reason);
        SetActive(allowed);
        if(styled!=(Active?1:0)) { var f=title!.textFormat;f.strikethrough=!Active;title.textFormat=f; styled=Active?1:0; }
        int favor=shown?.Data?.Favorability??0;
        int stage=shown==null?0:Rules.Stage(favor,shown.First,shown.Second,shown.Third);
        string st=shown==null?EntryPoint.Reason:$"현재 호감도 {stage}단계 · {favor}";
        if(st!=shownStatus) { status!.text=st; shownStatus=st; }
        if(!allowed)Message(reason);
        else if(blocked!="" || previous?.Player!=shown?.Player || previous?.City!=shown?.City || previous?.Role!=shown?.Role)Message("클릭하면 한 단계 상승합니다");
        blocked=allowed?"":reason;
        if(button!.touchable!=allowed) { button.touchable=allowed;button.alpha=allowed?1f:.35f; }
    }
    public override void Reset(){shown=null;blocked="";}
    protected override void ClearReferences(){shown=null;title=status=message=null;button=null;blocked="";shownMessage=shownStatus=null;styled=-1;}
}

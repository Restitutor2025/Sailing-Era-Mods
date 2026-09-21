using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
namespace Restitutor.Cheats.Money;
internal sealed class MoneyPanel:Panel {
    public override string Id=>"money";
    public override int Order=>15;
    public override float Height=>174;
    private GTextInput? input;
    private GTextField? label,message;
    private IntPtr shownPlayer;
    private string? shownMessage; private long shownAmount=long.MinValue; private int editable=-1; // 1.0.1
    internal void Message(string text) { if(message!=null && text!=shownMessage){message.text=text;shownMessage=text;} }
    public override void Build() {
        Text("소지금 변경",7,20);label=Text("데이터 준비 중",43,18);
        var target=Text("목표값",84,17);target.SetSize(64,30);
        Rect(Container!,78,79,152,36,Surface);
        input=new GTextInput { singleLine=true,restrict="[0-9]",maxLength=10,keyboardInput=true,editable=true };
        var f=input.textFormat;f.font=UIConfig.defaultFont;f.size=19;f.color=Foreground;input.textFormat=f;
        input.SetXY(84,82);input.SetSize(140,30);Container!.AddChild(input);
        var button=new GComponent();button.SetXY(238,80);button.SetSize(70,34);Container.AddChild(button);
        ButtonFace(button,"적용",70).DrawRect(70,34,1,Border,Accent);
        message=Text(Rules.Hint,124,14);
        Listen(button.onClick,c=>{Consume(c);EntryPoint.Apply(shownPlayer,input.text);});
        Listen(input.onChanged,c=>{string text=input.text;string clamped=Rules.ClampInput(text);if(text!=clamped)input.text=clamped;});
        Listen(input.onFocusOut,Consume);
    }
    public override void Refresh() {
        var currency=EntryPoint.Resolve();bool wasActive=Active;SetActive(currency!=null);
        if(editable!=(Active?1:0)) { input!.editable=Active; editable=Active?1:0; }
        if(!Active) { if(shownAmount!=long.MinValue+1) { label!.text="데이터 준비 중"; shownAmount=long.MinValue+1; } Message("저장 로드·장면 전환 후 사용 가능");return;}
        var player=Host.Player!.Pointer;
        if(shownPlayer!=player) {shownPlayer=player;input!.text="";Message(Rules.Hint);}
        else if(!wasActive) Message(Rules.Hint);
        long amount=currency!.GetCoinCurrencyAmount();
        if(amount!=shownAmount) { label!.text=$"현재 소지금  {amount:N0}"; shownAmount=amount; }
    }
    public override void Reset() {shownPlayer=IntPtr.Zero;if(input!=null)input.text="";}
    protected override void ClearReferences() {input=null;label=message=null;shownPlayer=IntPtr.Zero;shownMessage=null;shownAmount=long.MinValue;editable=-1;}
}

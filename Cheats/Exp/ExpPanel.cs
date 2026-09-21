using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
namespace Restitutor.Cheats.Exp;
internal sealed class ExpPanel:Panel {
    public override string Id=>"exp";
    public override int Order=>16;
    public override float Height=>206;
    // Hidden entirely (not struck through) outside the tavern screen.
    public override bool Visible=>EntryPoint.InTavern();
    private GTextInput? input;
    private GTextField? label,message;
    private GComponent? maxButton;
    private IntPtr shownPlayer;
    private string? shownMessage; private long shownAmount=long.MinValue; private int editable=-1; // 1.0.1
    internal void Message(string text) { if(message!=null && text!=shownMessage){message.text=text;shownMessage=text;} }
    public override void Build() {
        Text("경험치 변경",7,20);label=Text("",43,18);
        var target=Text("목표값",84,17);target.SetSize(64,30);
        Rect(Container!,78,79,152,36,Surface);
        input=new GTextInput { singleLine=true,restrict="[0-9]",maxLength=10,keyboardInput=true,editable=true };
        var f=input.textFormat;f.font=UIConfig.defaultFont;f.size=19;f.color=Foreground;input.textFormat=f;
        input.SetXY(84,82);input.SetSize(140,30);Container!.AddChild(input);
        var apply=new GComponent();apply.SetXY(238,80);apply.SetSize(70,34);Container.AddChild(apply);
        ButtonFace(apply,"적용",70).DrawRect(70,34,1,Border,Accent);
        maxButton=new GComponent();maxButton.SetXY(12,122);maxButton.SetSize(296,34);Container.AddChild(maxButton);
        ButtonFace(maxButton,"Max (2,147,483,647)",296);
        message=Text(Rules.Hint,164,14);
        Listen(apply.onClick,c=>{
            Consume(c);
            string text=Rules.ClampInput(input.text);input.text=text;
            if(!Rules.TryTarget(text,out int value)) { Message(Rules.Hint);return; }
            EntryPoint.Apply(shownPlayer,value);Refresh();
        });
        Listen(maxButton.onClick,c=>{Consume(c);input.text="";EntryPoint.Apply(shownPlayer,Rules.Max);Refresh();});
        Listen(input.onChanged,c=>{string text=input.text;string clamped=Rules.ClampInput(text);if(text!=clamped)input.text=clamped;});
        Listen(input.onFocusOut,Consume);
    }
    public override void Refresh() {
        if(!EntryPoint.InTavern()) return; // Host hides the panel via Visible.
        var data=Host.Player?.PlayerCurrency?.FindCurrency(Rules.CurrencyId);
        bool wasActive=Active;SetActive(data!=null);
        if(editable!=(Active?1:0)) { input!.editable=Active; editable=Active?1:0; }
        if(data==null) { if(shownAmount!=long.MinValue+1) { label!.text="경험치 기록 없음"; shownAmount=long.MinValue+1; } Message("경험치를 한 번 획득한 뒤 사용 가능");return; }
        var player=Host.Player!.Pointer;
        if(shownPlayer!=player) { shownPlayer=player;input!.text="";Message(Rules.Hint); }
        else if(!wasActive) Message(Rules.Hint);
        long amount=data.Amount;
        if(amount!=shownAmount) {
            label!.text=$"현재 경험치  {amount:N0}";
            bool atMax=amount==Rules.Max;
            maxButton!.touchable=!atMax;maxButton.alpha=atMax?.35f:1f;
            shownAmount=amount;
        }
    }
    public override void Reset() { shownPlayer=IntPtr.Zero;if(input!=null)input.text=""; }
    protected override void ClearReferences() { input=null;label=message=null;maxButton=null;shownPlayer=IntPtr.Zero;shownMessage=null;shownAmount=long.MinValue;editable=-1; }
}

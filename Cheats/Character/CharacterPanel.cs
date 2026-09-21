using Il2CppFairyGUI;
using Il2CppClient.PlayerStore;
using Restitutor.Cheats.Interface;
using UnityEngine;
namespace Restitutor.Cheats.Character;

internal sealed class CharacterPanel:Panel {
    public override string Id=>"character";
    public override int Order=>17;
    public override float Width=>Rules.PanelWidth; // Interface 1.4.0 widens the window while this panel is shown.
    private float height=Rules.Height(0);
    public override float Height=>height;
    // Hidden entirely (not struck through) outside the Tab character sheet.
    public override bool Visible=>EntryPoint.Current()!=null;

    private static readonly Color Navy=new(.035f,.075f,.10f,.97f); // Interface Theme.Navy (panel background)
    private sealed class Row {
        internal GTextField Value=null!; internal GTextInput Input=null!;
        internal Field? Field; internal int SkillId; internal string Label="";
        internal long Shown=long.MinValue; // 1.1.1: last displayed value pair (packed), text rewritten only on change
    }
    private (IntPtr role,int level)? shownTitle;
    private readonly List<Row> rows=new();
    private GComponent? body;
    private GTextField? title,message;
    private IntPtr builtRole; private string builtSkills="";
    private IntPtr shownRole;

    internal void Message(string text){ if(message!=null) message.text=text; }

    private GTextField Label(GComponent parent,string text,float x,float y,float w,int size,bool right=false) {
        var t=new GTextField{touchable=false,autoSize=AutoSizeType.None,singleLine=true};
        var f=t.textFormat;f.font=UIConfig.defaultFont;f.size=size;f.color=Foreground;f.align=right?AlignType.Right:AlignType.Left;t.textFormat=f;
        t.SetXY(x,y);t.SetSize(w,28);t.text=text;parent.AddChild(t);return t;
    }
    private static string Fit(string s,int max)=>s.Length<=max?s:s.Substring(0,max-1)+"…";

    public override void Build() {
        Text("인물 수정",7,20);
        title=Text("",38,16);title.SetSize(Rules.PanelWidth-24,28);
        body=new GComponent{name="CharacterRows"};body.SetXY(0,Rules.RowsTop);body.SetSize(Rules.PanelWidth,10);Container!.AddChild(body);
        message=Text("",height-Rules.Footer+4,14);message.SetSize(Rules.PanelWidth-24,30);
        builtRole=IntPtr.Zero;builtSkills="";
    }

    private Row AddRow(float x,float y,string label,Field? field,int skillId) {
        var row=new Row{Field=field,SkillId=skillId,Label=label};
        Label(body!,Fit(label,7),x+12,y+4,90,15);
        row.Value=Label(body!,"",x+102,y+5,84,14,true);
        Rect(body!,x+192,y+2,62,30,Surface);
        var input=new GTextInput{singleLine=true,restrict="[0-9]",maxLength=Rules.Digits,keyboardInput=true,editable=true};
        var f=input.textFormat;f.font=UIConfig.defaultFont;f.size=16;f.color=Foreground;input.textFormat=f;
        input.SetXY(x+196,y+4);input.SetSize(54,26);body!.AddChild(input);row.Input=input;
        var apply=new GComponent();apply.SetXY(x+260,y);apply.SetSize(48,34);body.AddChild(apply);
        ButtonFace(apply,"적용",48).DrawRect(48,34,1,Border,Accent);
        Listen(apply.onClick,c=>{Consume(c);Submit(row);});
        Listen(input.onChanged,c=>{string t=input.text;string d=new(t.Where(ch=>ch>='0'&&ch<='9').ToArray());if(d!=t)input.text=d;});
        Listen(input.onFocusOut,Consume);
        rows.Add(row);return row;
    }

    private void Submit(Row row) {
        var t=EntryPoint.Current();
        if(t==null || t.Role.Pointer!=shownRole) { Message("대상이 바뀌었습니다. 다시 적용하세요.");return; }
        int min,max;
        if(row.Field is Field f) (min,max)=EntryPoint.Limits(t.Role,f);
        else { var s=t.Role.GetRoleSkillById(row.SkillId); if(s==null){Message("스킬을 찾을 수 없습니다.");return;} min=0;max=s.MaxLevel; }
        if(!Rules.TryTarget(row.Input.text,min,max,out int target,out bool clamped)) { Message($"{row.Label}: 숫자를 입력하세요 ({min:N0}~{max:N0})");return; }
        row.Input.text="";
        if(row.Field is Field field) EntryPoint.ApplyField(shownRole,field,target,row.Label);
        else EntryPoint.ApplySkill(shownRole,row.SkillId,target,row.Label);
        if(clamped) message!.text+=$" (범위 {min:N0}~{max:N0}로 보정)";
    }

    private static string SkillKey(PlayerRoleData role) {
        var list=role.Skills; if(list==null) return "";
        var ids=new int[list.Count];for(int i=0;i<list.Count;i++)ids[i]=list[i]?.Id??0;
        return string.Join(",",ids);
    }

    private void Rebuild(Target t,string key) {
        if(body==null) return;
        if(body.numChildren>0) body.RemoveChildren(0,-1,true);
        rows.Clear();
        var owned=new List<RoleSkillData>();
        var skills=t.Role.Skills;
        if(skills!=null) for(int i=0;i<skills.Count;i++) { var s=skills[i]; if(s!=null) owned.Add(s); }
        int n=owned.Count,slot=0;
        foreach(Field f in Enum.GetValues<Field>()) { var (x,y)=Rules.Slot(slot++,n);AddRow(x,y,EntryPoint.FieldLabel(f),f,0); }
        { var (x,y)=Rules.Slot(slot++,n);Label(body,"스킬 (기본 Lv / 최대 Lv)",x+12,y+6,Rules.ColumnWidth-12,15); }
        foreach(var s in owned) { var (x,y)=Rules.Slot(slot++,n);AddRow(x,y,EntryPoint.SkillName(s),null,s.Id); }
        if(n==0) { var (x,y)=Rules.Slot(slot,n);Label(body,"보유 스킬 없음",x+12,y+6,Rules.ColumnWidth-12,15); }
        height=Rules.Height(n);
        body.SetSize(Rules.PanelWidth,height-Rules.RowsTop-Rules.Footer);
        Container!.SetSize(Rules.PanelWidth,height);
        // Mount draws the background as the first child with the mount-time height; redraw for the new height.
        Container.GetChildAt(0)?.TryCast<GGraph>()?.DrawRect(Rules.PanelWidth,height,1,Border,Navy);
        message!.y=height-Rules.Footer+4;
        builtRole=t.Role.Pointer;builtSkills=key;
    }

    public override void Refresh() {
        var t=EntryPoint.Current();
        if(t==null) return; // Host hides the panel via Visible.
        var role=t.Role;
        string key=SkillKey(role);
        if(builtRole!=role.Pointer || builtSkills!=key) {
            if(shownRole!=role.Pointer) Message("");
            Rebuild(t,key);
        }
        shownRole=role.Pointer;
        int level=role.Level;
        if(shownTitle!=(role.Pointer,level)) { title!.text=$"{Fit(EntryPoint.RoleName(role),14)}  ·  Lv{level}"; shownTitle=(role.Pointer,level); }
        foreach(var row in rows) {
            if(row.Field is Field f) {
                int b=EntryPoint.Base(role,f),s=EntryPoint.Shown(role,f);
                long shownKey=((long)b<<32)|(uint)s;
                if(shownKey!=row.Shown) { row.Value.text=b==s?$"{b:N0}":$"{b:N0}({s:N0})"; row.Shown=shownKey; }
            } else {
                var s=role.GetRoleSkillById(row.SkillId);
                long shownKey=s==null?long.MaxValue:((long)s.LevelBase<<32)|(uint)s.MaxLevel;
                if(shownKey!=row.Shown) { row.Value.text=s==null?"-":$"{s.LevelBase}/{s.MaxLevel}"; row.Shown=shownKey; }
            }
        }
    }
    public override void Reset() { shownRole=IntPtr.Zero;builtRole=IntPtr.Zero;builtSkills="";Message(""); }
    protected override void ClearReferences() { rows.Clear();body=null;title=message=null;builtRole=IntPtr.Zero;builtSkills="";shownTitle=null; }
}

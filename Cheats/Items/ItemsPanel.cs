using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
using UnityEngine;
namespace Restitutor.Cheats.Items;

internal sealed class ItemsPanel:Panel {
    public override string Id=>"items";
    public override int Order=>22;
    public override float Width=>Rules.PanelWidth;   // Interface 1.4.0
    public override float Height=>Rules.Height;
    // Hidden entirely outside the Tab character sheet.
    public override bool Visible=>EntryPoint.CharacterSheet()!=null;

    private Tab tab=Tab.Equipment;
    private int page;
    private string query="";
    private List<Entry> filtered=new();
    private bool dirty=true;
    private GTextInput? search;
    private GTextField? pageText,message;
    private GComponent? rowsRoot;
    private readonly List<(Tab tab,GComponent button,GGraph face)> tabs=new();
    private GComponent? fruitTab;

    internal void Message(string text){ if(message!=null) message.text=text; }

    private GTextField Label(GComponent parent,string text,float x,float y,float w,int size,bool right=false) {
        var t=new GTextField{touchable=false,autoSize=AutoSizeType.None,singleLine=true};
        var f=t.textFormat;f.font=UIConfig.defaultFont;f.size=size;f.color=Foreground;f.align=right?AlignType.Right:AlignType.Left;t.textFormat=f;
        t.SetXY(x,y);t.SetSize(w,28);t.text=text;parent.AddChild(t);return t;
    }
    private GComponent Button(GComponent parent,string title,float x,float y,float w,Action action,out GGraph face) {
        var b=new GComponent();b.SetXY(x,y);b.SetSize(w,34);parent.AddChild(b);
        face=ButtonFace(b,title,w);
        Listen(b.onClick,c=>{Consume(c);action();});
        return b;
    }
    private static string Fit(string s,int max)=>s.Length<=max?s:s.Substring(0,max-1)+"…";

    public override void Build() {
        Text("아이템 추가",7,20);
        float x=12;
        foreach(var (t,title) in new[]{(Tab.Equipment,"장비"),(Tab.Book,"도서"),(Tab.DevilFruit,"악마의 열매")}) {
            var tt=t;var b=Button(Container!,title,x,Rules.TabsTop,t==Tab.DevilFruit?130:90,()=>Select(tt),out var face);
            tabs.Add((t,b,face));if(t==Tab.DevilFruit) fruitTab=b;
            x+=(t==Tab.DevilFruit?130:90)+8;
        }
        Label(Container!,"검색",12,Rules.SearchTop+4,50,17);
        Rect(Container!,64,Rules.SearchTop,300,34,Surface);
        search=new GTextInput{singleLine=true,maxLength=30,keyboardInput=true,editable=true};
        var f=search.textFormat;f.font=UIConfig.defaultFont;f.size=17;f.color=Foreground;search.textFormat=f;
        search.SetXY(70,Rules.SearchTop+3);search.SetSize(288,28);Container!.AddChild(search);search.text=query;
        Listen(search.onChanged,c=>{query=search.text;page=0;dirty=true;});
        Listen(search.onFocusOut,Consume);
        Button(Container!,"지우기",372,Rules.SearchTop,70,()=>{query="";if(search!=null)search.text="";page=0;dirty=true;},out _);
        rowsRoot=new GComponent{name="ItemRows"};rowsRoot.SetXY(0,Rules.ListTop);rowsRoot.SetSize(Rules.PanelWidth,Rules.PerColumn*Rules.RowHeight);Container!.AddChild(rowsRoot);
        float py=Rules.ListTop+Rules.PerColumn*Rules.RowHeight+3;
        Button(Container!,"◀",12,py,60,()=>{page--;dirty=true;},out _);
        pageText=Label(Container!,"",80,py+4,480,16);var ptf=pageText.textFormat;ptf.align=AlignType.Center;pageText.textFormat=ptf;
        Button(Container!,"▶",568,py,60,()=>{page++;dirty=true;},out _);
        message=Text("",Rules.Height-Rules.Footer+4,14);message.SetSize(Rules.PanelWidth-24,30);
        dirty=true;
    }
    private void Select(Tab t){ tab=t;page=0;dirty=true;Message(""); }

    private void Rebuild() {
        if(rowsRoot==null) return;
        if(rowsRoot.numChildren>0) rowsRoot.RemoveChildren(0,-1,true);
        var all=EntryPoint.Catalog(tab);
        filtered=Rules.Filter(all,query);
        page=Rules.ClampPage(page,filtered.Count);
        int i=0;
        foreach(var e in Rules.PageOf(filtered,page)) {
            var (x,y)=Rules.Slot(i++);var entry=e;
            Label(rowsRoot,Fit(e.Name,11),x+12,y+4,170,15);
            Label(rowsRoot,Fit(e.TypeName,5),x+182,y+5,70,13);
            Button(rowsRoot,"추가",x+258,y,50,()=>EntryPoint.Add(entry),out var face);
            face.DrawRect(50,34,1,Border,Accent);
        }
        if(filtered.Count==0) Label(rowsRoot,all.Count==0?"표시할 아이템이 없습니다.":"검색 결과 없음",12,4,400,15);
        pageText!.text=$"{page+1} / {Rules.Pages(filtered.Count)}   ·   {filtered.Count}개";
        foreach(var (t,_,face) in tabs) face.DrawRect(t==Tab.DevilFruit?130:90,34,1,Border,t==tab?Accent:Surface);
        // Item table not loaded yet: try again next frame instead of caching an empty list.
        dirty=all.Count==0 && tab!=Tab.DevilFruit;
    }
    public override void Refresh() {
        if(EntryPoint.CharacterSheet()==null) return; // Host hides the panel via Visible.
        // The fruit tab exists only while Restitutor_devil_fruits has registered at least one fruit item.
        bool fruits=EntryPoint.FruitsRegistered();
        if(fruitTab!=null && fruitTab.visible!=fruits) { fruitTab.visible=fruits; if(!fruits && tab==Tab.DevilFruit) Select(Tab.Equipment); dirty=true; }
        if(dirty) Rebuild();
    }
    public override void Reset() { page=0;dirty=true;Message(""); }
    protected override void ClearReferences() { search=null;pageText=message=null;rowsRoot=null;fruitTab=null;tabs.Clear();filtered=new();dirty=true; }
}

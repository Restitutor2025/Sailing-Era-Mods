using System.Reflection;
using Restitutor.Cheats.Interface;
using Il2CppFairyGUI;
using Il2CppClient.PlayerStore;
int checks=0;
void Check(bool value){checks++;if(!value)throw new Exception("Failed lifecycle check "+checks);}
object? Call(string name,params object[] args)=>typeof(Host).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,args);
// Test only managed hosting; native initialization is audited separately.
typeof(Host).GetField("<Enabled>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,true);
typeof(Host).GetField("log",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,new MelonLoader.MelonLogger.Instance());
var host=new Host(); var speed=new TestPanel("speed",20,112); var contribution=new TestPanel("contribution",10,218);
Host.Register(speed); Host.Register(contribution);
host.OnUpdate(); Check(speed.Container==null); Check(contribution.Container==null);
Call("Loaded",new PlayerData()); host.OnUpdate();
Check(GRoot.inst.Children.Single().height==48);
Check(!speed.Container!.visible && !contribution.Container!.visible);
((WindowState)typeof(Host).GetField("window",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!).Toggle();
host.OnUpdate();
Check(contribution.Container!.y==0); Check(speed.Container!.y==224);
Check(speed.Container.parent!.height==342); Check(GRoot.inst.Children.Count==1);
bool duplicate=false;try{Host.Register(new TestPanel("speed",30,20));}catch(InvalidOperationException){duplicate=true;}Check(duplicate);
GRoot.inst.focus=speed.Container; Check((bool)Call("Capture")! ==false);
contribution.Enable=false; host.OnUpdate(); Check(GRoot.inst.focus==speed.Container);
speed.Enable=false; host.OnUpdate(); Check(GRoot.inst.focus==null); Check((bool)Call("Capture")!);
speed.Enable=true; contribution.Enable=true; host.OnUpdate();
var sameView=speed.Container;
GRoot.inst.focus=contribution.Container;
contribution.Show=false;host.OnUpdate();
Check(!contribution.Container!.visible);Check(GRoot.inst.focus==null);
Check(speed.Container!.y==0);Check(speed.Container.parent!.height==118);
Check(speed.Container==sameView);Check(contribution.Resets==0);
contribution.Show=true;host.OnUpdate();Check(contribution.Container.visible);Check(speed.Container.y==224);
var old=speed.Container; Host.Unregister(speed); Check(old!.isDisposed);Check(speed.Resets==1);
host.OnUpdate(); Check(contribution.Container!.parent!.height==224);Check(GRoot.inst.Children.Count==1);
Host.Register(speed); host.OnUpdate();Check(speed.Container!.y==224);Check(GRoot.inst.Children.Count==1);
for(int i=0;i<5;i++){host.OnUpdate();Check(GRoot.inst.Children.Count==1);Check(speed.Container.Children.Count==2);}
speed.Fail=true; host.OnUpdate(); Check(speed.Container==null);Check(contribution.Container!=null);Check(Host.Player!=null);
speed.Fail=false;host.OnUpdate();Check(speed.Container!=null);
var originalRoot=GRoot.inst; GRoot.inst=new GRoot{width=800,height=600}; host.OnUpdate();
Check(originalRoot.Children.Count==0);Check(GRoot.inst.Children.Count==1);Check(Host.Player!=null);
Call("ResetSession");Check(Host.Player==null);Check(speed.Container==null);Check(contribution.Container==null);Check(GRoot.inst.Children.Count==0);
Call("Loaded",new PlayerData());host.OnUpdate();Check(speed.Container!=null);Check(contribution.Container!=null);
Host.Unregister(contribution);host.OnUpdate();Check(speed.Container!.y==0);Check(speed.Container.parent!.height==118);
GObject Find(GComponent c,string name){foreach(var child in c.Children){if(child.name==name)return child;if(child is GComponent cc){try{return Find(cc,name);}catch(KeyNotFoundException){}}}throw new KeyNotFoundException(name);}
Check(speed.Container!.visible); // A session reset preserves the toggle.
var future=new TestPanel("future-any-module",25,900);Host.Register(future);host.OnUpdate();
Check(future.Container!=null);Check(Find(GRoot.inst,"CheatScrollbar").visible);
var drag=Find(GRoot.inst,"CheatTitleDrag");drag.onTouchBegin.Fire(10,10);drag.onTouchMove.Fire(250,80);drag.onTouchEnd.Fire();
var moved=GRoot.inst.Children.Single();Check(moved.x>12);float movedX=moved.x;
Find(GRoot.inst,"CheatMinimize").onClick.Fire();host.OnUpdate();
Check(!speed.Container!.visible&&!future.Container!.visible);Check(moved.height==48);Check((bool)Call("Capture")! ==false);
Call("ResetSession");Call("Loaded",new PlayerData());host.OnUpdate();Check(GRoot.inst.Children.Single().height==48);Check(GRoot.inst.Children.Single().x==movedX);
Find(GRoot.inst,"CheatMinimize").onClick.Fire();host.OnUpdate();
Check(speed.Container!.visible&&future.Container!.visible);
var bar=Find(GRoot.inst,"CheatScrollbar");bar.onTouchBegin.Fire();bar.onTouchMove.Fire(0,10000);bar.onTouchEnd.Fire();
Check(speed.Container.parent!.y<0);
// 1.4.0: mouse wheel scrolls; wheel up returns to the top, wheel down moves again.
var wheel=GRoot.inst.Children.Single().displayObject!.onMouseWheel;
for(int i=0;i<200;i++)wheel.Wheel(-1);Check(speed.Container.parent!.y==0);
wheel.Wheel(1);Check(speed.Container.parent!.y==-60);wheel.Wheel(3);Check(speed.Container.parent!.y==-120);
// 1.4.0: window width follows the widest visible panel; hidden/removed wide panel restores 344.
Check(GRoot.inst.Children.Single().width==344);
var wide=new WidePanel("wide",26,100);Host.Register(wide);host.OnUpdate();
Check(GRoot.inst.Children.Single().width==664);Check(wide.Container!.width==640);Check(speed.Container!.width==320);
Check(Find(GRoot.inst,"CheatClose").x==615);Check(Find(GRoot.inst,"CheatScrollbar").x==651);
wide.Show=false;host.OnUpdate();Check(GRoot.inst.Children.Single().width==344);Check(Find(GRoot.inst,"CheatClose").x==295);
Host.Unregister(wide);host.OnUpdate();Check(GRoot.inst.Children.Single().width==344);
Host.Unregister(future);host.OnUpdate();Check(!Find(GRoot.inst,"CheatScrollbar").visible);Check(speed.Container.parent!.y==0);
var scene=Il2CppCore.SceneSystem.SceneManager.Instance;
var player=Host.Player!;
GRoot.inst.focus=speed.Container;
scene.IsInHarborScene=false;scene.IsInOceanScene=true;player.PlayerPort.IsStayInPort=false;
host.OnUpdate();Check(GRoot.inst.Children.Single().height>48);Check(GRoot.inst.focus==null);
Check(speed.Container!.visible);
host.OnUpdate();Check(speed.Container.visible); // Tab/pause do not change location.
scene.IsInLoadingOrStarting=true;host.OnUpdate();
scene.IsInLoadingOrStarting=false;scene.IsInHarborScene=true;scene.IsInOceanScene=false;player.PlayerPort.IsStayInPort=true;
host.OnUpdate();Check(GRoot.inst.Children.Single().height>48);
Check(speed.Container.visible);
player.PlayerPort.StayInPortId=2;host.OnUpdate();Check(GRoot.inst.Children.Single().height>48);
Find(GRoot.inst,"CheatMinimize").onClick.Fire();host.OnUpdate();Check(!speed.Container.visible);
player.PlayerPort.StayInPortId=3;host.OnUpdate();Check(!speed.Container.visible);
var keyboard=UnityEngine.InputSystem.Keyboard.current!;
keyboard.hKey.isPressed=true;host.OnUpdate();Check(speed.Container!.visible);
host.OnUpdate();Check(speed.Container.visible); // Holding H toggles only once.
keyboard.hKey.isPressed=false;host.OnUpdate();keyboard.hKey.isPressed=true;host.OnUpdate();Check(!speed.Container.visible);
keyboard.hKey.isPressed=false;host.OnUpdate();
GRoot.inst.focus=new GTextInput();keyboard.hKey.isPressed=true;host.OnUpdate();Check(!speed.Container.visible);
GRoot.inst.focus=null;host.OnUpdate();Check(!speed.Container.visible); // No delayed toggle after typing.
keyboard.hKey.isPressed=false;host.OnUpdate();UnityEngine.Application.isFocused=false;
keyboard.hKey.isPressed=true;host.OnUpdate();Check(!speed.Container.visible);
UnityEngine.Application.isFocused=true;host.OnUpdate();Check(!speed.Container.visible);
keyboard.hKey.isPressed=false;host.OnUpdate();keyboard.hKey.isPressed=true;host.OnUpdate();Check(speed.Container.visible);
keyboard.hKey.isPressed=false;host.OnUpdate();
Call("ResetSession");Call("Loaded",new PlayerData());host.OnUpdate();Check(speed.Container!.visible);
Find(GRoot.inst,"CheatClose").onClick.Fire();host.OnUpdate();Check(!GRoot.inst.Children.Single().visible);
Call("ResetSession");Call("Loaded",new PlayerData());host.OnUpdate();Check(!GRoot.inst.Children.Single().visible);
var later=new TestPanel("later",30,100);Host.Register(later);host.OnUpdate();Check(!GRoot.inst.Children.Single().visible);Host.Unregister(later);
keyboard.hKey.isPressed=true;host.OnUpdate();Check(GRoot.inst.Children.Single().visible);Check(speed.Container!.visible);
keyboard.hKey.isPressed=false;host.OnUpdate();
// 1.5.0 O: all cheats off/on. Off hides the window, resets every panel once, releases input; H ignored.
int resetsBefore=speed.Resets;
keyboard.oKey.isPressed=true;host.OnUpdate();Check(!Host.CheatsOn);Check(GRoot.inst.Children.Count==0);
Check(speed.Resets==resetsBefore+1);Check(contribution.Resets>0);Check((bool)Call("Capture")!);
host.OnUpdate();Check(!Host.CheatsOn);Check(speed.Resets==resetsBefore+1); // Holding O toggles once, resets once.
keyboard.oKey.isPressed=false;host.OnUpdate();
keyboard.hKey.isPressed=true;host.OnUpdate();Check(GRoot.inst.Children.Count==0);keyboard.hKey.isPressed=false;host.OnUpdate();
player.PlayerPort.StayInPortId=4;host.OnUpdate();Check(!Host.CheatsOn);Check(GRoot.inst.Children.Count==0); // location change keeps off
Call("ResetSession");Call("Loaded",new PlayerData());host.OnUpdate();Check(!Host.CheatsOn);Check(GRoot.inst.Children.Count==0); // save load keeps off
GRoot.inst.focus=new GTextInput();keyboard.oKey.isPressed=true;host.OnUpdate();Check(!Host.CheatsOn);
GRoot.inst.focus=null;host.OnUpdate();Check(!Host.CheatsOn); // no delayed toggle after typing
keyboard.oKey.isPressed=false;host.OnUpdate();UnityEngine.Application.isFocused=false;
keyboard.oKey.isPressed=true;host.OnUpdate();Check(!Host.CheatsOn);
UnityEngine.Application.isFocused=true;keyboard.oKey.isPressed=false;host.OnUpdate();
keyboard.oKey.isPressed=true;host.OnUpdate();Check(Host.CheatsOn);Check(GRoot.inst.Children.Single().visible);Check(speed.Container!.visible); // fold state kept
keyboard.oKey.isPressed=false;host.OnUpdate();
Find(GRoot.inst,"CheatClose").onClick.Fire();host.OnUpdate();Check(!GRoot.inst.Children.Single().visible);
keyboard.oKey.isPressed=true;host.OnUpdate();Check(!Host.CheatsOn);keyboard.oKey.isPressed=false;host.OnUpdate();
keyboard.oKey.isPressed=true;host.OnUpdate();Check(Host.CheatsOn);Check(GRoot.inst.Children.Single().visible); // O-on also undoes X
keyboard.oKey.isPressed=false;host.OnUpdate();
host.OnDeinitializeMelon();Check(!Host.Enabled);Check(Host.Player==null);Check(GRoot.inst.Children.Count==0);
Host.Unregister(speed);host.OnUpdate();Check(GRoot.inst.Children.Count==0);
Console.WriteLine($"PASS {checks} host lifecycle checks: ordering, duplicates, focus ownership, single-feature use, unload, failure isolation, stage replacement, save reset, shutdown, wheel scroll, panel width.");
sealed class WidePanel(string id,int order,float height):Panel {
    public override string Id=>id;public override int Order=>order;public override float Height=>height;public override float Width=>640;
    public bool Show=true;public override bool Visible=>Show;
    public override void Build(){}public override void Refresh(){}public override void Reset(){}
}
sealed class TestPanel(string id,int order,float height):Panel {
    public override string Id=>id;public override int Order=>order;public override float Height=>height;
    public override bool Visible=>Show;
    public bool Show=true;
    public bool Enable=true,Fail;public int Resets;
    public override void Build(){}
    public override void Refresh(){if(Fail)throw new Exception("panel failure");SetActive(Enable);}
    public override void Reset(){Resets++;}
}

using System.Reflection;
using Restitutor.Map;
using Il2CppClient.UILogic.UIMap;
using Il2CppClient.UILogic.UIMap.UIMapIcon;
using Il2CppClient.UILogic.UISailReady;
using Il2CppClient.Manager;
using Il2CppCore.NewUISystem;
using UnityEngine;
using UnityEngine.InputSystem;

int count=0;
object? Call(string name,params object?[] args)=>typeof(SharedMap).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args);
void Set(string name,object? value)=>typeof(SharedMap).GetField(name,BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,value);
bool Flag(string name)=>(bool)typeof(SharedMap).GetField(name,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
void Check(bool pass,string name) {if(!pass) throw new Exception(name); count++;}
void Reset()
{
    Call("Clear"); Set("closing",false);Set("closingAction","");
    UIMapCtrl.Instance=new(); UISailReadyCtrl.Instance=new(); UISailLineCtrl.Instance=new();
    TransitionManager.Instance=new(); UIManager.Instance=new();
    var m=UIMapCtrl.Instance; var p=UISailLineCtrl.Instance; var r=UISailReadyCtrl.Instance;
    m.OnClose=()=>Call("MapClose",m);
    p.OnClose=()=>Call("PlannerClose",p);
    p.Select=id=>Call("SelectPort",p,id);
    r.OnClose=()=>Call("ReadyClose",r);
    r.OnCloseCheck=harbor=>Call("CloseCheckLine",r,harbor);
    foreach(int id in new[]{20,30,40})
    {
        p.Model.TotalPortDic[id]=new();
        m.Model.HarbourIcons[id]=new UIMapHarbourIcon { HarbourId=id,Model=m.Model };
    }
}
void Begin(bool route=true)
{
    Call("Begin",true);
    if(route) { UISailLineCtrl.Instance._isHarbor=true; Call("PlannerShow",UISailLineCtrl.Instance); }
    Call("ReadyUpdate",UISailReadyCtrl.Instance.View);
    SharedMap.Tick();
}
Reset();
Check((bool)Call("MapAction",UIMapCtrl.Instance)!,"ordinary Tab actions retain original");
var normalIcon=new UIMapHarbourIcon();
Check((bool)Call("PortClick",normalIcon)!,"ordinary icon retains original click");
Begin();
Check(Flag("routeMode")&&Flag("available"),"route mode becomes available after background");
Check(UIMapCtrl.Instance.ShowCalls==1,"one map show per entry");
Check(UIManager.Instance.CurrentFocusViewCtrl==UISailLineCtrl.Instance,"native planner owns focus");
Check(!(bool)Call("MapAction",UIMapCtrl.Instance)!,"route mode blocks duplicate Tab actions");
Time.frameCount=10;
var icon=UIMapCtrl.Instance.Model.HarbourIcons[20];
Check(!(bool)Call("PortClick",icon)!,"route click handled by shared adapter");
Check(UISailLineCtrl.Instance.SailLineManager.SelectPortList[0]==20,"route click selects clicked port");
Call("PortClick",icon);
Check(UISailLineCtrl.Instance.SailLineManager.SelectPortList[0]==20,"same-frame duplicate does not toggle twice");
Time.frameCount++;
Call("SelectFocused",UISailLineCtrl.Instance);
Check(UISailLineCtrl.Instance.SailLineManager.SelectPortList[0]==0,"Space uses same toggle as click");
UIManager.Instance.CurrentFocusViewCtrl=new IUIBaseCtrl(); Time.frameCount++;
Call("PortClick",icon);
Check(UISailLineCtrl.Instance.SailLineManager.SelectPortList[0]==0,"confirmation focus blocks map click");
var inputArgs=new object?[]{UIMapCtrl.Instance.View,true};Call("MapInput",inputArgs);
Check(!(bool)inputArgs[1]!,"confirmation focus blocks map movement input");
Reset();UIMapCtrl.Instance.BackgroundReady=false;Begin();
Check(!Flag("available"),"input stays locked before textures ready");
Check(UISailReadyCtrl.Instance.Updates==1,"supply update still runs during map loading");
UIMapCtrl.Instance.BackgroundReady=true;
TransitionManager.Instance.IsInBlackScreen=true;SharedMap.Tick();
Check(!Flag("available"),"black transition still locks route");
Call("ReadyUpdate",UISailReadyCtrl.Instance.View);SharedMap.Tick();
Check(Flag("available"),"ready transition releases map");
Reset();Begin(false);
Check(!Flag("routeMode")&&Flag("available"),"departure preparation does not enable route editing");
Check((bool)Call("PortClick",UIMapCtrl.Instance.Model.HarbourIcons[20])!,"preparation does not route port click to waypoint editing");
Reset();Begin();
var action=new InputAction {name="Action_Start"};
var press=new InputAction.CallbackContext {action=action,performed=true,Pressed=true};
Check(!(bool)Call("CaptureClose",press)!,"Tab closes route and consumes press");
Check(UISailLineCtrl.Instance.SailLineManager.SelectPortList.Count==0,"closing clears editing list");
var oldTimer=UISailReadyCtrl.Instance.Timers.Single();
oldTimer.Callback();
Check(!Flag("session")&&!UIMapCtrl.Instance.View.Open,"delayed cleanup closes shared map");
var release=new InputAction.CallbackContext {action=action,canceled=true};
Check(!(bool)Call("CaptureClose",release)!,"release does not leak to city");
Check((bool)Call("MapAction",UIMapCtrl.Instance)!,"normal Tab behavior restored after route close");
Reset();Begin();
UIMapCtrl.Instance.Close();
var stale=UISailReadyCtrl.Instance.Timers.Single();
Begin();
stale.Callback();
Check(Flag("session")&&UIMapCtrl.Instance.View.Open,"stale close timer cannot close reopened route");
Reset();Begin();
typeof(Restitutor.Map.RoutePortVisuals).GetField("UrlMode",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.SetValue(null,true);
var portIcon=UIMapCtrl.Instance.Model.HarbourIcons[20];
var portUi=(Il2CppMap.UIHarbourIcon)portIcon.Component!;
UISailLineCtrl.Instance.Model.TotalPortDic[20].MeasureCanReach=false;
Call("ComputeRed");Call("RefreshPort",portIcon,false);
Check(portUi.loaderIcon.url=="red:port" && portUi.ctrlSelfPort.selectedIndex==2,"unreachable port becomes red");
UISailLineCtrl.Instance.Model.TotalPortDic[20].MeasureCanReach=true;
Call("ComputeRed");Call("RefreshPort",portIcon,false);
Check(portUi.loaderIcon.url=="normal" && portUi.ctrlSelfPort.selectedIndex==1,"reachable port restores baseline");
UISailLineCtrl.Instance.Model.TotalPortDic[20].NoLineReach=false;
Call("ComputeRed");Call("RefreshPort",portIcon,false);
Check(portUi.loaderIcon.url=="red:port","missing route also becomes red");
portUi.loaderIcon.url="native-updated";portUi.ctrlSelfPort.selectedIndex=0;
Call("RefreshPort",portIcon,true);
Call("Restore");
Check(portUi.loaderIcon.url=="native-updated" && portUi.ctrlSelfPort.selectedIndex==0,"close restores latest native baseline");
UISailLineCtrl.Instance._playerData.PlayerPort.StayInPortId=20;
Call("ComputeRed");Call("RefreshPort",portIcon,false);
Check(portUi.loaderIcon.url=="native-updated","current port is always reachable");
// 0.2.5: no UpdateInfo hook. The UIMapView.Refresh postfix re-applies route visuals to icons in view.
Reset();Begin();
var rIcon=UIMapCtrl.Instance.Model.HarbourIcons[20];var rUi=(Il2CppMap.UIHarbourIcon)rIcon.Component!;
UISailLineCtrl.Instance._playerData.PlayerPort.StayInPortId=0;
UISailLineCtrl.Instance.Model.TotalPortDic[20].MeasureCanReach=false;UISailLineCtrl.Instance.Model.TotalPortDic[20].NoLineReach=true;
typeof(Restitutor.Map.RoutePortVisuals).GetField("UrlMode",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.SetValue(null,false);
void NativeRefresh(){ foreach(var id in UIMapCtrl.Instance.Model.HarbourIcons.Keys){ var ui=(Il2CppMap.UIHarbourIcon)UIMapCtrl.Instance.Model.HarbourIcons[id].Component!; ui.loaderIcon.url="normal"; ui.ctrlSelfPort.selectedIndex=1; }
  Call("MapRefresh",UIMapCtrl.Instance.View); }
Call("ComputeRed");NativeRefresh();
Check(rUi.ctrlSelfPort.selectedIndex==2&&rUi.loaderIcon.url=="normal","controller mode: red port gets state 2 after the native redraw, url untouched");
typeof(Restitutor.Map.RoutePortVisuals).GetField("UrlMode",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.SetValue(null,true);
NativeRefresh();Check(rUi.ctrlSelfPort.selectedIndex==2&&rUi.loaderIcon.url=="red:port","url mode: red url re-applied after the native redraw");
Call("MapRefresh",UIMapCtrl.Instance.View);Check(rUi.loaderIcon.url=="red:port","no native redraw: red kept, baseline not overwritten by our red");
UISailLineCtrl.Instance.Model.TotalPortDic[20].MeasureCanReach=true;Call("RefreshRoute");
Check(rUi.ctrlSelfPort.selectedIndex==1&&rUi.loaderIcon.url=="normal","route change: reachable port restored to the native drawing");
Call("Restore");Check(UIMapCtrl.Instance.RedPort.Count==0,"RedPort is never touched");
typeof(Restitutor.Map.RoutePortVisuals).GetField("UrlMode",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.SetValue(null,false);
Il2CppInterop.Runtime.IL2CPP.NativeLayout=false;
Console.WriteLine($"PASS {count} shared map/input/lifecycle checks. Isolated doubles only; not native execution.");


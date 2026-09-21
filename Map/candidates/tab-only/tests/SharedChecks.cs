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
var portIcon=UIMapCtrl.Instance.Model.HarbourIcons[20];
var portUi=(Il2CppMap.UIHarbourIcon)portIcon.Component!;
UISailLineCtrl.Instance.Model.TotalPortDic[20].MeasureCanReach=false;
Call("RefreshPort",portIcon,false);
Check(portUi.loaderIcon.url=="red:port" && portUi.ctrlSelfPort.selectedIndex==2,"unreachable port becomes red");
UISailLineCtrl.Instance.Model.TotalPortDic[20].MeasureCanReach=true;
Call("RefreshPort",portIcon,false);
Check(portUi.loaderIcon.url=="normal" && portUi.ctrlSelfPort.selectedIndex==1,"reachable port restores baseline");
UISailLineCtrl.Instance.Model.TotalPortDic[20].NoLineReach=false;
Call("RefreshPort",portIcon,false);
Check(portUi.loaderIcon.url=="red:port","missing route also becomes red");
portUi.loaderIcon.url="native-updated";portUi.ctrlSelfPort.selectedIndex=0;
Call("PortRefresh",portIcon);
Call("Restore");
Check(portUi.loaderIcon.url=="native-updated" && portUi.ctrlSelfPort.selectedIndex==0,"close restores latest native baseline");
UISailLineCtrl.Instance._playerData.PlayerPort.StayInPortId=20;
Call("RefreshPort",portIcon,false);
Check(portUi.loaderIcon.url=="native-updated","current port is always reachable");
// 0.2.3 native red-port path. The double below mirrors the native branch at 0x5E4DE4.
Reset();Il2CppInterop.Runtime.IL2CPP.NativeLayout=true;typeof(Restitutor.Map.SharedMap).GetField("verified",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.SetValue(null,false);Begin();
Check((bool)typeof(Restitutor.Map.RoutePortVisuals).GetProperty("Native",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.GetValue(null)!,"layout 0x68/0x78/0x38 enables native path");
var nIcon=UIMapCtrl.Instance.Model.HarbourIcons[20];var nUi=(Il2CppMap.UIHarbourIcon)nIcon.Component!;
UISailLineCtrl.Instance._playerData.PlayerPort.StayInPortId=0;
void NativeUpdate(UIMapHarbourIcon icon){
  var args=new object?[]{icon,null};
  typeof(Restitutor.Map.SharedMap).GetMethod("BeforePortInfo",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.Invoke(null,args);
  var ui=(Il2CppMap.UIHarbourIcon)icon.Component!;var m=UIMapCtrl.Instance;
  bool red=m._eMapUseType==EMapUseType.Commerce&&m.RedPort.Contains(icon.HarbourId);
  ui.loaderIcon.url=red?"red:port":"normal";ui.ctrlSelfPort.selectedIndex=red?2:1;
  Call("PortRefresh",icon);
  typeof(Restitutor.Map.SharedMap).GetMethod("AfterPortInfo",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.Invoke(null,new object?[]{null,args[1]});
}
UISailLineCtrl.Instance.Model.TotalPortDic[20].MeasureCanReach=false;UISailLineCtrl.Instance.Model.TotalPortDic[20].NoLineReach=true;
NativeUpdate(nIcon);
Check(nUi.loaderIcon.url=="red:port"&&nUi.ctrlSelfPort.selectedIndex==2,"native branch draws unreachable port red");
Check(UIMapCtrl.Instance._eMapUseType==EMapUseType.Normal,"use type restored after the call");
Check(UIMapCtrl.Instance.RedPort.Count(x=>x==20)==1,"id added to RedPort once");
NativeUpdate(nIcon);Check(UIMapCtrl.Instance.RedPort.Count(x=>x==20)==1&&nUi.loaderIcon.url=="red:port","repeated frames keep one entry and the same url");
UISailLineCtrl.Instance.Model.TotalPortDic[20].NoLineReach=false;NativeUpdate(nIcon);Check(nUi.loaderIcon.url=="red:port","missing route stays red");
UISailLineCtrl.Instance.Model.TotalPortDic[20].MeasureCanReach=true;UISailLineCtrl.Instance.Model.TotalPortDic[20].NoLineReach=true;NativeUpdate(nIcon);
Check(nUi.loaderIcon.url=="normal"&&nUi.ctrlSelfPort.selectedIndex==1&&UIMapCtrl.Instance._eMapUseType==EMapUseType.Normal,"reachable port drawn by the untouched native path");
UISailLineCtrl.Instance._playerData.PlayerPort.StayInPortId=20;UISailLineCtrl.Instance.Model.TotalPortDic[20].MeasureCanReach=false;NativeUpdate(nIcon);
Check(nUi.loaderIcon.url=="normal","current port is always reachable (native path)");
UISailLineCtrl.Instance._playerData.PlayerPort.StayInPortId=0;UIMapCtrl.Instance.RedPort.Add(999);
Call("Restore");
Check(!UIMapCtrl.Instance.RedPort.Contains(20)&&UIMapCtrl.Instance.RedPort.Contains(999),"restore removes only ids this session added");
Il2CppInterop.Runtime.IL2CPP.NativeLayout=false;
Console.WriteLine($"PASS {count} shared map/input/lifecycle checks. Isolated doubles only; not native execution.");


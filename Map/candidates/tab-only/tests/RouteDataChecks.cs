using Restitutor.Map;
using Il2CppClient.UILogic.UISailReady;
using Il2CppClient.UILogic.UITips;
using UnityEngine;

int checks=0,refreshes=0;
void Check(bool value,string name) { if(!value) throw new Exception(name);checks++; }
UISailLineCtrl New()
{
    refreshes=0; UITipsCtrl.Instance.Last="";
    var p=new UISailLineCtrl();
    foreach(int id in new[]{10,20,30,40,50}) p.Model.TotalPortDic[id]=new();
    return p;
}
void Select(UISailLineCtrl p,int id,bool allowed=true) => NativeRouteData.Select(p,id,allowed,()=>refreshes++);

foreach(int port in new[]{0,-1})
{ var p=New(); Select(p,port); Check(p.SailLineManager.PathCalls==0,"nonpositive port rejected"); }
{
    var p=New(); Select(p,20,false);
    Check(p.SailLineManager.PathCalls==0 && refreshes==0,"locked input does not calculate or repaint");
    p.Model.Measurer=null; Select(p,20);
    Check(UITipsCtrl.Instance.Last=="Wharf_Preparing_Cabin_Need_Hero","original missing navigator message");
    Check(p.SailLineManager.PathCalls==0,"missing navigator cannot edit");
}
{
    var p=New(); Select(p,10); Select(p,999);
    Check(p.SailLineManager.PathCalls==0,"origin first and unknown port rejected");
    p.Status=ESelectPortStatus.Unreached; Select(p,20);
    Check(p.SailLineManager.PathCalls==0 && UITipsCtrl.Instance.Last=="","status five rejected silently");
    p.Status=ESelectPortStatus.NeedMeasurer; Select(p,20);
    Check(UITipsCtrl.Instance.Last=="Tip_MeasureLvNoReached","native navigator level restriction");
    p.Status=0; p.Model.TotalPortDic[20].NoLineReach=false; Select(p,20);
    Check(p.SailLineManager.PathCalls==0,"native NoLineReach false rejects selection");
}
{
    var p=New(); Select(p,20);
    var m=p.SailLineManager;
    Check(m.SelectPortList.SequenceEqual(new[]{20,0,0}),"first waypoint occupies slot zero");
    Check(m.LastPath==(100,200,0,Vector2.zero),"first native path has original port node and no preceding port");
    Select(p,30);
    Check(m.LastPath==(200,300,20,new Vector2(200,0)),"second native path carries preceding endpoint");
    Select(p,40); Select(p,50);
    Check(m.SelectPortList.SequenceEqual(new[]{20,30,50}),"fourth choice replaces only third waypoint");
    Check(m.BestLineList[2].SequenceEqual(new[]{300,500}),"replaced path does not append stale nodes");
    Select(p,30);
    Check(m.SelectPortList.SequenceEqual(new[]{20,0,0}),"cancel middle removes suffix");
    Check(m.BestLineList[0].SequenceEqual(new[]{100,200}),"cancel middle retains first path");
    Check(m.BestLineList[1].Count==0 && m.BestLineVector2List[2].Count==0,"canceled paths and coordinates cleared");
    Check(m.SelectPortList.Count==3,"cancellation does not shrink native three slots");
    Select(p,20);
    Check(m.SelectPortList.All(x=>x==0),"cancel first clears all waypoints");
}
{
    var p=New(); p.SailLineManager.PathSucceeds=false; Select(p,20);
    Check(p.SailLineManager.SelectPortList.All(x=>x==0) && refreshes==0,"failed native calculation does not add waypoint");
    Check(UITipsCtrl.Instance.Last=="UIStatic_SailLine_NoOpenSailLineOrContainUnexploredPorts","native failed-path message retained");
}
{
    var p=New(); Select(p,20); Select(p,30); Select(p,40);
    p.SailLineManager.PathSucceeds=false; Select(p,50);
    Check(p.SailLineManager.SelectPortList.SequenceEqual(new[]{20,30,0}),"native replacement clears former third even if new path fails");
}
Console.WriteLine($"PASS {checks} route data checks against isolated native API doubles. No game assemblies loaded.");

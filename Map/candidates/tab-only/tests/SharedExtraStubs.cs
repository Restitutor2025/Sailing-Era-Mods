namespace Il2Cpp { public static class Messenger { public static void Broadcast<T>(string name,T value) {} } }
namespace Il2CppSystem { public delegate void Action(); }
namespace Il2CppCommon.Trigger { public class Trigger { public Il2CppSystem.Action Callback; public Trigger(uint delay,uint count,Il2CppSystem.Action callback) { Callback=callback; } } }
namespace Il2CppGyyx.Template { public static class TemplateManager { public static Port GetPort(int id)=>new(); public static int[] PrefabLaneKeys=Array.Empty<int>(); } }
namespace Il2CppClient.Const { public enum EPortFacilityType { None } }
namespace Il2CppClient.PlayerStore { public enum EPlayerHelp { None } }
namespace Il2CppClient.WorldLogic.Scenes { public class HarbourScene {} }
namespace Il2CppClient.UILogic.UIMenu { public static class UIMenuHelper { public static int _menuUIShow; } }
namespace Il2CppClient.Manager
{
    public static class GameManager { public static bool IsPlayerInGame=true; }
    public class FunctionState { public bool IsOpen; }
    public class FunctionDB { public FunctionState? GetFunctionData(int id)=>null; }
    public class FunctionOpenManager { public static FunctionOpenManager Instance=new(); public FunctionDB _function=new(); }
    public class PortManager { public static PortManager Instance=new(); public int Returns; public void CommonBackToPortFromFacility(Il2CppClient.Const.EPortFacilityType type)=>Returns++; }
}
namespace Il2CppClient.UILogic.UIHarbor
{
    public class UIHarborModel { public int AutoSupplyCost; }
    public class UIHarborView { public UIHarborModel model=new(); public bool _isClick; }
    public class UIHarborCtrl { public static UIHarborCtrl Instance=new(); public void Close() {} }
}
namespace Il2CppClient.UILogic.UIWharf
{
    public class UIWharfView { public int AutoSupplyCost; public bool _isClick; }
    public class UIWharfCtrl { public static UIWharfCtrl Instance=new(); public int Shows; public void Close() {} public void Show()=>Shows++; }
}
namespace Il2CppClient.UILogic.UISailReady
{
    public class SailLine { public bool isSelect,Order; public int StartNodeId=100,EndNodeId=200; }
    public class LaneDB { public bool CheckNewLane(int id,out List<int> lines) {lines=new();return false;} public void UnlockPrefabLane(int id) {} }
    public partial class SailLineManager
    {
        public List<int> ComplexLineDatas=new(),SinglePortLineDatas=new();
        public List<SailLine> Lines=new(){new()};
        public void InitSelectPortList() {} public void InitAllLine() {}
        public List<SailLine> GetAllSailLine()=>Lines;
        public void StopLineNewAni(List<int> lines) {}
    }
    public class UISailLineView : Il2CppCore.NewUISystem.UIBase
    { public Il2CppFairyGUI.GList ListPort=new(),ListArea=new(); public void SetFocusPortList() {} }
    public partial class UISailLineCtrl
    {
        public static UISailLineCtrl Instance=new();
        public new UISailLineView View=new();
        public bool _isHarbor,NeedClearPortList,OverList;
        public Action<int>? Select;
        public Action? OnClose;
        public void OnAction_A(int port)=>Select?.Invoke(port);
        public void ShowCheckLane(bool harbor,int cost) { _isHarbor=harbor; }
        public void BuildData() {} public void AddPortData() {}
        public bool TouchPortOrAreaList()=>OverList;
        public void Close() { OnClose?.Invoke(); View.Open=false; }
    }
    public class ReadyModel { public bool MapIsDown,IsAutoSail; public int ShowInterActive; }
    public class UISailReadyView : Il2CppCore.NewUISystem.UIBase { public ReadyModel model=new(); }
    public class UISailReadyCtrl : Il2CppCore.NewUISystem.IUIBaseCtrl
    {
        public static UISailReadyCtrl Instance=new();
        public new UISailReadyView View=new(){Open=false};
        public ReadyModel Model=>View.model;
        public bool PlayTransition=true;
        public int Updates,Closes;
        public Action? OnClose;
        public Action<bool>? OnCloseCheck;
        public List<Il2CppCommon.Trigger.Trigger> Timers=new();
        public void Show() {View.Open=true; Model.MapIsDown=false;}
        public bool IsClose()=>!View.Open;
        public void Close() {Closes++;OnClose?.Invoke();View.Open=false;}
        public void CloseCheckLine(bool harbor)=>OnCloseCheck?.Invoke(harbor);
        public void ShowCheckLine() {Model.ShowInterActive=1;}
        public void Updata()=>Updates++;
        public void AddTimer(Il2CppCommon.Trigger.Trigger timer)=>Timers.Add(timer);
    }
}

namespace Il2CppGyyx.Template { public class Port { public string mapIcon="port"; } }
namespace Il2CppClient.Utils { public static class IconUtils { public static string GetRedHarbourIcon(string icon)=>"red:"+icon; } }

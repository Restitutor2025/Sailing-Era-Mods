// Test doubles only. They exercise the adapter, NOT native game/UI execution.
namespace TestSupport
{
    public class NativeObject
    {
        static long next = 1;
        public IntPtr Pointer = (IntPtr)next++;
        public T Cast<T>() where T : class => (this as T)!;
        public T? TryCast<T>() where T : class => this as T;
    }
}
namespace UnityEngine { public static class Time { public static int frameCount; } }
namespace UnityEngine.InputSystem
{
    public class InputAction
    {
        public string name = "";
        public struct CallbackContext
        {
            public InputAction action;
            public bool performed, Pressed, canceled;
            public bool ReadValueAsButton() => Pressed;
        }
    }
}
namespace Il2CppCore.InputSystem { public class InputSystemManager {} }
namespace Il2CppFairyGUI
{
    public class GObject : TestSupport.NativeObject
    {
        public bool isDisposed, visible = true;
    }
    public class GComponent : GObject {}
    public class GLoader : GObject { public string url="normal"; }
public class Controller { public int selectedIndex=1; } public class Box { public int Value; public T Unbox<T>() => (T)(object)Value; } public class GButton : GObject { public Box data=new(); public GObject parent=new GList(); } public class GList : GObject { public int selectedIndex=-1; public List<GButton> Children=new(); public GObject GetChildAt(int i)=>Children[i]; public int GetChildIndex(GObject o)=>Children.IndexOf((GButton)o); } public class EventContext { public GObject? sender; }
    public class Content : GComponent { public GObject groupTip = new(); public Controller ctrlOpenStatus = new(); }
}
namespace Il2CppMap
{
    public class UIHarbourIcon : Il2CppFairyGUI.GComponent { public Il2CppFairyGUI.GLoader loaderIcon=new(); public Il2CppFairyGUI.Controller ctrlSelfPort=new(); public Il2CppFairyGUI.GObject imgRedFlag = new() { visible = false }; }
}
namespace Il2CppCore.NewUISystem
{
    public class UIBase : TestSupport.NativeObject
    {
        public bool Open = true, IsInputActive = true, IsOnFocused;
        public Il2CppFairyGUI.Content Content = new();
        public Il2CppFairyGUI.Content UIContent => Content;
        public bool IsOpen() => Open;
    }
    public class IUIBaseCtrl : TestSupport.NativeObject { public UIBase View = new(); }
}
namespace Il2CppClient.UILogic.UIMap.UIMapIcon
{
    public class UIMapHarbourIcon : TestSupport.NativeObject
    {
        public static IntPtr NativeFieldInfoPtr__HarbourId_k__BackingField = (IntPtr)0x1003;
        public int HarbourId = 123, Guid;
        public bool IsInit = true;
        public Il2CppClient.UILogic.UIMap.TestModel? Model;
        public Il2CppFairyGUI.GComponent? Component = new Il2CppMap.UIHarbourIcon();
    }
    public class UIMapLineHarbourIcon : TestSupport.NativeObject { public int HarbourId, Guid; }
}
namespace Il2CppClient.UILogic.UIMap
{
    using Il2CppClient.UILogic.UIMap.UIMapIcon;
    using Il2CppClient.UILogic.UISailReady;
    public enum MapViewMode { World, Area }
    public class TestModel : TestSupport.NativeObject
    {
        public int DirtyCalls; public void ClearAllLaneIcon() {}
        public Dictionary<int, UIMapHarbourIcon> HarbourIcons = new();
        public HashSet<int> InViewIcons = new();
        public MapViewMode ViewMode = MapViewMode.Area;
        public void MarkDirty() => DirtyCalls++;
    }
    public class TestLineModel
    {
        public bool AcceptInput = true;
        public Dictionary<int, UIMapLineHarbourIcon> HarbourIcons = new();
        public int SelectIconId;
        public MapViewMode ViewMode = MapViewMode.Area;
    }
    public class UIMapView : Il2CppCore.NewUISystem.UIBase {}
    public class UIMapHandler : TestSupport.NativeObject {}
    public enum EMapUseType { Normal = 0, Commerce = 1 }
    public class UIMapCtrl : Il2CppCore.NewUISystem.IUIBaseCtrl
    {
        public static IntPtr NativeFieldInfoPtr__eMapUseType = (IntPtr)0x1001, NativeFieldInfoPtr_RedPort = (IntPtr)0x1002;
        public EMapUseType _eMapUseType;
        public List<int> RedPort = new();
        public static UIMapCtrl Instance = new();
        public new UIMapView View = new() { Open = false };
        public TestModel Model = new();
        public UIMapHandler UIMapHandler = new();
        public int ShowCalls, CloseCalls, FocusPort, FocusPlayerCalls, EnteredArea, WorldSelectCalls;
        public bool HoldOpen, BackgroundReady = true;
        public bool MapBgLoadDown() => View.Open && BackgroundReady;
        public Action? OnShow, OnClose, OnSetLane;
        public UIMapHarbourIcon? Selected;
        public List<SailLine>? Lines;
        public bool IsClose() => !View.Open;
        public void Show() { ShowCalls++; View.Open = !HoldOpen; View.IsOnFocused = true; OnShow?.Invoke(); }
        public void TransferFocusTo(Il2CppCore.NewUISystem.IUIBaseCtrl ctrl)
        {
            View.IsOnFocused = false; ctrl.View.IsOnFocused = true;
            Il2CppClient.Manager.UIManager.Instance.CurrentFocusViewCtrl = ctrl;
        }
        public void Close() { CloseCalls++; if (View.Open) { OnClose?.Invoke(); View.Open = false; } }
        public UIMapHarbourIcon? GetCurrentSelectPort(bool value) => Selected;
        public void SetLane(List<SailLine> lines) { Lines = lines; OnSetLane?.Invoke(); }
        public void SetFocusPortIcon(int id) { FocusPort = id; Selected = Model.HarbourIcons.GetValueOrDefault(id); }
        public void SetMapFocusOnPlayer() => FocusPlayerCalls++;
        public void EnterArea(int area, bool inHarbourShowHookOpen) => EnteredArea = area;
        public void OnAction_A() => WorldSelectCalls++; public void UpdatePlayerInHarbour() {}
    }
    public class UIMapLineCtrl : Il2CppCore.NewUISystem.IUIBaseCtrl
    {
        public static UIMapLineCtrl Instance = new();
        public TestLineModel Model = new();
        public bool BackgroundReady = true;
        public bool MapBgLoadDown() => View.Open && BackgroundReady;
    }
}
namespace Il2CppClient.UILogic.UIOperationTips
{
    public class TipModel { public string uiName = "native-tip-owner"; public int state = 4; }
    public class UIOperationTipsCtrl { public static UIOperationTipsCtrl Instance = new(); public TipModel Model = new(); }
}
namespace Restitutor.Map
{
    internal static class EntryPoint
    {
        internal static TestLog Log = new();
        internal static void Hook(Type target, string name, Type handler, string? before = null, string? after = null, Type[]? args = null, string? final = null) {}
        internal static void InputHandler(Func<UnityEngine.InputSystem.InputAction.CallbackContext, bool> allow) {}
    }
    internal class TestLog
    { public void Warning(string text) => Msg("WARN "+text);
        public List<string> Messages = new();
        public void Msg(string message) => Messages.Add(message);
        public void Error(string message) => Messages.Add(message);
    }
}
namespace Il2CppClient.PlayerStore
{
    public class PlayerAreaDB { public IntPtr Pointer = (IntPtr)300; }
    public class PlayerData { public PlayerAreaDB? PlayerAreaDB = new(); }
}
namespace Il2CppClient.Manager
{
    public class UIManager
    {
        public static UIManager Instance = new();
        public Il2CppCore.NewUISystem.IUIBaseCtrl? CurrentFocusViewCtrl;
        public string Tips = "";
        public int TipState; public void HideOperationTipsPanel() { Tips=""; }
        public void SetFocusOnUIView(Il2CppCore.NewUISystem.IUIBaseCtrl ctrl, bool value) => CurrentFocusViewCtrl = ctrl;
        public void ShowOperationTipsPanel(string name, int state) { Tips = name; TipState = state; }
    }
    public class TransitionManager
    {
        public static TransitionManager Instance = new();
        public bool IsInGatherState, IsEnterBlackScreen, IsInBlackScreen; public int Dissolves; public void LineDissolve(float delay) { Dissolves++; IsInGatherState=IsEnterBlackScreen=IsInBlackScreen=false; }
    }
    public class PlayerDataManager
    {
        public static PlayerDataManager Instance = new();
        public Il2CppClient.PlayerStore.PlayerData? Data = new();
    }
    public class MapMaskManager
    {
        public static MapMaskManager Instance = new();
        public Il2CppClient.UILogic.UIMap.UIMapMask.UIMapMask? MapMask = new();
    }
}
namespace Il2CppClient.UILogic.UIMap.UIMapMask
{
    public class UIMapMask
    {
        public IntPtr Pointer = (IntPtr)400;
        public static object? MainTexture = new(), MaskBufferLeft = new(), MaskBufferRight = new();
    }
}


namespace Il2CppInterop.Runtime
{
    public static class IL2CPP
    {
        public static bool NativeLayout;
        public static uint il2cpp_field_get_offset(IntPtr field) => !NativeLayout ? 0u : (long)field switch { 0x1001 => 0x68u, 0x1002 => 0x78u, 0x1003 => 0x38u, _ => 0u };
    }
}

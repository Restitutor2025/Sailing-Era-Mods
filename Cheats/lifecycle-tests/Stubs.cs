// No game/Unity/Melon assemblies are loaded by these lifecycle tests.
namespace UnityEngine {
    public record struct Vector2(float x,float y);
    public struct Color { public Color(float r,float g,float b,float a){} public static Color white=>new(); }
}
namespace HarmonyLib {
    public class HarmonyMethod { public HarmonyMethod(System.Reflection.MethodInfo method){} }
    public class Harmony { public void Patch(System.Reflection.MethodInfo m,HarmonyMethod? a,HarmonyMethod? b,HarmonyMethod? finalizer){} public void UnpatchSelf(){} }
}
namespace MelonLoader {
    [AttributeUsage(AttributeTargets.Assembly)] public class MelonInfoAttribute:Attribute { public MelonInfoAttribute(Type t,string n,string v,string a){} }
    [AttributeUsage(AttributeTargets.Assembly)] public class MelonGameAttribute:Attribute { public MelonGameAttribute(string a,string b){} }
    public class MelonLogger { public class Instance { public void Msg(string s){} public void Error(string s){} } }
    public class MelonMod {
        public MelonLogger.Instance LoggerInstance=new(); public HarmonyLib.Harmony HarmonyInstance=new();
        public virtual void OnInitializeMelon(){} public virtual void OnUpdate(){} public virtual void OnDeinitializeMelon(){}
    }
}
namespace Il2CppClient.PlayerStore {
    public class PlayerData { public IntPtr Pointer=(IntPtr)1; public Port PlayerPort=new(); public void Deserialize(){} }
    public class Port { public bool IsStayInPort=true; public int StayInPortId=1; }
    public class WorldPortHoldDB { public void InitHook(){} }
}
namespace Il2CppClient.Manager {
    public class PlayerDataManager { public static PlayerDataManager Instance=new(); public Il2CppClient.PlayerStore.PlayerData Data=new(); public void ProcessArchiveInitialize(){} }
}
namespace Il2CppCore.InputSystem { public class InputSystemManager { public void OnEventCaptureInput(){} } }
namespace Il2CppFairyGUI {
    public class InputEvent { public float x,y,mouseWheelDelta; }
    public class DisplayObject { public EventListener onMouseWheel=new(); }
    public class EventContext { public InputEvent inputEvent=new();public void CaptureTouch(){}public void StopPropagation(){} }
    public class EventCallback1 {
        public Action<EventContext> Action=null!;
        public static explicit operator EventCallback1(Action<EventContext> a)=>new(){Action=a};
    }
    public class EventListener { public List<EventCallback1> Callbacks=new(); public void Add(EventCallback1 a)=>Callbacks.Add(a);public void Fire(float x=0,float y=0){foreach(var c in Callbacks)c.Action(new(){inputEvent=new(){x=x,y=y}});} public void Wheel(float d){foreach(var c in Callbacks)c.Action(new(){inputEvent=new(){mouseWheelDelta=d}});} }
    public class GTextInput:GObject {}
    public class GObject {
        public T? TryCast<T>() where T:class=>this as T;
        private static int next;
        public IntPtr Pointer=(IntPtr)(++next);
        public string name="";
        public bool isDisposed,visible=true,touchable=true;
        public float alpha=1,width,height,x,y;
        public GComponent? parent;
        public DisplayObject? displayObject=new();
        public EventListener onTouchBegin=new(),onTouchEnd=new(),onKeyDown=new(),onTouchMove=new(),onRollOut=new(),onClick=new();
        public UnityEngine.Vector2 GlobalToLocal(UnityEngine.Vector2 p)=>p;
        public void SetXY(float a,float b){x=a;y=b;}
        public void SetSize(float a,float b){width=a;height=b;}
        public void SetScale(float a,float b){}
        public virtual void Dispose(){isDisposed=true;parent?.Children.Remove(this);parent=null;}
    }
    public class GComponent:GObject {
        public void SetupOverflow(OverflowType t){}public GObject GetChildAt(int i)=>new GObject(); // Model native base wrappers; CLR downcasts must not be relied on.
        public int sortingOrder;
        public List<GObject> Children=new();
        public void AddChild(GObject o){o.parent?.Children.Remove(o);Children.Add(o);o.parent=this;}
        public override void Dispose(){foreach(var child in Children.ToArray())child.Dispose();base.Dispose();}
    }
    public class GRoot:GComponent { public static GRoot inst=new(){width=1600,height=900}; public GObject? focus,touchTarget; }
    public enum AutoSizeType { None }
    public enum AlignType {Left,Center}public enum OverflowType {Hidden}
    public class TextFormat { public string font=""; public int size; public UnityEngine.Color color;public AlignType align; }
    public class GTextField:GObject { public AutoSizeType autoSize;public bool singleLine;public string text="";public TextFormat textFormat=new(); }
    public class GGraph:GObject { public void DrawRect(float w,float h,int border,UnityEngine.Color a,UnityEngine.Color b){SetSize(w,h);} }
    public static class UIConfig { public static string defaultFont=""; }
}


namespace Il2CppCore.SceneSystem {
    public class SceneManager {
        public static SceneManager Instance=new();
        public bool IsSceneEntered=true, IsInLoadingOrStarting, IsInHarborScene=true, IsInOceanScene;
        public Il2CppClient.WorldLogic.Scenes.OceanScene Ocean=new();
        public Il2CppClient.WorldLogic.Scenes.OceanScene CurScene()=>Ocean;
    }
}
namespace Il2CppClient.WorldLogic.Scenes.SceneState { public enum SceneStateType { Sailing, Other } }
namespace Il2CppClient.WorldLogic.Scenes {
    public class OceanScene {
        public bool IsInBattle;
        public SceneState.SceneStateType SceneStateType;
        public T? TryCast<T>() where T:class=>this as T;
    }
}

namespace UnityEngine { public static class Application { public static bool isFocused=true; } }
namespace UnityEngine.InputSystem {
    public class KeyControl { public bool isPressed; }
    public class Keyboard { public static Keyboard? current=new(); public KeyControl hKey=new(),oKey=new(); }
}

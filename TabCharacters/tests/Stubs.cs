using System.Reflection;

namespace HarmonyLib
{
    public sealed class HarmonyMethod { public HarmonyMethod(MethodInfo method) { } }
    public sealed class Harmony
    {
        public void Patch(MethodInfo method, HarmonyMethod? prefix, HarmonyMethod? postfix, HarmonyMethod? finalizer) { }
        public void UnpatchSelf() { }
    }
}
namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)] public sealed class MelonInfoAttribute : Attribute
    { public MelonInfoAttribute(Type type, string name, string version, string author) { } }
    [AttributeUsage(AttributeTargets.Assembly)] public sealed class MelonGameAttribute : Attribute
    { public MelonGameAttribute(string company, string game) { } }
    public class Log { public void Msg(string text) { } public void Warning(string text) { } public void Error(string text) { } }
    public class MelonMod
    {
        public readonly Log LoggerInstance = new();
        public readonly HarmonyLib.Harmony HarmonyInstance = new();
        public virtual void OnInitializeMelon() { }
        public virtual void OnDeinitializeMelon() { }
        public virtual void OnUpdate() { }
    }
}
namespace MelonLoader.Utils { public static class MelonEnvironment { public static string GameRootDirectory => "."; } }
namespace Il2CppFairyGUI
{
    public static class Hook
    {
        public static object? Call(string name, params object?[] args) => typeof(Restitutor.TabCharacters.EntryPoint)
            .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args);
    }
    public class GObject
    {
        private static long counter;
        public IntPtr Pointer { get; } = (IntPtr)Interlocked.Increment(ref counter);
        public bool isDisposed;
        public int CharacterIndex;
        public Action? Click;
        public GObject? parent;
        public T? TryCast<T>() where T : class => this as T;
    }
    public enum ListLayoutType { SingleColumn, SingleRow, FlowHorizontal, FlowVertical, Pagination }
    public enum AlignType { Left, Center, Right }
    public enum ListSelectionMode { Single, Multiple, Multiple_SingleClick, None }
    public sealed class ListItemRenderer : GObject
    {
        private readonly Action<int, GObject> action;
        public ListItemRenderer(Action<int, GObject> action) { this.action = action; }
        public static implicit operator ListItemRenderer(Action<int, GObject> action) => new(action);
        public void Invoke(int index, GObject row) => action(index, row);
    }
    public sealed class ScrollPane
    {
        public bool touchEffect = true, mouseWheelEnabled = true;
        public float X = 17, Y = 0;
        public bool Tweening = true;
        public void CancelDragging() { }
        public void KillTween() { Tweening = false; }
        public void SetPosX(float value, bool animation) { X = value; Tweening = animation; }
        public void SetPosY(float value, bool animation) { Y = value; Tweening = animation; }
    }
    public sealed class GList : GObject
    {
        public GObjectPool itemPool = new();
        public ListItemRenderer itemRenderer = (ListItemRenderer)(Action<int, GObject>)((_, _) => { });
        public ListLayoutType layout = ListLayoutType.SingleRow;
        public AlignType align = AlignType.Left;
        public ListSelectionMode selectionMode = ListSelectionMode.Single;
        public bool scrollItemToViewOnClick = true, isVirtual;
        public ScrollPane scrollPane = new();
        public readonly List<GObject> Rows = new();
        public int RenderCalls, ScrollCalls;
        public int numItems
        {
            get => Rows.Count;
            set
            {
                object?[] args = { this, value };
                Hook.Call("LimitCount", args);
                int count = (int)args[1]!;
                while (Rows.Count > count)
                {
                    var row = Rows[^1]; Rows.RemoveAt(Rows.Count - 1); row.parent = null;
                    itemPool._pool["portraits"].Enqueue(row);
                }
                while (Rows.Count < count)
                {
                    var queue = itemPool._pool["portraits"];
                    var row = queue.Count != 0 ? queue.Dequeue() : new Il2CppCharacter.UIbtnRole();
                    row.parent = this; Rows.Add(row);
                }
                for (int i = 0; i < count; ++i) { RenderCalls++; itemRenderer.Invoke(i, Rows[i]); }
            }
        }
        public void EnsureBoundsCorrect() { }
        public void ScrollToView(int index, bool animate, bool setFirst)
        {
            if ((bool)Hook.Call("AllowScroll", this)!) { ScrollCalls++; scrollPane.SetPosX(index * 100, animate); }
        }
        public int HandleArrowKey(int direction)
        {
            object?[] args = { this, 99 };
            return (bool)Hook.Call("AllowArrow", args)! ? 99 : (int)args[1]!;
        }
        public void Dispose() { Hook.Call("BeforeListDispose", this); isDisposed = true; Rows.Clear(); }
    }
}
namespace Il2CppClient.UILogic.UICharacter
{
    using Il2CppFairyGUI;
    public enum ESheetType { Character, Skill, Equip, Other = 9 } // Other: test-only non-retired sheet
    public sealed class UICharacterModel : GObject
    {
        public ESheetType SheetType;
        public List<int> ListRole = new();
        public int RoleIndex;
        public bool IsSelectEquipFilter, IsSelectSkillFilter;
    }
    public sealed class Content : GObject
    {
        public GList listRole = new();
        public GLoader loaderRole = new Il2CppCore.NewUISystem.MyGLoader();
        public Il2CppCharacter.UICom_RoleInfo roleInfo = new();
    }
    public sealed class UICharacterView : GObject
    {
        public UICharacterModel _model = new();
        public Content? _UIContent_k__BackingField = new();
        public bool IsInputActive = true;
        public int DetailsRefreshes;
        public void Refresh()
        {
            Hook.Call("BeforeRefresh", this);
            _UIContent_k__BackingField!.listRole.numItems = _model.ListRole.Count;
            _UIContent_k__BackingField.listRole.ScrollToView(_model.RoleIndex, false, false);
            DetailsRefreshes++;
            Hook.Call("AfterRefresh", this);
        }
        public void HideHook() { Hook.Call("BeforeHide", this); }
    }
    public sealed class UICharacterCtrl
    {
        public UICharacterModel _Model_k__BackingField;
        public UICharacterView _View_k__BackingField;
        public UICharacterCtrl(UICharacterView view) { _View_k__BackingField = view; _Model_k__BackingField = view._model; }
        public void ShowSheet(ESheetType sheet)
        { var args = new object?[] { this, sheet }; Hook.Call("BeforeSheet", args); sheet = (ESheetType)args[1]!; _Model_k__BackingField.SheetType = sheet; _View_k__BackingField.Refresh(); }
        public void OnClickBtnLeft() { Hook.Call("Left", this); }
        public void OnClickBtnRight() { Hook.Call("Right", this); }
    }
}
namespace Il2CppFairyGUI
{
    public class GLoader : GObject
    {
        private string currentUrl = "";
        public bool Loaded;
        public int UrlWrites;
        public bool ThrowOnClear;
        public string url
        {
            get => currentUrl;
            set
            {
                if (value == currentUrl) return;
                if (value.Length == 0 && ThrowOnClear) throw new Exception("loader clear failure");
                UrlWrites++;
                if (Loaded && this is Il2CppCore.NewUISystem.MyGLoader loader) loader.ReleaseLoader();
                Loaded = false; currentUrl = value;
            }
        }
    }
    public sealed class GObjectPool
    {
        public Dictionary<string, Queue<GObject>> _pool = new() { ["portraits"] = new() };
        public int count => _pool.Values.Sum(q => q.Count);
    }
}
namespace Il2CppCore.NewUISystem
{
    public sealed class MyGLoader : Il2CppFairyGUI.GLoader
    {
        public object? _handler;
        public int Releases;
        public void ReleaseLoader() { if (_handler != null) { Releases++; _handler = null; } }
    }
}
namespace Il2CppCharacter
{
    public sealed class UIbtnRole : Il2CppFairyGUI.GObject
    { public Il2CppFairyGUI.GLoader loaderRole = new Il2CppCore.NewUISystem.MyGLoader(); }
    public sealed class UICom_RoleInfo
    { public Il2CppFairyGUI.GLoader loaderRole = new Il2CppCore.NewUISystem.MyGLoader(); }
}

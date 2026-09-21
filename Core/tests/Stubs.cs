// Stand-ins for MelonLoader, Harmony and the game so Core logic runs without Unity.
namespace MelonLoader
{
    public delegate void LemonAction();
    public class MelonLogger { public class Instance { public readonly List<string> Lines = new(); public static readonly List<string> All = new(); public Instance() {} public Instance(string name) {} public void Msg(string s) { Lines.Add("MSG " + s); All.Add("MSG " + s); } public void Error(string s) { Lines.Add("ERR " + s); All.Add("ERR " + s); } } }
    public class MelonEvent
    {
        public readonly List<LemonAction> Subs = new();
        public void Subscribe(LemonAction a, int priority = 0, bool unsubscribeOnFirstInvocation = false) { if (!Subs.Contains(a)) Subs.Add(a); }
        public void Unsubscribe(LemonAction a) => Subs.Remove(a);
        public void Invoke() { foreach (var a in Subs.ToArray()) a(); }
    }
    public static class MelonEvents { public static readonly MelonEvent OnUpdate = new(); }
}
namespace HarmonyLib
{
    using System.Reflection;
    public class HarmonyMethod { public readonly MethodInfo method; public HarmonyMethod(MethodInfo m) { method = m ?? throw new ArgumentNullException(); } }
    public class Harmony
    {
        public readonly List<(MethodBase Original, HarmonyMethod? Pre, HarmonyMethod? Post, HarmonyMethod? Fin)> Patches = new();
        public int Unpatched;
        public MethodInfo? Patch(MethodBase original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null)
        { Patches.Add((original, prefix, postfix, finalizer)); return null; }
        public void UnpatchSelf() { Unpatched++; Patches.Clear(); }
    }
}
namespace Il2CppCore.SceneSystem
{
    public class SceneManager { public static SceneManager? Instance; public static bool Throw; public bool Entered; public bool IsSceneEntered => Throw ? throw new Exception("native") : Entered; }
}
namespace Il2CppClient.Manager { public class UIManager { public void SetFocusOnUIView() {} } }

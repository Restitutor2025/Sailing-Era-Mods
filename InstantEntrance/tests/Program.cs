using System.Reflection;
using Restitutor.InstantEntrance;
using Il2CppClient.WorldLogic.Entity.Component.PortNote;
using Il2CppClient.WorldLogic.Map.GOComponent;

static object? Invoke(string name, params object?[] args) => typeof(EntryPoint).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args);
int checks = 0;
void Check(bool result) { checks++; if (!result) throw new Exception("Check " + checks); }
float Play(string name, float time) { object?[] args = { name, time }; Invoke("CompleteEntryAnimation", args); return (float)args[1]!; }
object? Begin(string name) { object?[] args = { new EntityPortNoteAnimator { ENTER_ANIM_NAME = name }, null }; Invoke("BeginEntry", args); return args[1]; }
Check(Play("enter", 0f) == 0f);
var outer = Begin("enter");
Check(Play("enter", 0f) == 1f);
Check(Play("exit", 0.25f) == 0.25f);
bool separateThread = false;
var thread = new Thread(() => separateThread = Play("enter", 0f) == 0f);
thread.Start(); thread.Join(); Check(separateThread);
var inner = Begin("nested");
Check(Play("enter", 0f) == 0f);
Check(Play("nested", 0f) == 1f);
var error = new Exception("native error");
Check(ReferenceEquals(Invoke("EndEntry", error, inner), error));
Check(Play("enter", 0f) == 1f);
Invoke("EndEntry", null, outer);
Check(Play("enter", 0f) == 0f);
var harbour = new MapHarbourEnterCtrl();
Invoke("ReadyAfterStart", harbour); Check(harbour._canEnterHarbour);
// Install through Restitutor.Core: same three targets, same handlers, same order as 0.1.0.
var mod = new EntryPoint(); mod.OnInitializeMelon();
var p = mod.HarmonyInstance.Patches;
Check(p.Count == 3);
Check(p[0].o.DeclaringType == typeof(MapHarbourEnterCtrl) && p[0].o.Name == "Start" && p[0].pre == null && p[0].post!.method.Name == "ReadyAfterStart" && p[0].fin == null);
Check(p[1].o.DeclaringType == typeof(EntityPortNoteAnimator) && p[1].o.Name == "PlayEnterAnimation" && p[1].pre!.method.Name == "BeginEntry" && p[1].post == null && p[1].fin!.method.Name == "EndEntry");
Check(p[2].o.DeclaringType == typeof(UnityEngine.Animator) && p[2].o.Name == "Play" && p[2].o.GetParameters().Select(x => x.ParameterType).SequenceEqual(new[] { typeof(string), typeof(int), typeof(float) }) && p[2].pre!.method.Name == "CompleteEntryAnimation" && p[2].post == null && p[2].fin == null);
Check(mod.HarmonyInstance.Unpatched == 0 && mod.LoggerInstance.Lines.Count == 1 && mod.LoggerInstance.Lines[0].StartsWith("MSG 0.1.1 (Restitutor.Core 0.1.0)"));
Console.WriteLine($"PASS: {checks} managed checks. No Unity/game/Steam execution; Harmony/native dispatch not tested.");

namespace Il2CppClient.WorldLogic.Map.GOComponent { public class MapHarbourEnterCtrl { public bool _canEnterHarbour; public void Start() {} } }
namespace Il2CppClient.WorldLogic.Entity.Component.PortNote { public class EntityPortNoteAnimator { public string ENTER_ANIM_NAME = ""; public void PlayEnterAnimation() {} } }
namespace UnityEngine { public class Animator { public void Play(string name, int layer, float time) {} } }
namespace HarmonyLib {
 public class HarmonyMethod { public readonly MethodInfo method; public HarmonyMethod(MethodInfo method) { this.method = method; } }
 public class Harmony { public readonly List<(MethodBase o, HarmonyMethod? pre, HarmonyMethod? post, HarmonyMethod? fin)> Patches = new(); public int Unpatched;
  public MethodInfo? Patch(MethodBase original, HarmonyMethod? prefix=null, HarmonyMethod? postfix=null, HarmonyMethod? transpiler=null, HarmonyMethod? finalizer=null) { Patches.Add((original, prefix, postfix, finalizer)); return null; }
  public void UnpatchSelf() { Unpatched++; Patches.Clear(); } }
}
namespace MelonLoader {
 public class MelonInfoAttribute : Attribute { public MelonInfoAttribute(Type type,string name,string version,string author) {} }
 public class MelonGameAttribute : Attribute { public MelonGameAttribute(string developer,string game) {} }
 public class MelonMod { public HarmonyLib.Harmony HarmonyInstance = new(); public MelonLogger.Instance LoggerInstance = new(); public virtual void OnInitializeMelon() {} }
 public class MelonLogger { public class Instance { public readonly List<string> Lines = new(); public Instance() {} public Instance(string name) {} public void Msg(string text) => Lines.Add("MSG " + text); public void Error(string text) => Lines.Add("ERR " + text); } }
 public delegate void LemonAction();
 public class MelonEvent { public void Subscribe(LemonAction a, int priority = 0, bool unsubscribeOnFirstInvocation = false) {} public void Unsubscribe(LemonAction a) {} }
 public static class MelonEvents { public static readonly MelonEvent OnUpdate = new(); }
}
namespace Il2CppCore.SceneSystem { public class SceneManager { public static SceneManager? Instance; public bool IsSceneEntered => false; } }

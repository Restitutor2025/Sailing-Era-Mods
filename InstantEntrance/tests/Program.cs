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
Console.WriteLine($"PASS: {checks} managed checks. No Unity/game/Steam execution; Harmony/native dispatch not tested.");

namespace Il2CppClient.WorldLogic.Map.GOComponent { public class MapHarbourEnterCtrl { public bool _canEnterHarbour; public void Start() {} } }
namespace Il2CppClient.WorldLogic.Entity.Component.PortNote { public class EntityPortNoteAnimator { public string ENTER_ANIM_NAME = ""; public void PlayEnterAnimation() {} } }
namespace UnityEngine { public class Animator { public void Play(string name, int layer, float time) {} } }
namespace HarmonyLib {
 public class HarmonyMethod { public HarmonyMethod(MethodInfo method) {} }
 public class Harmony { public void Patch(MethodInfo original, HarmonyMethod? prefix=null, HarmonyMethod? postfix=null, HarmonyMethod? finalizer=null) {} public void UnpatchSelf() {} }
}
namespace MelonLoader {
 public class MelonInfoAttribute : Attribute { public MelonInfoAttribute(Type type,string name,string version,string author) {} }
 public class MelonGameAttribute : Attribute { public MelonGameAttribute(string developer,string game) {} }
 public class MelonMod { public HarmonyLib.Harmony HarmonyInstance = new(); public MelonLogger.Instance LoggerInstance = new(); public virtual void OnInitializeMelon() {} }
 public class MelonLogger { public class Instance { public void Msg(string text) {} public void Error(string text) {} } }
}

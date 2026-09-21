using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Restitutor.Core;
using SceneManager = Il2CppCore.SceneSystem.SceneManager;

int checks = 0;
void Check(bool ok, string what) { checks++; if (!ok) throw new Exception($"Check {checks} failed: {what}"); }
bool Throws<T>(Action a) where T : Exception { try { a(); return false; } catch (T) { return true; } }
void Frame() => MelonEvents.OnUpdate.Invoke();

// Version
Check(VersionText.AtLeast("0.1.0", "0.1.0"), "equal");
Check(VersionText.AtLeast("0.2.0", "0.1.9"), "minor");
Check(VersionText.AtLeast("1.0.0", "0.9.9"), "major");
Check(!VersionText.AtLeast("0.1.0", "0.1.1"), "older patch");
Check(!VersionText.AtLeast("0.1.10", "0.2.0"), "numeric not text");
Check(VersionText.AtLeast("0.1.10", "0.1.9"), "10 > 9");
Check(VersionText.AtLeast("0.1.0-beta", "0.1.0"), "suffix ignored");
Check(Throws<FormatException>(() => VersionText.Parse("0.1")), "two parts");
Check(Throws<FormatException>(() => VersionText.Parse("0.1.x")), "non-number");
Check(Throws<FormatException>(() => VersionText.Parse("0.-1.0")), "negative");
Check(typeof(CoreInfo).GetProperty("Version") != null && typeof(CoreInfo).GetField("Version") == null, "Version is a property, not a const");
Check(CoreInfo.Version == "0.1.0" && CoreInfo.Satisfies("0.1.0") && !CoreInfo.Satisfies("0.2.0"), "CoreInfo");
var modLog = new MelonLogger.Instance("mod");
Check(!CoreInfo.Require(modLog, "9.0.0") && modLog.Lines.Count == 1 && modLog.Lines[0].StartsWith("ERR"), "Require logs one line");
Check(CoreInfo.Require(modLog, "0.1.0") && modLog.Lines.Count == 1, "Require silent when met");

// Method lookup
Check(MethodLookup.Unique(typeof(Target), "Single").Name == "Single", "unique");
Check(Throws<MissingMethodException>(() => MethodLookup.Unique(typeof(Target), "Over")), "overload without args is ambiguous");
Check(MethodLookup.Unique(typeof(Target), "Over", new[] { typeof(int) }).GetParameters()[0].ParameterType == typeof(int), "overload by args");
Check(MethodLookup.Unique(typeof(Target), "Over", new[] { typeof(int), typeof(int).MakeByRefType() }).GetParameters().Length == 2, "by-ref args");
Check(Throws<MissingMethodException>(() => MethodLookup.Unique(typeof(Target), "Missing")), "missing");
Check(Throws<MissingMethodException>(() => MethodLookup.Unique(typeof(Derived), "Inherited")), "declaredOnly skips inherited");
Check(MethodLookup.Unique(typeof(Derived), "Inherited", declaredOnly: false).DeclaringType == typeof(Target), "declaredOnly:false finds inherited");
Check(MethodLookup.Unique(typeof(Target), "StaticOne").IsStatic && MethodLookup.Unique(typeof(Target), "Hidden").IsPrivate, "static and private");
Check(MethodLookup.Handler(typeof(Handlers), "Pre").Name == "Pre" && MethodLookup.Handler(typeof(Handlers), "PublicPost").IsPublic, "handlers");
Check(Throws<MissingMethodException>(() => MethodLookup.Handler(typeof(Handlers), "Nope")), "missing handler");
Check(Throws<MissingMethodException>(() => MethodLookup.Handler(typeof(Handlers), "Twice")), "ambiguous handler");
Check(Throws<MissingMethodException>(() => MethodLookup.Handler(typeof(Handlers), "Instance")), "instance handler rejected");

// Deferred queue
var order = new List<string>(); var errors = new List<string>();
var q = new DeferredQueue();
q.Add("a", () => order.Add("a")); q.Add("b", () => throw new Exception("boom")); q.Add("c", () => { order.Add("c"); q.Add("d", () => order.Add("d")); });
q.RunAll((o, e) => errors.Add(o));
Check(string.Join(",", order) == "a,c" && errors.SequenceEqual(new[] { "b" }), "order + isolation");
Check(q.Count == 1, "added while running waits");
q.RunAll((o, e) => throw new Exception("error handler itself fails"));
Check(string.Join(",", order) == "a,c,d" && q.Count == 0, "second run");
Check(Throws<ArgumentException>(() => q.Add("", () => { })) && Throws<ArgumentNullException>(() => q.Add("x", null!)), "arguments");

// HookSet before ready
var h = new Harmony(); var hooks = new HookSet(h, typeof(Handlers));
Check(MelonEvents.OnUpdate.Subs.Count == 1, "Core watches readiness after first use");
Check(MelonLogger.Instance.All.Any(l => l.Contains("Restitutor.Core 0.1.0 loaded")), "load line");
hooks.Hook(typeof(Target), "Single", prefix: "Pre", postfix: "PublicPost", finalizer: "Fin");
Check(h.Patches.Count == 1 && h.Patches[0].Pre!.method.Name == "Pre" && h.Patches[0].Post!.method.Name == "PublicPost" && h.Patches[0].Fin!.method.Name == "Fin", "patched with all handlers");
Check(Throws<MissingMethodException>(() => hooks.Hook(typeof(Target), "StaticOne", prefix: "Pre", postfix: "Nope")) && h.Patches.Count == 1, "missing handler patches nothing");
Check(Throws<ArgumentException>(() => hooks.Hook(typeof(Target), "StaticOne")) && h.Patches.Count == 1, "no handler rejected");
Check(Throws<InvalidOperationException>(() => hooks.Hook(typeof(Il2CppClient.Manager.UIManager), "SetFocusOnUIView", postfix: "PublicPost")) && h.Patches.Count == 1, "UIManager refused before ready");
var log = new MelonLogger.Instance("mod");
Check(hooks.InstallAll(log, "Install", () => hooks.Hook(typeof(Target), "Hidden", prefix: "Pre")) && h.Patches.Count == 2 && log.Lines.Count == 0, "InstallAll success");
Check(!hooks.InstallAll(log, "Install", () => { hooks.Hook(typeof(Target), "StaticOne", prefix: "Pre"); hooks.Hook(typeof(Target), "Missing", prefix: "Pre"); })
      && h.Patches.Count == 0 && h.Unpatched == 1 && log.Lines.Count == 1 && log.Lines[0].StartsWith("ERR"), "InstallAll failure removes every hook, one error");

// GameReady
var ran = new List<string>();
GameReady.Run("m1", () => ran.Add("m1"));
GameReady.Run("m2", () => throw new Exception("m2 broke"));
GameReady.Run("m3", () => { ran.Add("m3"); GameReady.Run("m3-nested", () => ran.Add("nested")); });
Frame(); Check(ran.Count == 0 && !GameReady.IsReady, "no SceneManager yet");
SceneManager.Instance = new SceneManager(); Frame(); Check(ran.Count == 0, "scene not entered");
SceneManager.Throw = true; Frame(); Check(ran.Count == 0 && !GameReady.IsReady, "native exception = not ready");
SceneManager.Throw = false; SceneManager.Instance.Entered = true; Frame();
Check(GameReady.IsReady && string.Join(",", ran) == "m1,m3,nested", "ran once in order, nested ran immediately");
Check(MelonLogger.Instance.All.Any(l => l.StartsWith("ERR") && l.Contains("m2") && l.Contains("m2 broke")), "failure logged with owner");
Check(MelonEvents.OnUpdate.Subs.Count == 0, "frame callback removed once ready");
Frame(); Check(ran.Count == 3, "never runs twice");
GameReady.Run("late", () => ran.Add("late")); Check(ran.Last() == "late" && MelonEvents.OnUpdate.Subs.Count == 0, "after ready runs immediately without watching");
GameReady.Run("late-bad", () => throw new Exception("x")); Check(true, "late failure isolated");
var h2 = new Harmony(); new HookSet(h2, typeof(Handlers)).Hook(typeof(Il2CppClient.Manager.UIManager), "SetFocusOnUIView", postfix: "PublicPost");
Check(h2.Patches.Count == 1, "UIManager allowed after ready");

Console.WriteLine($"PASS: {checks} checks. Managed stubs only; no Unity, game, Harmony native patching or MelonLoader runtime.");

class Target
{
    public void Single() {}
    public void Over(int a) {}
    public void Over(string a) {}
    public void Over(int a, ref int b) {}
    public static void StaticOne() {}
    private void Hidden() {}
    public void Inherited() {}
}
class Derived : Target {}
class Handlers
{
    static void Pre() {}
    public static void PublicPost() {}
    static Exception? Fin(Exception? e) => e;
    static void Twice() {}
    static void Twice(int a) {}
    public void Instance() {}
}

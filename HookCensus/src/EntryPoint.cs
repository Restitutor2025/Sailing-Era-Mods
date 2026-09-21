using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using SceneManager = Il2CppCore.SceneSystem.SceneManager;

[assembly: MelonInfo(typeof(Restitutor.HookCensus.EntryPoint), "Restitutor Analytics HookCensus", "0.1.0", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.HookCensus;

// Diagnostic only: counts calls of native methods that other Restitutor mods hook, so the
// resource audit (handoff/RESOURCE_AUDIT_2026-09-22.md, section B) is decided on data.
// Each counter prefix only increments a long. Remove this DLL after one play session.
public sealed class EntryPoint : MelonMod
{
    private sealed record Target(string Type, string Method, int Params, string? FirstParam);
    private static readonly Target[] targets = {
        new("Transition","_Play",-1,null),
        new("BaseObjectData","GetPointProperty",-1,null),
        new("BaseObjectData","GetProperty",-1,null),
        new("BoatEntityOceanDriver","FixedUpdate",0,null),
        new("Animator","Play",3,"String"),
        new("GList","set_numItems",1,null),
        new("GList","ScrollToView",3,null),
        new("GList","HandleArrowKey",1,null),
        new("GList","Dispose",0,null),
        new("FunctionOpenDB","GetFunctionData",1,"Int32"),
        new("PlayerBagDB","GetFreeCapacity",-1,null),
        new("UIMapHarbourIcon","UpdateInfo",0,null),
        new("UIBase","get_IsInputActive",0,null),
        new("InputSystemManager","OnEventCaptureInput",-1,null),
        new("UIManager","SetFocusOnUIView",-1,null),
        new("UIPropStoreModel","get_SellCount",0,null),
        new("UIMapView","Refresh",0,null),
        new("UIHarborCtrl","ShowMainUI",0,null),
        new("UIHeroLevelUpModel","get_LevelGetSkill",0,null)
    };
    private static readonly long[] counts = new long[19];
    private static readonly bool[] active = new bool[19];
    private static long windowStart;
    private static int lines;
    private static string lastScene = "";
    private static readonly Dictionary<string, (double seconds, long[] totals)> perScene = new();

    // Patched on the first frame after a scene is entered, not in OnInitializeMelon: resolving a
    // UIManager method at load runs its .cctor and creates FairyGUI.Stage too early (black screen).
    private bool installed;
    private void Install()
    {
        installed = true;
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name is string n && (n.StartsWith("Assembly-CSharp") || n.StartsWith("Il2Cpp") || n.StartsWith("UnityEngine")))
            .SelectMany(a => { try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).Select(t => t!).ToArray(); } })
            .ToArray();
        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            try
            {
                var methods = types.Where(x => x.Name == t.Type)
                    .SelectMany(o => o.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    .Where(m => m.Name == t.Method && (t.Params < 0 || m.GetParameters().Length == t.Params)
                        && (t.FirstParam == null || (m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType.Name == t.FirstParam))).ToArray();
                if (methods.Length != 1) { LoggerInstance.Warning($"skip {t.Type}.{t.Method}: {methods.Length} matches"); continue; }
                HarmonyInstance.Patch(methods[0], prefix: new HarmonyMethod(typeof(EntryPoint).GetMethod($"C{i:00}", BindingFlags.NonPublic | BindingFlags.Static)!));
                active[i] = true;
            }
            catch (Exception ex) { LoggerInstance.Warning($"skip {t.Type}.{t.Method}: {ex.Message}"); }
        }
        LoggerInstance.Msg($"HookCensus 0.1.0: counting {active.Count(a => a)}/{targets.Length} methods. One [CENSUS] line per 5 s (calls per second); per-scene averages on exit. Diagnostic only.");
    }

    private static string Scene()
    {
        try
        {
            var s = SceneManager.Instance;
            if (s == null || !s.IsSceneEntered || s.IsInLoadingOrStarting) return "loading";
            if (s.IsInHarborScene) return "harbor";
            if (s.IsInOceanScene) return "ocean";
            return "other";
        }
        catch { return "?"; }
    }

    public override void OnUpdate()
    {
        if (!installed)
        {
            try { var s = SceneManager.Instance; if (s == null || !s.IsSceneEntered) return; } catch { return; }
            Install();
        }
        long now = Stopwatch.GetTimestamp();
        if (windowStart == 0) { windowStart = now; lastScene = Scene(); return; }
        double sec = (now - windowStart) / (double)Stopwatch.Frequency;
        if (sec < 5) return;
        var parts = new List<string>();
        if (!perScene.TryGetValue(lastScene, out var acc)) acc = (0, new long[counts.Length]);
        acc.seconds += sec;
        for (int i = 0; i < counts.Length; i++)
        {
            long c = counts[i]; counts[i] = 0; acc.totals[i] += c;
            if (c > 0) parts.Add($"{targets[i].Type}.{targets[i].Method}={c / sec:0}");
        }
        perScene[lastScene] = acc;
        if (parts.Count > 0 && lines++ < 3000) LoggerInstance.Msg($"[CENSUS] {sec:0.0}s scene={lastScene}: " + string.Join(" ", parts));
        windowStart = now; lastScene = Scene();
    }

    public override void OnDeinitializeMelon()
    {
        foreach (var (scene, acc) in perScene)
        {
            var parts = new List<string>();
            for (int i = 0; i < counts.Length; i++) if (acc.totals[i] > 0) parts.Add($"{targets[i].Type}.{targets[i].Method}={acc.totals[i] / Math.Max(1, acc.seconds):0}/s");
            LoggerInstance.Msg($"[CENSUS-TOTAL] scene={scene} {acc.seconds:0}s: " + string.Join(" ", parts));
        }
    }

    private static void C00() => counts[0]++;
    private static void C01() => counts[1]++;
    private static void C02() => counts[2]++;
    private static void C03() => counts[3]++;
    private static void C04() => counts[4]++;
    private static void C05() => counts[5]++;
    private static void C06() => counts[6]++;
    private static void C07() => counts[7]++;
    private static void C08() => counts[8]++;
    private static void C09() => counts[9]++;
    private static void C10() => counts[10]++;
    private static void C11() => counts[11]++;
    private static void C12() => counts[12]++;
    private static void C13() => counts[13]++;
    private static void C14() => counts[14]++;
    private static void C15() => counts[15]++;
    private static void C16() => counts[16]++;
    private static void C17() => counts[17]++;
    private static void C18() => counts[18]++;
}

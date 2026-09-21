using System.Runtime.CompilerServices;
using MelonLoader;
using SceneManager = Il2CppCore.SceneSystem.SceneManager;

namespace Restitutor.Core;

/// <summary>
/// "Game ready" = the first frame on which SceneManager reports a scene entered.
/// Hooks on types whose native static constructor builds UI (UIManager -> FairyGUI.Stage)
/// must not be resolved before this; doing it in OnInitializeMelon gave a black screen
/// (Item Rebuild 0.1.0, docs/mods/item-rebuild/0.1.1.md).
/// Cost: one check per frame only until ready, then the frame callback is removed.
/// </summary>
public static class GameReady
{
    private static readonly DeferredQueue queue = new();
    private static bool watching;

    /// <summary>True from the first frame a scene has been entered.</summary>
    public static bool IsReady { get; private set; }

    static GameReady()
    {
        CoreLog.Msg($"Restitutor.Core {CoreInfo.Version} loaded.");
        Watch();
    }

    /// <summary>Makes sure the readiness watch runs; called by anything in Core that depends on it.</summary>
    internal static void Touch() { }

    /// <summary>Runs <paramref name="action"/> once the game is ready (immediately if it already is).
    /// An exception is logged with <paramref name="owner"/> and never reaches other mods or the game.</summary>
    public static void Run(string owner, Action action)
    {
        if (IsReady) { DeferredQueue.RunIsolated(owner, action, Report); return; }
        queue.Add(owner, action);
        Watch();
    }

    private static void Watch()
    {
        if (watching || IsReady) return;
        watching = true;
        MelonEvents.OnUpdate.Subscribe(Tick);
    }

    private static void Tick()
    {
        try
        {
            if (IsReady || !SceneEntered()) return;
            IsReady = true;
            // MelonEvent invokes a snapshot array, so removing ourselves here is safe.
            MelonEvents.OnUpdate.Unsubscribe(Tick);
            watching = false;
            CoreLog.Msg($"Game ready; running {queue.Count} deferred registration(s).");
            queue.RunAll(Report);
        }
        catch (Exception ex) { CoreLog.Error("Readiness check: " + ex); }
    }

    private static void Report(string owner, Exception ex) => CoreLog.Error($"{owner}: deferred registration failed: {ex}");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SceneEntered()
    {
        try { var s = SceneManager.Instance; return s != null && s.IsSceneEntered; }
        catch { return false; }
    }
}

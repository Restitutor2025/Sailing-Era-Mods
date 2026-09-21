using System.Diagnostics;
using Il2CppClient.Manager;
using Il2CppClient.WorldLogic.Scenes;

namespace Restitutor.Map;

// Measurement only (0.2.2-diag, kept in 0.2.3 for verification). Counters are advanced from the existing
// hooks and flushed from those same hooks once per second of activity (no OnUpdate polling).
internal static class MapDiag
{
    private static long windowStart;
    private static int portInfo, nativeRed, routeRefresh, viewRefresh, inputAll, inputSession;
    private static int lines;
    private const int MaxLines = 400; // per game run
    private static long Now => Stopwatch.GetTimestamp();
    private static double Seconds(long from) => (Now - from) / (double)Stopwatch.Frequency;

    // Integer increments only; a line is written from these same hooks at most once per second.
    internal static void PortInfo() { portInfo++; Flush(); }
    internal static void NativeRed() { nativeRed++; }
    internal static void Route() { routeRefresh++; Flush(); }
    internal static void View() { viewRefresh++; Flush(); }
    internal static void Input(bool session) { inputAll++; if (session) { inputSession++; Flush(); } }

    private static void Flush()
    {
        if (windowStart == 0) { windowStart = Now; return; }
        if (Seconds(windowStart) < 1) return;
        if (lines++ < MaxLines)
            EntryPoint.Log.Msg($"[MAPDIAG] {Seconds(windowStart):0.00}s: UIMapView.Refresh={viewRefresh} UpdateInfo={portInfo} nativeRedCalls={nativeRed} " +
                $"RefreshRoute={routeRefresh} IsInputActive(all managed calls)={inputAll} (during session={inputSession})");
        windowStart = Now; portInfo = nativeRed = routeRefresh = viewRefresh = inputAll = inputSession = 0;
    }

    // Ready-screen lifecycle, one line per event.
    private static long sessionStart;
    private static bool lateLogged;
    private static long mapDownAt;
    internal static void Event(string what)
    {
        if (lines++ >= MaxLines) return;
        var t = TransitionManager.Instance;
        string flags = t == null ? "transition=null" :
            $"gather={t.IsInGatherState} enterBlack={t.IsEnterBlackScreen} black={t.IsInBlackScreen}";
        EntryPoint.Log.Msg($"[MAPDIAG] +{(sessionStart == 0 ? 0 : Seconds(sessionStart)):0.000}s {what}; {flags}");
    }
    internal static void Begin(bool harbor, bool playTransition)
    { sessionStart = Now; mapDownAt = 0; lateLogged = false; windowStart = 0; Event($"session begin harbor={harbor} PlayTransition={playTransition}"); }
    internal static void MapDown(bool playTransition, bool dissolve)
    { mapDownAt = Now; Event($"map loaded (MapIsDown) PlayTransition={playTransition} LineDissolve={(dissolve ? "called" : "skipped")}"); }
    // Called from the existing per-frame native ReadyView.UpdateHook replacement; one-shot.
    internal static void ReadyFrame()
    {
        if (lateLogged || mapDownAt == 0 || Seconds(mapDownAt) < 2) return;
        lateLogged = true; Event("2s after map loaded");
    }
}

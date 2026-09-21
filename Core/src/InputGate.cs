using System.Reflection;
using HarmonyLib;
using Il2CppCore.InputSystem;
using UnityEngine.InputSystem;

namespace Restitutor.Core;

/// <summary>
/// One shared prefix on <c>InputSystemManager.OnEventCaptureInput</c> for every Restitutor mod.
/// Each mod registers a handler that returns false to swallow the input (as its own bool prefix did).
/// Every handler runs on every call (no short-circuit, like separate Harmony prefixes), the input is
/// swallowed if any handler returns false, and a handler that throws is logged once and counts as
/// "allow" so one broken mod never blocks input or reaches the native caller.
/// Removing a mod's registration (or its DLL) removes only its handler.
/// </summary>
public static class InputGate
{
    private sealed class Entry : IDisposable
    {
        internal readonly string Owner;
        internal readonly Func<InputAction.CallbackContext, bool> Allow;
        internal bool Reported;
        internal Entry(string owner, Func<InputAction.CallbackContext, bool> allow) { Owner = owner; Allow = allow; }
        public void Dispose() => Remove(this);
    }

    private static readonly object sync = new();
    private static Entry[] entries = Array.Empty<Entry>(); // copy-on-write: dispatch never sees a half-updated list
    private static HarmonyLib.Harmony? harmony;

    /// <summary>Registered handlers (diagnostics/tests).</summary>
    public static int Count => entries.Length;

    /// <summary>Adds a handler; the shared hook is installed on the first registration.
    /// Throws if the hook cannot be installed (the caller's install then fails as a whole).</summary>
    public static IDisposable Register(string owner, Func<InputAction.CallbackContext, bool> allow)
    {
        if (string.IsNullOrEmpty(owner)) throw new ArgumentException("owner is required", nameof(owner));
        if (allow == null) throw new ArgumentNullException(nameof(allow));
        lock (sync)
        {
            EnsureHook();
            var entry = new Entry(owner, allow);
            entries = entries.Append(entry).ToArray();
            return entry;
        }
    }

    private static void Remove(Entry entry)
    {
        lock (sync) entries = entries.Where(e => !ReferenceEquals(e, entry)).ToArray();
    }

    private static void EnsureHook()
    {
        if (harmony != null) return;
        var target = MethodLookup.Unique(typeof(InputSystemManager), "OnEventCaptureInput");
        var h = new HarmonyLib.Harmony("restitutor.core.input");
        h.Patch(target, prefix: new HarmonyMethod(MethodLookup.Handler(typeof(InputGate), nameof(Dispatch))));
        harmony = h;
        CoreLog.Msg("Input gate installed on InputSystemManager.OnEventCaptureInput.");
    }

    private static bool Dispatch(InputAction.CallbackContext __0)
    {
        var list = entries;
        bool allow = true;
        foreach (var e in list)
        {
            try { if (!e.Allow(__0)) allow = false; }
            catch (Exception ex)
            {
                if (e.Reported) continue;
                e.Reported = true;
                try { CoreLog.Error($"{e.Owner}: input handler failed, input allowed (reported once): {ex}"); } catch { }
            }
        }
        return allow;
    }
}

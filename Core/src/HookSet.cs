using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace Restitutor.Core;

/// <summary>
/// One mod's hooks: finds exactly one target, resolves every handler before patching,
/// and refuses UI-building types before the game is ready. Behaviour is the same as the
/// per-mod Hook/Patch helpers it replaces; the mod keeps its own Harmony instance, so
/// removing one mod removes only its hooks.
/// </summary>
public sealed class HookSet
{
    // Types whose native static constructor must not run during Melon initialization.
    // Compared by name: touching the proxy type's static members would run it.
    private static readonly HashSet<string> readyOnly = new(StringComparer.Ordinal) { "Il2CppClient.Manager.UIManager" };

    private readonly HarmonyLib.Harmony harmony;
    private readonly Type handlers;

    public HookSet(HarmonyLib.Harmony harmony, Type handlers)
    {
        this.harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
        this.handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        GameReady.Touch();
    }

    /// <summary>Patches one method. Throws (and patches nothing) if the target is not unique,
    /// a handler is missing, no handler is given, or the target is UI-building and the game is not ready.</summary>
    public MethodInfo Hook(Type target, string name, string? prefix = null, string? postfix = null,
        string? finalizer = null, Type[]? args = null, bool declaredOnly = true)
    {
        if (prefix == null && postfix == null && finalizer == null)
            throw new ArgumentException($"{target?.FullName}.{name}: no handler given");
        if (!GameReady.IsReady && readyOnly.Contains(target!.FullName ?? ""))
            throw new InvalidOperationException($"{target.FullName}.{name} must be hooked after the game is ready; use GameReady.Run.");
        var original = MethodLookup.Unique(target!, name, args, declaredOnly);
        HarmonyMethod? H(string? n) => n == null ? null : new HarmonyMethod(MethodLookup.Handler(handlers, n));
        var pre = H(prefix); var post = H(postfix); var fin = H(finalizer);
        harmony.Patch(original, pre, post, finalizer: fin);
        return original;
    }

    /// <summary>All-or-nothing install for OnInitializeMelon: on any exception every hook of this
    /// mod's Harmony instance is removed and one error is logged. Returns true on success.</summary>
    public bool InstallAll(MelonLogger.Instance log, string what, Action body)
    {
        try { body(); return true; }
        catch (Exception ex)
        {
            try { harmony.UnpatchSelf(); } catch (Exception un) { log.Error("Unpatch after failed install: " + un); }
            log.Error(what + " failed; all hooks of this mod removed: " + ex);
            return false;
        }
    }
}

using System.Runtime.CompilerServices;
using HarmonyLib;
using MelonLoader;
using Restitutor.Core;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(Restitutor.Map.EntryPoint), "Restitutor fixes map", "0.2.4", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]

namespace Restitutor.Map;

public sealed class EntryPoint : MelonMod
{
    internal static MelonLogger.Instance Log = null!;
    private static HarmonyLib.Harmony? patches;
    // One HookSet per handler class (FogLifetime, SharedMap), all on this mod's Harmony instance.
    private static readonly Dictionary<Type, HookSet> sets = new();
    private static HookSet? main;
    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        patches = HarmonyInstance;
        // Everything that touches Restitutor.Core sits in Install(): without Restitutor.Core.dll in
        // UserLibs the failure surfaces here and only this mod stays disabled.
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { Log.Error("Restitutor.Core.dll is missing from UserLibs; map hooks not installed."); }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Install()
    {
        if (!CoreInfo.Require(Log, "0.2.0")) return;
        main = Set(typeof(EntryPoint));
        if (main.InstallAll(Log, "Installation", () =>
            {
                FogLifetime.Install();
                SharedMap.Install();
            }))
            Log.Msg("Map 0.2.4 hooks installed (Restitutor.Core " + CoreInfo.Version + ", shared input gate; red ports via the native UpdateInfo branch, no per-frame icon overwrite; [MAPDIAG] counters). No save files are edited by this mod.");
    }
    public override void OnUpdate() => SharedMap.Tick();
    private static HookSet Set(Type handler)
    {
        if (!sets.TryGetValue(handler, out var set)) sets[handler] = set = new HookSet(patches!, handler);
        return set;
    }
    // Declared members only, exact parameter types when given (as 0.2.3).
    internal static void Hook(Type target, string name, Type handler, string? before = null, string? after = null, Type[]? args = null, string? final = null)
        => Set(handler).Hook(target, name, prefix: before, postfix: after, finalizer: final, args: args);
    // 0.2.4: the route planner's close-key capture runs on the shared Core input gate. Registered through
    // the main set so a failed install (InstallAll) or RemoveAll drops it with the Harmony hooks.
    internal static void InputHandler(Func<InputAction.CallbackContext, bool> allow) => main!.Input("Map", allow);
}

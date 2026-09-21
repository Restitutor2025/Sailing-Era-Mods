using System.Reflection;
using HarmonyLib;
using MelonLoader;

[assembly: MelonInfo(typeof(Restitutor.Map.EntryPoint), "Restitutor fixes map", "0.2.3", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]

namespace Restitutor.Map;

public sealed class EntryPoint : MelonMod
{
    internal static MelonLogger.Instance Log = null!;
    private static HarmonyLib.Harmony? patches;
    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        patches = HarmonyInstance;
        try
        {
            FogLifetime.Install();
            SharedMap.Install();
            Log.Msg("Map 0.2.3 hooks installed (red ports via the native UpdateInfo branch, no per-frame icon overwrite; [MAPDIAG] counters). Runtime validation is required; no save files are edited by this mod.");
        }
        catch (Exception ex)
        {
            patches.UnpatchSelf();
            Log.Error("Installation failed; all map hooks removed: " + ex);
        }
    }
    public override void OnUpdate() => SharedMap.Tick();
    internal static void Hook(Type target, string name, Type handler, string? before = null, string? after = null, Type[]? args = null, string? final = null)
    {
        var methods = target.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == name && (args == null || m.GetParameters().Select(p => p.ParameterType).SequenceEqual(args))).ToArray();
        if (methods.Length != 1) throw new AmbiguousMatchException(target.FullName + "." + name + ": " + methods.Length);
        HarmonyMethod? H(string? n) => n == null ? null : new HarmonyMethod(handler.GetMethod(n, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!);
        patches!.Patch(methods[0], prefix: H(before), postfix: H(after), finalizer: H(final));
    }
}



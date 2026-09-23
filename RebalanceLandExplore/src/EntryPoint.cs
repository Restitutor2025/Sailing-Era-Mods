using System.Runtime.CompilerServices;
using MelonLoader;
using Restitutor.Core;
using Il2CppClient.UILogic.UILandExploreEvent;

[assembly: MelonInfo(typeof(Restitutor.RebalanceLandExplore.EntryPoint), "Restitutor Rebalance LandExplore", "0.1.0", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.RebalanceLandExplore;

// Land exploration (organised at a post station): the Fate Coin re-roll.
//  1. Its chance is capped at Rules.MaxRatio (50%).
//  2. The number shown becomes the real chance (the original rolls Random.Next(0,100) <= ratio,
//     which is one percentage point more often than it says).
// Two hooks, neither blocks or replaces an original call; see Rules.cs for the addresses this is based on.
public sealed class EntryPoint : MelonMod
{
    internal const string Version = "0.1.0";
    private static MelonLogger.Instance log = null!;
    private static bool enabled, cappedLogged, rollLogged;
    private static string? lastError;
    private HookSet? hookSet;

    public override void OnInitializeMelon()
    {
        log = LoggerInstance;
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { enabled = false; log.Error("Restitutor.Core.dll is missing from UserLibs; Rebalance LandExplore stays disabled."); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install()
    {
        if (!CoreInfo.Require(log, "0.1.0")) return;
        hookSet = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (!hookSet.InstallAll(log, "Rebalance LandExplore install", () =>
        {
            hookSet.Hook(typeof(UILandExploreEventView), "ListRoleRender", postfix: nameof(AfterListRoleRender));
            hookSet.Hook(typeof(UILandExploreEventCtrl), "ShowRatioResult", prefix: nameof(BeforeShowRatioResult));
            enabled = true;
        })) { enabled = false; return; }
        log.Msg($"Rebalance LandExplore {Version} loaded (Restitutor.Core {CoreInfo.Version}); 2 hooks. Fate coin chance <= {Rules.MaxRatio}%, shown number = real chance (was +1%p).");
    }

    // UILandExploreEventView.ListRoleRender postfix: the original has just raised Model.Ratio to this
    // navigator's chance when it is the best so far. Lower it back to the cap. The coin button is the last
    // item of the same list, so it reads the capped value; the roll reads it through OnClickCoin.
    private static void AfterListRoleRender(UILandExploreEventView __instance)
    {
        if (!enabled) return;
        try
        {
            var model = __instance?._model;
            if (model == null || !Rules.NeedsCap(model.Ratio)) return;
            int was = model.Ratio;
            model.Ratio = Rules.Cap(was);
            if (!cappedLogged) { cappedLogged = true; log.Msg($"coin chance capped: {was}% -> {model.Ratio}%."); }
        }
        catch (Exception ex) { Fail("ListRoleRender postfix", ex); }
    }

    // UILandExploreEventCtrl.ShowRatioResult(int ratio) prefix: the original rolls
    // Random.Next(0,100) <= ratio, i.e. ratio + 1 percent. Hand it one less so the shown number is the
    // truth. The original still runs with every other argument untouched; if the cap above already
    // applied, this only removes the off-by-one.
    private static void BeforeShowRatioResult(ref int __0)
    {
        if (!enabled) return;
        try
        {
            int shown = Rules.Cap(__0);
            int threshold = Rules.RollThreshold(__0);
            if (!rollLogged) { rollLogged = true; log.Msg($"coin roll: shown {shown}% -> threshold {threshold} (real chance {Rules.ChanceOf(threshold)}%, original would be {Rules.OriginalChance(__0)}%)."); }
            __0 = threshold;
        }
        catch (Exception ex) { Fail("ShowRatioResult prefix", ex); }
    }

    private static void Fail(string what, Exception ex)
    {
        var key = what + ":" + ex.Message;
        if (lastError == key) return;
        lastError = key; log.Error($"[{what}] {ex}");
    }

    public override void OnDeinitializeMelon()
    {
        enabled = false;
        hookSet?.RemoveAll();
    }
}

using System.Collections;
using System.Runtime.CompilerServices;
using MelonLoader;
using UnityEngine;
using Restitutor.Core;
using Il2CppClient.WorldLogic.Entity.Component.Boat;

[assembly: MelonInfo(typeof(Restitutor.RebalanceBoarding.EntryPoint), "Restitutor Rebalance Boarding", "0.3.0", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.RebalanceBoarding;

// Boarding (접현) reaches melee sooner.
//  0.1.0  melee threshold 100 -> 40: one native byte in CheckProgressFull (NativeThreshold.cs), no hook.
//  0.2.0  3-second grace: progress kept when the same ship is boarded again within 3 s (Grace.cs).
//         Prefix on OnEnemyExitShooting (records, never skips the original) + postfix on BeginShooting
//         (gives the progress back). Both run only on contact start/stop events, never per frame.
//  0.3.0  boarding progress gauge under the sailor gauge of every player ship (ProgressBarUI.cs).
//         Show/hide fade 0.5 s. Attached to the ship's own UICompBattleHp, so the game's existing
//         UpdateHPBar carries it - this mod still does no per-frame work.
// See docs/mods/rebalance-boarding/ and analysis/boarding-melee-duel/REPORT.md.
public sealed class EntryPoint : MelonMod
{
    internal const string Version = "0.3.0";
    private const int DetailLines = 40;
    private static MelonLogger.Instance log = null!;
    private static bool graceEnabled;
    private static bool gaugeEnabled;
    private static string? lastError;
    private static int recorded, restored, detailed;
    private static readonly Dictionary<IntPtr, Grace.Record> records = new();

    private NativeThreshold? threshold;
    private HookSet? hookSet;

    public override void OnInitializeMelon()
    {
        log = LoggerInstance;
        threshold = new NativeThreshold(log);
        threshold.Apply();   // independent of the hooks and of Restitutor.Core
        ProgressBarUI.Threshold = threshold.EffectiveThreshold;
        try { InstallHooks(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { graceEnabled = gaugeEnabled = false; log.Error("Restitutor.Core.dll is missing from UserLibs; 3-second grace and progress gauge disabled (threshold 40 still applies if active)."); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InstallHooks()
    {
        if (!CoreInfo.Require(log, "0.1.0")) return;
        hookSet = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (!hookSet.InstallAll(log, "Rebalance Boarding install", () =>
        {
            hookSet.Hook(typeof(BoatEntityBoardShoot), "OnEnemyExitShooting", prefix: nameof(BeforeEnemyExit));
            hookSet.Hook(typeof(BoatEntityBoardShoot), "BeginShooting", postfix: nameof(AfterBeginShooting));
            hookSet.Hook(typeof(BoatEntityBoardShoot), "BoardShootTickForAccumulateProgress", postfix: nameof(AfterProgressTick));
            hookSet.Hook(typeof(BoatEntityBoardShoot), "OnEnterMeleeBattle", postfix: nameof(AfterEnterMelee));
            hookSet.Hook(typeof(BoatEntityHealthBar), "ShowHPBar", postfix: nameof(AfterShowHPBar));
            hookSet.Hook(typeof(BoatEntityHealthBar), "HideHPBar", prefix: nameof(BeforeHideHPBar));
            graceEnabled = gaugeEnabled = true;
        })) { graceEnabled = gaugeEnabled = false; return; }
        log.Msg($"Rebalance Boarding {Version}: grace {Grace.GraceSeconds:0}s for re-boarding the same ship, progress gauge on player ships (fill = {ProgressBarUI.Threshold}); Restitutor.Core {CoreInfo.Version}; 6 hooks.");
    }

    // --- 0.2.0 grace -----------------------------------------------------------------------

    // Prefix, returns void: the original always runs. Called when a boarding target leaves the trigger
    // (or becomes invalid). If it is the last target, the original then locks shooting and StopShooting
    // zeroes ShootProgress - we remember the value first.
    private static void BeforeEnemyExit(BoatEntityBoardShoot __instance, BoatEntityBoardShoot __0)
    {
        if (!graceEnabled || __instance == null || __0 == null) return;
        try
        {
            var list = __instance.enemyList;
            if (list == null) return;
            int count = list.Count;
            bool only = count == 1 && list[0] != null && list[0].Pointer == __0.Pointer;
            int progress = __instance.ShootProgress;
            if (!Grace.ShouldRecord(count, only, __instance.isMeleeBattling, progress))
            { if (only) ProgressBarUI.HideFor(__instance); return; }
            float now = UnityEngine.Time.time;
            if (records.Count >= Grace.MaxRecords) Prune(now);
            var key = __instance.Pointer;
            records[key] = new Grace.Record(__0.Pointer, progress, now);
            recorded++;
            // The gauge stays as it is for the grace window; if nobody re-boards, this hides it.
            if (gaugeEnabled) MelonCoroutines.Start(HideIfGraceLapsed(key, __instance));
            Detail($"contact lost: keep progress {progress} for {Grace.GraceSeconds:0}s");
        }
        catch (Exception ex) { Fail("OnEnemyExitShooting prefix", ex); }
    }

    // One coroutine per contact break, not a poll: it wakes once when the grace window is over.
    private static IEnumerator HideIfGraceLapsed(IntPtr key, BoatEntityBoardShoot shoot)
    {
        yield return new WaitForSeconds(Grace.GraceSeconds + 0.1f);   // margin, so a restore at the edge wins
        if (!records.Remove(key)) yield break;   // already restored by AfterBeginShooting
        try { if (shoot != null) ProgressBarUI.HideFor(shoot); }
        catch (Exception ex) { Fail("grace lapse", ex); }
    }

    // Postfix: the original (new timers, first damage tick) has already run. StopShooting only zeroed
    // ShootProgress, so writing the saved value back resumes the same accumulation.
    private static void AfterBeginShooting(BoatEntityBoardShoot __instance)
    {
        if (!graceEnabled || __instance == null || records.Count == 0) return;
        try
        {
            if (!records.Remove(__instance.Pointer, out var r)) return;
            var target = __instance.targetEnemy;
            float now = UnityEngine.Time.time;
            if (target == null || !Grace.ShouldRestore(r, target.Pointer, now) || __instance.isMeleeBattling)
            { if (gaugeEnabled) ProgressBarUI.HideFor(__instance); return; }
            if (__instance.ShootProgress < r.Progress) __instance.ShootProgress = r.Progress;
            restored++;
            if (gaugeEnabled) ProgressBarUI.Update(__instance);
            Detail($"re-boarded same ship after {now - r.Time:0.00}s: progress {r.Progress} restored");
        }
        catch (Exception ex) { Fail("BeginShooting postfix", ex); }
    }

    // --- 0.3.0 gauge -----------------------------------------------------------------------

    // Postfix on the 2-second accumulation tick: the only place the numbers the gauge shows change.
    private static void AfterProgressTick(BoatEntityBoardShoot __instance)
    {
        if (!gaugeEnabled || __instance == null) return;
        try { ProgressBarUI.Update(__instance); }
        catch (Exception ex) { Fail("BoardShootTickForAccumulateProgress postfix", ex); }
    }

    private static void AfterEnterMelee(BoatEntityBoardShoot __instance)
    {
        if (!gaugeEnabled || __instance == null) return;
        try { ProgressBarUI.HideFor(__instance); }
        catch (Exception ex) { Fail("OnEnterMeleeBattle postfix", ex); }
    }

    private static void AfterShowHPBar(BoatEntityHealthBar __instance)
    {
        if (!gaugeEnabled || __instance == null) return;
        try { ProgressBarUI.Attach(__instance); }
        catch (Exception ex) { Fail("ShowHPBar postfix", ex); }
    }

    // Prefix, returns void: reads hpBarId while it is still valid; the original then releases the bar.
    private static void BeforeHideHPBar(BoatEntityHealthBar __instance)
    {
        if (!gaugeEnabled || __instance == null) return;
        try { ProgressBarUI.Detach(__instance); }
        catch (Exception ex) { Fail("HideHPBar prefix", ex); }
    }

    // --- shared ----------------------------------------------------------------------------

    private static void Prune(float now)
    {
        foreach (var key in records.Where(p => Grace.Expired(p.Value, now)).Select(p => p.Key).ToList()) records.Remove(key);
        if (records.Count >= Grace.MaxRecords) records.Clear();
    }

    private static void Detail(string text)
    {
        if (detailed >= DetailLines) return;
        detailed++;
        log.Msg(text + (detailed == DetailLines ? " (further events not logged; counters at exit)" : ""));
    }

    private static void Fail(string what, Exception ex)
    {
        var key = what + ":" + ex.Message;
        if (lastError == key) return;
        lastError = key; log.Error($"[{what}] {ex}");
    }

    public override void OnDeinitializeMelon()
    {
        graceEnabled = gaugeEnabled = false;
        log.Msg($"Rebalance Boarding: grace recorded {recorded}, restored {restored}; gauge attached {ProgressBarUI.attached}, detached {ProgressBarUI.detached} this run.");
        ProgressBarUI.Clear();
        hookSet?.RemoveAll();
        threshold?.Restore();
    }
}

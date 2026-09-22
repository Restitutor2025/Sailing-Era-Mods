using System.Runtime.CompilerServices;
using HarmonyLib;
using Il2CppClient.UILogic.UILaunch;
using Il2CppFairyGUI;
using MelonLoader;
using Restitutor.Core;
using UnityEngine.Rendering;

[assembly: MelonInfo(typeof(Restitutor.IntroSkipMod), "Restitutor fixes", "0.1.4", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]

namespace Restitutor;

public sealed class IntroSkipMod : MelonMod
{
    private static IntroSkipMod? instance;
    private static UILaunchView? launch;
    private static IntPtr health, disclaimer;
    private static UILaunchView? pending;
    private static bool disabled;
    private static bool noticeSkipped, contractCleanupLogged;
    private int splashFrames = 600;
    private bool splashLogged;

    public override void OnInitializeMelon()
    {
        instance = this;
        StopSplash();
        // Everything that touches Restitutor.Core sits in Install(): without Restitutor.Core.dll in
        // UserLibs the failure surfaces here and only this mod stays disabled (original UI kept).
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        {
            disabled = true;
            LoggerInstance.Error("Restitutor.Core.dll is missing from UserLibs; intro hooks disabled, original UI preserved.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install()
    {
        if (!CoreInfo.Require(LoggerInstance, "0.1.0")) { disabled = true; return; }
        var hooks = new HookSet(HarmonyInstance, typeof(IntroSkipMod));
        // declaredOnly: false keeps the 0.1.2 lookup (inherited members included); each name is unique.
        if (hooks.InstallAll(LoggerInstance, "Intro hooks (original UI preserved)", () =>
            {
                hooks.Hook(typeof(UILaunchView), "OnInit", postfix: nameof(RegisterLaunch), declaredOnly: false);
                hooks.Hook(typeof(UILaunchView), "HideHook", postfix: nameof(ReleaseLaunch), declaredOnly: false);
                hooks.Hook(typeof(UILaunchView), "UpdateHook", prefix: nameof(BeforeLaunchUpdate), declaredOnly: false);
                // 0.1.4: no hook on FairyGUI Transition._Play (every UI animation in the game passed through it).
                // The two notice transitions are checked with Transition.playing only while the launch view exists.
            }))
            LoggerInstance.Msg("Intro hooks ready (Restitutor.Core " + CoreInfo.Version + "): startup logos, health bulletin, disclaimer only.");
        else
            disabled = true;
    }

    private static void RegisterLaunch(UILaunchView __instance)
    {
        if (disabled) return;
        try
        {
            var h = __instance.AniTexHealthBulletin;
            var d = __instance.AniTexDisclaimer;
            if (h == null || d == null || h.Pointer == d.Pointer)
                throw new InvalidOperationException("Expected two distinct launch transitions.");
            launch = __instance;
            noticeSkipped = contractCleanupLogged = false;
            health = h.Pointer;
            disclaimer = d.Pointer;
            instance!.splashFrames = 0;
            instance.LoggerInstance.Msg("Launch UI registered; only its two notice transitions will be skipped.");
        }
        catch (Exception ex)
        {
            launch = null;
            health = disclaimer = IntPtr.Zero;
            instance!.LoggerInstance.Error($"Cannot register launch notices: {ex}");
        }
    }

    // Called every Update/LateUpdate while the launch view exists (a few seconds at startup): if one of the two
    // registered notice transitions has started, finish it before it is rendered. Other animations are not touched.
    private static void CheckNotices()
    {
        var view = launch;
        if (disabled || view == null) return;
        Transition? t = null;
        try
        {
            var h = view.AniTexHealthBulletin; var d = view.AniTexDisclaimer;
            if (h != null && h.Pointer == health && h.playing) t = h;
            else if (d != null && d.Pointer == disclaimer && d.playing) t = d;
        }
        catch (Exception ex) { disabled = true; instance!.LoggerInstance.Error($"Notice check failed; using original animation: {ex}"); return; }
        if (t != null) SkipNotice(t);
    }
    private static void SkipNotice(Transition __instance)
    {
        var ptr = __instance.Pointer;
        try
        {
            // Run the original timeline setup, then apply its end values BEFORE rendering.
            // Calling the callback alone leaves controller/visibility/alpha state unfinished.
            __instance.Stop(true, true);
            noticeSkipped = true;
            ClearContractResidue(launch!);
            pending = launch;
            instance!.LoggerInstance.Msg(ptr == health ? "Skipped health bulletin." : "Skipped disclaimer.");
        }
        catch (Exception ex)
        {
            instance!.LoggerInstance.Error($"Notice callback failed; using original animation: {ex}");
        }
    }

    private static void ReleaseLaunch(UILaunchView __instance)
    {
        if (launch?.Pointer != __instance.Pointer) return;
        pending = launch = null;
        noticeSkipped = false;
        health = disclaimer = IntPtr.Zero;
    }

    public override void OnUpdate()
    {
        if (splashFrames-- > 0) StopSplash();
        var view = pending;
        pending = null;
        if (view != null && !disabled)
        {
            try
            {
                // Defer until Deal has finished updating IsFirstEnterLaunch, avoiding reentry.
                view.SkipDeal();
                ClearContractResidue(view);
                LoggerInstance.Msg("Resumed vanilla launch flow after skipped notice.");
            }
            catch (Exception ex)
            {
                disabled = true;
                LoggerInstance.Error($"Failed to resume launch flow: {ex}");
            }
        }
        // After the pending step: a notice found now is resumed on the next frame, as with the old _Play postfix.
        CheckNotices();
    }

    private static void BeforeLaunchUpdate(UILaunchView __instance)
    {
        if(!disabled && launch?.Pointer == __instance.Pointer) CheckNotices();
        if(disabled || !noticeSkipped || launch?.Pointer != __instance.Pointer)return;
        try { ClearContractResidue(__instance); }
        catch(Exception ex)
        {
            noticeSkipped=false;
            instance!.LoggerInstance.Error($"Contract display cleanup suspended: {ex}");
        }
    }

    public override void OnLateUpdate()
    {
        // FairyGUI timelines can change visibility after the launch view's UpdateHook.
        // Keep cleanup limited to the launch instance whose notices we actually skipped.
        if(launch!=null)BeforeLaunchUpdate(launch); // also checks the two notices (0.1.4)
    }

    private static void ClearContractResidue(UILaunchView view)
    {
        var group=view.GroupDeal;
        if(group==null)throw new InvalidOperationException("Launch contract group missing.");
        if(!group.visible)return;
        // SkipDeal stops aniDeal and continues via the original gamma/menu callbacks,
        // but does not clear GroupDeal. Its visibility also blocks menu input in UpdateHook.
        // Do not invoke Confirm/Refuse, change acceptance flags, or replay menu transitions.
        group.visible=false;
        if(!contractCleanupLogged)
        {
            contractCleanupLogged=true;
            instance!.LoggerInstance.Msg("Cleared contract panel left visible after notice skip; no acceptance state changed.");
        }
    }

    private void StopSplash()
    {
        try
        {
            if (SplashScreen.isFinished) return;
            SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
            if (!splashLogged)
            {
                splashLogged = true;
                LoggerInstance.Msg("Stopped Unity startup splash immediately.");
            }
        }
        catch (Exception ex)
        {
            splashFrames = 0;
            LoggerInstance.Warning($"Unity splash API unavailable: {ex.Message}");
        }
    }
}

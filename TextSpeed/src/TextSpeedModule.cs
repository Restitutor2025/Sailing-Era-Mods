using System.Runtime.CompilerServices;
using HarmonyLib;
using Il2CppClient.UILogic.UIDialog;
using Il2CppClient.UILogic.UISystemSetting;
using Il2CppClient.Utils;
using MelonLoader;
using MelonLoader.Utils;
using Restitutor.Core;

namespace Restitutor.TextSpeed;

public static class TextSpeedModule
{
    public const string HarmonyId = "restitutor.module.textspeed.v1";
    internal static ModeStore Store = null!;
    internal static MelonLogger.Instance Log = null!;
    private static HarmonyLib.Harmony? harmony;
    private static readonly Dictionary<IntPtr, UILaunchOwner> owners = new();
    private sealed record UILaunchOwner(UIDialogView View);
    private sealed record Pending(TypingEffectObject Effect, IntPtr Callback);
    private static readonly Dictionary<IntPtr, Pending> pending = new();
    private static bool failed;

    public static void Initialize(MelonLogger.Instance logger)
    {
        if (harmony != null) return;
        Log = logger;
        if (HarmonyLib.Harmony.HasAnyPatches(HarmonyId))
        { Log.Error("Another text-speed module is already loaded; refusing duplicate hooks."); return; }
        // Stable across assembly rename/merge. No save-game or vanilla preference changes.
        Store = new ModeStore(Path.Combine(MelonEnvironment.UserDataDirectory,"Restitutor","textspeed.v1.json"));
        if (!Store.Ready) Log.Error("Text speed disabled: " + Store.Error);
        harmony = new HarmonyLib.Harmony(HarmonyId);
        // Everything that touches Restitutor.Core sits in Install(): without Restitutor.Core.dll in
        // UserLibs the failure surfaces here and only this mod stays disabled.
        try { Install(harmony); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { failed = true; Log.Error("Restitutor.Core.dll is missing from UserLibs; text speed stays disabled."); }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Install(HarmonyLib.Harmony h)
    {
        if (!CoreInfo.Require(Log, "0.1.0")) { failed = true; return; }
        var own = new HookSet(h, typeof(TextSpeedModule));
        var row = new HookSet(h, typeof(OptionsRow));
        if (!own.InstallAll(Log, "Install", () =>
            {
                row.Hook(typeof(UISettingView), "OnVideoSettingItemRender", prefix: nameof(OptionsRow.BeforeRender), postfix: nameof(OptionsRow.AfterRender));
                own.Hook(typeof(UIDialogView), "OnInit", postfix: nameof(RegisterDialog));
                // ShowHook replaces all five effects before it calls StartEffect.
                own.Hook(typeof(UIDialogView), "StartEffect", prefix: nameof(RegisterDialog));
                own.Hook(typeof(UIDialogView), "HideHook", postfix: nameof(ReleaseDialog));
                own.Hook(typeof(TypingEffectObject), "Start", postfix: nameof(AfterStart));
                // 0.1.4: Instant mode, CTRL not held: next line 3 s after the line is complete (auto on or off).
                own.Hook(typeof(UIDialogView), "EndTyping", prefix: nameof(BeforeEndTyping), postfix: nameof(AfterEndTyping));
                // Any native continue (click, key, CTRL press with auto off) ends the wait: no double advance.
                own.Hook(typeof(UIDialogCtrl), "OnAction_ContinueTalk", prefix: nameof(BeforeContinueTalk));
            })) { failed = true; return; }
        Log.Msg($"Ready (0.1.6, Restitutor.Core {CoreInfo.Version}). Saved mode={Store.Current}; settings={Path.Combine(MelonEnvironment.UserDataDirectory,"Restitutor","textspeed.v1.json")}");
    }
    private static TypingEffectObject?[] Effects(UIDialogView v) => new[] {v.TypingEffectDialogContent,v.TypingEffectDialogAside,v.TypingEffectBubbleLeft,v.TypingEffectBubbleMiddle,v.TypingEffectBubbleRight};
    private static void RegisterDialog(UIDialogView __instance)
    {
        try
        {
            var effects=Effects(__instance);
            var current=effects.Where(e=>e!=null).Select(e=>e!.Pointer).ToHashSet();
            foreach(var p in owners.Where(x=>x.Value.View.Pointer==__instance.Pointer && !current.Contains(x.Key)).Select(x=>x.Key).ToArray())
            {owners.Remove(p);pending.Remove(p);}
            foreach(var e in effects)if(e!=null)owners[e.Pointer]=new(__instance);
        }
        catch(Exception ex){Log.Error("Cannot identify dialogue typing effects: "+ex.Message);}
    }
    private static void ReleaseDialog(UIDialogView __instance)
    {
        foreach(var p in owners.Where(x=>x.Value.View.Pointer==__instance.Pointer).Select(x=>x.Key).ToArray())
        {owners.Remove(p);pending.Remove(p);}
        if(autoWait!=null && autoWait.View.Pointer==__instance.Pointer) autoWait=null;
    }

    // ---- 0.1.4 auto-talk hold (user: Instant mode, CTRL not held -> next line 3 s after full output,
    // whether native auto talk is on or off).
    // Native UIDialogView.EndTyping (0x1349E40): IsTyping=false; only when model._autoTalk it sets
    // model.clickWait=true and adds a Trigger (500 ms, or 200/x ms while IsSpeed) whose callback
    // <EndTyping>b__56_0 (0x134F490) calls UIDialogCtrl.OnAction_ContinueTalk only if clickWait is
    // still true. OnAction_ContinueTalk clears clickWait (0x1343CC5). CTRL fast-forward
    // (OnAction_FastForwardTalk 0x1344800) sets ctrl.autoAndSpeed=true until release (FreeUp
    // 0x1344A40). Here: clear clickWait (the native trigger then does nothing) and continue
    // through the same native OnAction_ContinueTalk 3 s later, or at once if CTRL is pressed.
    // With auto off native arms nothing; OnAction_ContinueTalk is the player's own continue input
    // and keeps all its guards (_canClick, talk types 8/9/13/14, canClick, options, effect).
    // EndTyping does nothing unless model.IsTyping was true on entry (0x1349ECC), so the prefix
    // records that and only a real line completion starts the wait.
    internal const float AutoHoldSeconds = 3f;
    private sealed record AutoWait(UIDialogView View, int TalkTid, float Due);
    private static AutoWait? autoWait;
    private static int autoLogs;
    private static void AutoLog(string text){ if(autoLogs>=60) return; autoLogs++; Log.Msg("[AutoHold] "+text); }

    private static void BeforeEndTyping(UIDialogView __instance, out bool __state)
    {
        __state=false;
        try { __state=__instance._model?.IsTyping==true; } catch { }
    }
    private static void AfterEndTyping(UIDialogView __instance, bool __state)
    {
        try
        {
            if(!__state || failed || !Store.Ready || Store.Current!=TextMode.Instant) return;
            var model=__instance._model;
            if(model==null) return;
            var ctrl=UIDialogCtrl.Instance;
            if(ctrl==null || ctrl.autoAndSpeed) return;                        // CTRL fast-forward: keep native timing
            if(model.clickWait) model.clickWait=false;                        // auto on: native 500 ms trigger becomes a no-op
            autoWait=new(__instance, model.TalkTid, UnityEngine.Time.unscaledTime+AutoHoldSeconds);
            AutoLog($"line {model.TalkTid} complete (auto={model._autoTalk}); next line in {AutoHoldSeconds:0} s.");
        }
        catch(Exception ex){ autoWait=null; Log.Error("Auto-talk hold skipped: "+ex.Message); }
    }

    private static void BeforeContinueTalk()
    {
        if(autoWait==null) return;
        autoWait=null; AutoLog("wait cancelled: the game continued the dialogue itself.");
    }

    private static void TickAutoWait()
    {
        var w=autoWait;
        if(w==null) return;
        try
        {
            var model=w.View._model;
            // Anything that moved the dialogue on hands control back to the game.
            if(model==null || model.IsTyping || model.TalkTid!=w.TalkTid)
            { autoWait=null; AutoLog("wait cancelled (line changed or typing again)."); return; }
            var ctrl=UIDialogCtrl.Instance;
            if(ctrl==null){ autoWait=null; return; }
            bool ctrlHeld=ctrl.autoAndSpeed;
            if(!ctrlHeld && UnityEngine.Time.unscaledTime<w.Due) return;
            autoWait=null;
            // CTRL with auto off: native FastForwardTalk already called OnAction_ContinueTalk (wait cleared
            // above). Reaching here with CTRL means auto was on, where native only raises the speed.
            AutoLog(ctrlHeld ? "CTRL fast-forward during wait: continue now." : "3 s elapsed: continue.");
            ctrl.OnAction_ContinueTalk();
        }
        catch(Exception ex){ autoWait=null; Log.Error("Auto-talk hold stopped: "+ex.Message); }
    }
    private static void AfterStart(TypingEffectObject __instance)
    {
        // Sample ONCE for this line. Redraws, resets, and changing the option mid-line
        // cannot change the decision for a line that is already printing.
        if(failed || !Store.Ready || Store.Current!=TextMode.Instant || !owners.ContainsKey(__instance.Pointer))return;
        if(!__instance.GetIsStart())return;
        pending[__instance.Pointer]=new(__instance,__instance._callBack?.Pointer??IntPtr.Zero);
    }
    public static void LateUpdate()
    {
        OptionsRow.LateUpdate();
        if(!failed) TickAutoWait();
        if(failed || pending.Count==0)return;
        var work=pending.Values.ToArray();pending.Clear();
        foreach(var p in work)
        {
            try
            {
                if(!owners.ContainsKey(p.Effect.Pointer)||!p.Effect.GetIsStart())continue;
                if((p.Effect._callBack?.Pointer??IntPtr.Zero)!=p.Callback)continue;
                // Native Cancel removes the timer and mesh mask, invokes the original
                // completion callback, and clears it. Do not invoke EndTyping/NextTalk again.
                p.Effect.Cancel();
                Log.Msg("Instant dialogue completed via native Cancel; next-line input left to game.");
            }
            catch(Exception ex){failed=true;Log.Error("Text-speed processing suspended: "+ex);break;}
        }
    }
    public static void Shutdown()
    {
        harmony?.UnpatchSelf(); harmony=null; pending.Clear(); owners.Clear(); autoWait=null;
    }
}

using System.Runtime.CompilerServices;
using Il2CppClient.WorldLogic.Entity.Component.PortNote;
using Il2CppClient.WorldLogic.Map.GOComponent;
using HarmonyLib;
using MelonLoader;
using Restitutor.Core;
using UnityEngine;

[assembly: MelonInfo(typeof(Restitutor.InstantEntrance.EntryPoint), "Restitutor fixes Instant Entrance", "0.1.1", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]

namespace Restitutor.InstantEntrance;

public sealed class EntryPoint : MelonMod
{
    // Only the synchronous native port-note entry call may alter Animator.Play.
    // Save/restore supports nesting and exceptions without leaking to other UI.
    [ThreadStatic] private static string? entryAnimation;
    private static MelonLogger.Instance log = null!;
    private static bool reportedFailure;

    public override void OnInitializeMelon()
    {
        log = LoggerInstance;
        // Everything that touches Restitutor.Core sits in Install(): if Restitutor.Core.dll is
        // missing from UserLibs, the failure surfaces here and only this mod stays disabled.
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { log.Error("Restitutor.Core.dll is missing from UserLibs; Instant Entrance stays disabled."); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install()
    {
        if (!CoreInfo.Require(log, "0.1.0")) return;
        var hooks = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (hooks.InstallAll(log, "Instant Entrance install", () =>
            {
                hooks.Hook(typeof(MapHarbourEnterCtrl), "Start", postfix: nameof(ReadyAfterStart), args: Type.EmptyTypes);
                hooks.Hook(typeof(EntityPortNoteAnimator), "PlayEnterAnimation", prefix: nameof(BeginEntry), finalizer: nameof(EndEntry), args: Type.EmptyTypes);
                hooks.Hook(typeof(Animator), "Play", prefix: nameof(CompleteEntryAnimation), args: new[] { typeof(string), typeof(int), typeof(float) });
            }))
            log.Msg("0.1.1 (Restitutor.Core " + CoreInfo.Version + "): port-note entry animation and initial 2.5-second harbour gate patched. Native entry checks retained.");
    }

    private static void ReadyAfterStart(MapHarbourEnterCtrl __instance)
    {
        try
        {
            // Original Start initializes identifiers, range checks and listeners first.
            // Its coroutine later writes the same value; no coroutine is cancelled.
            __instance._canEnterHarbour = true;
        }
        catch (Exception ex)
        {
            if (reportedFailure) return;
            reportedFailure = true;
            log.Error("Could not remove initial harbour delay; original timer retained: " + ex);
        }
    }

    private static void BeginEntry(EntityPortNoteAnimator __instance, out string? __state)
    {
        __state = entryAnimation;
        entryAnimation = __instance.ENTER_ANIM_NAME;
    }

    private static void CompleteEntryAnimation(string __0, ref float __2)
    {
        if (!string.IsNullOrEmpty(entryAnimation) && string.Equals(entryAnimation, __0, StringComparison.Ordinal))
            __2 = 1f;
    }

    private static Exception? EndEntry(Exception? __exception, string? __state)
    {
        entryAnimation = __state;
        return __exception;
    }
}


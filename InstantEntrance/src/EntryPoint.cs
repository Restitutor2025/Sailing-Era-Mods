using System.Reflection;
using Il2CppClient.WorldLogic.Entity.Component.PortNote;
using Il2CppClient.WorldLogic.Map.GOComponent;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Restitutor.InstantEntrance.EntryPoint), "Restitutor fixes Instant Entrance", "0.1.0", "Restitutor")]
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
        try
        {
            HarmonyInstance.Patch(Required(typeof(MapHarbourEnterCtrl), "Start", Type.EmptyTypes),
                postfix: Handler(nameof(ReadyAfterStart)));
            HarmonyInstance.Patch(Required(typeof(EntityPortNoteAnimator), "PlayEnterAnimation", Type.EmptyTypes),
                prefix: Handler(nameof(BeginEntry)), finalizer: Handler(nameof(EndEntry)));
            HarmonyInstance.Patch(Required(typeof(Animator), "Play", new[] { typeof(string), typeof(int), typeof(float) }),
                prefix: Handler(nameof(CompleteEntryAnimation)));
            log.Msg("0.1.0: port-note entry animation and initial 2.5-second harbour gate patched. Native entry checks retained; runtime validation pending.");
        }
        catch (Exception ex)
        {
            HarmonyInstance.UnpatchSelf();
            log.Error("Installation failed; Instant Entrance hooks removed: " + ex);
        }
    }

    private static MethodInfo Required(Type type, string name, Type[] args) =>
        type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            null, args, null) ?? throw new MissingMethodException(type.FullName, name);
    private static HarmonyMethod Handler(string name) => new(typeof(EntryPoint).GetMethod(name,
        BindingFlags.NonPublic | BindingFlags.Static)!);

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


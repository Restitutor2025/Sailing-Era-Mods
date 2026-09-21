using System.Runtime.CompilerServices;
using HarmonyLib;
using Il2CppCnControls;
using MelonLoader;
using Restitutor.Core;
using Il2CppClient.Const;
using Il2CppClient.Manager;
using Il2CppClient.UILogic.UISailing;
using Il2CppClient.WorldLogic.InputAgent;
using Il2CppClient.WorldLogic.Scenes;
using Il2CppClient.WorldLogic.Scenes.SceneState;
using Il2CppCore.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using SceneManager = Il2CppCore.SceneSystem.SceneManager;

[assembly: MelonInfo(typeof(Restitutor.CTRLInstant.EntryPoint), "Restitutor BugFixes CTRL Instant", "0.1.7", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]

namespace Restitutor.CTRLInstant;

public sealed class EntryPoint : MelonMod
{
    // The original fleet interaction window (opened by clicking a fleet) keeps the hold alive.
    private const string NpcWindowType = "Client.UILogic.UINpcInteractive.UINpcInteractiveCtrl";
    private static readonly PauseLease lease = new();
    private static UISailingCtrl? owner;
    private static InputAction? action;
    private static nint oceanPointer;
    private static nint acceptedFocus;
    private static int selectionToken;
    private static float nextSelection;
    private static bool enabled, cleaning;
    private static nint npcCachePtr;
    private static bool npcCacheValue;
    private static MelonLogger.Instance log = null!;
    private static string lastError = "";

    public override void OnInitializeMelon()
    {
        log = LoggerInstance;
        // Everything that touches Restitutor.Core sits in Install(): without Restitutor.Core.dll in
        // UserLibs the failure surfaces here and only this mod stays disabled.
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { log.Error("Restitutor.Core.dll is missing from UserLibs; CTRL Instant stays disabled."); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install()
    {
        if (!CoreInfo.Require(log, "0.1.0")) return;
        var hooks = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (!hooks.InstallAll(log, "CTRL Instant install", () =>
            {
                hooks.Hook(typeof(UISailInputAgent), "OnFeatureInputActionStart", postfix: nameof(Started));
                hooks.Hook(typeof(UISailInputAgent), "OnFeatureInputActionPerform", prefix: nameof(Performed));
                hooks.Hook(typeof(UISailInputAgent), "OnFeatureInputActionEnd", finalizer: nameof(Ended));
                hooks.Hook(typeof(UISailingCtrl), "CloseHook", prefix: nameof(Closing));
                hooks.Hook(typeof(UISailingCtrl), "DisposeHook", prefix: nameof(Closing));
                hooks.Hook(typeof(OceanSceneSailingState), "Exit", prefix: nameof(Leaving));
                hooks.Hook(typeof(GameManager), "ForceReset", postfix: nameof(Reset));
            })) return;
        enabled = true;
        log.Msg("0.1.7 (Restitutor.Core " + CoreInfo.Version + "): CTRL pauses sailing instantly; the fleet interaction window keeps the pause.");
    }

    private static OceanScene? Sailing()
    {
        var scenes = SceneManager.Instance;
        if (scenes == null || !scenes.IsSceneEntered || scenes.IsInLoadingOrStarting || !scenes.IsInOceanScene) return null;
        var ocean = scenes.CurScene()?.TryCast<OceanScene>();
        if (ocean == null || ocean.IsInBattle || ocean.SceneStateType != SceneStateType.Sailing ||
            ocean._focusTeam?.isPlayer != true || ocean._focusTeam.State != EBoatTeamState.Free) return null;
        return ocean;
    }

    private static void Started(UISailInputAgent __instance, FeatureInputEventType __0, InputAction.CallbackContext __1)
    {
        if (!enabled || __0 != FeatureInputEventType.CheckTarget || lease.Active || cleaning) return;
        try
        {
            // The sailing HUD is normally unfocused. The ocean scene owns normal sailing input.
            var ctrl = __instance._uiCtrl?.TryCast<UISailingCtrl>();
            var ocean = Sailing();
            if (!Application.isFocused || ocean == null || ctrl?.View == null || ctrl.Model == null ||
                !__instance.IsValid || !ctrl.View.IsInputActive ||
                ocean.SceneInputAgent?.IsOnFocused != true || GameManager.IsGamePaused) return;
            var input = __1.action;
            if (input == null || !input.enabled) return;
            var list = GameManager.pauseList;
            if (list == null) return;
            owner = ctrl;
            action = input;
            oceanPointer = ocean.Pointer;
            selectionToken = 0;
            int token = GameManager.PauseGame();
            lease.Take(token, list.Pointer);
            acceptedFocus = CurrentFocus();
            TrySelect();
        }
        catch (Exception ex) { Error(ex); Release(); }
    }

    // Keep swallowing this hold after UI handoff until release: no delayed second entry.
    private static bool Performed(FeatureInputEventType __0, InputAction.CallbackContext __1) =>
        __0 != FeatureInputEventType.CheckTarget || action == null || __1.action?.Pointer != action.Pointer;

    private static void Ended(FeatureInputEventType __0)
    {
        if (__0 != FeatureInputEventType.CheckTarget) return;
        Release();
        action = null;
    }

    private static void TrySelect()
    {
        if (owner?.Model == null || owner.Model.checkNPCShipStatus) return;
        nextSelection = Time.unscaledTime + 0.15f;
        try { owner.OnAction_CheckTarget_Long(); }
        finally
        {
            // Track only the original selection opened by our call; never claim pre-existing UI pauses.
            if (owner?.Model?.checkNPCShipStatus == true) selectionToken = owner.pauseIdOfCheckingNpcStatus;
            // Original selection may change UI focus itself. This synchronous handoff belongs to us.
            // The NPC window is accepted separately and must never replace the sailing focus boundary.
            if (!NpcWindowFocused()) acceptedFocus = CurrentFocus();
        }
    }

    public override void OnUpdate()
    {
        if (!enabled || action == null) return;
        try
        {
            if (!Application.isFocused || !action.enabled || !action.IsPressed())
            {
                Release(); action = null; return;
            }
            if (!lease.Active) return;
            var ocean = Sailing();
            bool npcWindow = NpcWindowFocused();
            if (ocean == null || ocean.Pointer != oceanPointer || owner?.View == null ||
                (CurrentFocus() != acceptedFocus && !npcWindow) || !owner.View.IsInputActive)
            {
                Release(); return;
            }
            var list = GameManager.pauseList;
            if (list == null || list.Pointer != lease.List || !list.Contains(lease.Token))
            {
                Forget(); return;
            }
            if (npcWindow)
            {
                // Original contract: UINpcInteractiveCtrl.CloseHook closes the selection pause when the window closes.
                // Hand our selection over instead of closing it, so the window stays paused after CTRL is released.
                selectionToken = 0;
                return; // No reselection while the window is open; our own pause stays until CTRL is released.
            }
            // A failed first selection is not latched for the rest of the hold.
            if (Time.unscaledTime >= nextSelection) TrySelect();
        }
        catch (Exception ex) { Error(ex); Release(); action = null; }
    }

    private static void Closing(UISailingCtrl __instance)
    {
        if (owner?.Pointer == __instance.Pointer) Release();
    }
    private static void Leaving() => Release();
    // Native ForceReset already clears the pause list. Do not release stale IDs into a new session.
    private static void Reset() { Forget(); action = null; }
    private static void Forget()
    {
        lease.Forget(); owner = null; selectionToken = 0; oceanPointer = 0; acceptedFocus = 0;
    }
    private static void Release()
    {
        if (cleaning) return;
        cleaning = true;
        try
        {
            var list = GameManager.pauseList;
            if (lease.Active && list != null && list.Pointer == lease.List && selectionToken > 0 &&
                owner?.Model?.checkNPCShipStatus == true && owner.pauseIdOfCheckingNpcStatus == selectionToken)
            {
                try { owner.CloseCheckNpcStatus(); }
                catch (Exception ex)
                {
                    Error(ex);
                    // Even a destroyed view must not retain the selection token issued by our call.
                    if (list.Contains(selectionToken)) GameManager.ContinueGame(selectionToken);
                }
            }
            lease.Release(list?.Pointer ?? IntPtr.Zero, t => list!.Contains(t), t => GameManager.ContinueGame(t));
        }
        catch (Exception ex) { Error(ex); }
        finally { Forget(); cleaning = false; }
    }

    private static bool NpcWindowFocused()
    {
        var focus = UIManager.Instance?.CurrentFocusViewCtrl;
        if (focus == null) return false;
        nint p = focus.Pointer;
        if (p != npcCachePtr)
        {
            npcCachePtr = p;
            try { npcCacheValue = focus.TryCast<Il2CppSystem.Object>()?.GetIl2CppType()?.FullName == NpcWindowType; }
            catch { npcCacheValue = false; }
        }
        return npcCacheValue;
    }

    private static nint CurrentFocus() => UIManager.Instance?.CurrentFocusViewCtrl?.Pointer ?? IntPtr.Zero;

    private static void Error(Exception ex)
    {
        if (lastError == ex.Message) return;
        lastError = ex.Message;
        log.Error("CTRL Instant: " + ex);
    }

    public override void OnDeinitializeMelon()
    {
        enabled = false; Release(); action = null; HarmonyInstance.UnpatchSelf();
    }
}

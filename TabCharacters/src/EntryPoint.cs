using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using Restitutor.Core;
using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(Restitutor.TabCharacters.EntryPoint), "Restitutor Additional Tab Characters", "0.6.12", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint : MelonMod
{
    private const string Baseline = "50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA";
    private static EntryPoint? host;
    private static PageState? state;
    private static bool enabled, recoveryPending;
    private static bool cleanupEnabled = true;
    private static int mainThread;
    private static bool Allowed => enabled && Environment.CurrentManagedThreadId == mainThread;

    public override void OnInitializeMelon()
    {
        host = this;
        mainThread = Environment.CurrentManagedThreadId;
        // Everything that touches Restitutor.Core sits in Install(): without Restitutor.Core.dll in
        // UserLibs the failure surfaces here and only this mod stays disabled.
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { enabled = false; LoggerInstance.Error("Restitutor.Core.dll is missing from UserLibs; Characters stays disabled."); }
    }

    private HookSet? hooks;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install()
    {
        if (!CoreInfo.Require(LoggerInstance, "0.2.0")) { enabled = false; return; }
        hooks = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (!hooks.InstallAll(LoggerInstance, "Characters initialization", () =>
        {
            using var stream = File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory, "GameAssembly.dll"));
            using var hash = SHA256.Create();
            if (Convert.ToHexString(hash.ComputeHash(stream)) != Baseline)
                throw new InvalidOperationException("GameAssembly baseline differs; pagination was not installed.");

            Patch(typeof(UICharacterView), "Refresh", Type.EmptyTypes, nameof(BeforeRefresh), nameof(AfterRefresh), nameof(RefreshFault));
            Patch(typeof(UICharacterView), "HideHook", Type.EmptyTypes, nameof(BeforeHide));
            Patch(typeof(UICharacterCtrl), "ShowSheet", new[] { typeof(ESheetType) }, nameof(BeforeSheet));
            Patch(typeof(UICharacterCtrl), "OnClickBtnLeft", Type.EmptyTypes, nameof(Left));
            Patch(typeof(UICharacterCtrl), "OnClickBtnRight", Type.EmptyTypes, nameof(Right));
            Patch(typeof(GList), "set_numItems", new[] { typeof(int) }, nameof(LimitCount));
            Patch(typeof(GList), "ScrollToView", new[] { typeof(int), typeof(bool), typeof(bool) }, nameof(AllowScroll));
            Patch(typeof(GList), "HandleArrowKey", new[] { typeof(int) }, nameof(AllowArrow));
            Patch(typeof(GList), "Dispose", Type.EmptyTypes, nameof(BeforeListDispose));
            InstallBooks();
            enabled = true;
        })) { enabled = false; return; }
        LoggerInstance.Msg("Characters 0.6.12 (Restitutor.Core " + CoreInfo.Version + ", shared input gate; behaviour as 0.6.11: idle equip-confirm key read removed; right-click close swallows only its own Action_B until release; language book panel; [LangTrace] kept): equipment slots open the native equipment list/filter/details beside them; read-only native skill info with adjacent book/point panel; skill tab retired; centered portrait pages.");
    }

    // Exact parameter types, declared members only (as 0.6.11).
    private void Patch(Type type, string name, Type[] args, string? prefix, string? postfix = null, string? finalizer = null)
        => hooks!.Hook(type, name, prefix: prefix, postfix: postfix, finalizer: finalizer, args: args);
    // 0.6.12: the book/equipment input capture runs on the shared Core input gate.
    private void InputHandler(Func<InputAction.CallbackContext, bool> allow) => hooks!.Input("Tab Characters", allow);
    // Removes this mod's hooks and its input handler (HarmonyInstance.UnpatchSelf() alone would leave the handler).
    private void RemoveHooks() { if (hooks != null) hooks.RemoveAll(); else HarmonyInstance.UnpatchSelf(); }
    // 0.6.0 equipment panel hooks (EquipPanel*.cs); optional so the pagination suite stays independent.
    static partial void EquipCloseForSheet(string reason);
    static partial void EquipRefreshProbe(UICharacterView view);
    private static bool IsCharacter(UICharacterView view) => view._model != null && view._model.SheetType == ESheetType.Character;
    private static bool Owns(GList list) => Allowed && state != null && state.List.Pointer == list.Pointer;

    private static PageState? Ensure(UICharacterView view)
    {
        if (!Allowed || recoveryPending) return null;
        var content = view._UIContent_k__BackingField;
        if (!IsCharacter(view) || content == null || content.isDisposed)
        {
            Release(false);
            return null;
        }
        var list = content.listRole;
        var model = view._model;
        if (list == null || list.isDisposed || model.ListRole == null) return null;
        if (state != null && (state.View.Pointer != view.Pointer || state.List.Pointer != list.Pointer || state.Model.Pointer != model.Pointer))
            Release(false);
        if (state == null)
        {
            // The known native list is non-virtual. Do not apply a different index contract to a virtual list.
            if (list.isVirtual || list.itemRenderer == null)
                throw new InvalidOperationException("Unexpected character list configuration (virtual or missing renderer).");
            var fresh = new PageState(view, model, list);
            state = fresh; // Own before any UI writes; failure recovery can restore partially applied settings.
            fresh.Attach();
            host?.LoggerInstance.Msg("Characters pagination attached (roster=" + model.ListRole.Count + ").");
        }
        state.Page.Reconcile(model.ListRole.Count);
        return state;
    }

    private static void BeforeRefresh(UICharacterView __instance)
    {
        if (!Allowed) return;
        EquipRefreshProbe(__instance);
        try { Ensure(__instance); }
        catch (Exception ex) { DeferRecovery(ex); }
    }
    private static void AfterRefresh(UICharacterView __instance)
    {
        if (!Allowed || recoveryPending || state == null || state.View.Pointer != __instance.Pointer) return;
        try { state.Pin(); RefreshBookBindings(__instance); }
        catch (Exception ex) { DeferRecovery(ex); }
    }
    private static Exception? RefreshFault(Exception? __exception)
    {
        if (__exception != null && state != null) DeferRecovery(__exception);
        return __exception; // Never suppress the game's exception.
    }
    private static void BeforeSheet(UICharacterCtrl __instance, ref ESheetType __0)
    {
        EquipCloseForSheet("sheet change");
        CloseBooks();
        var requested = __0;
        // 0.5.9 safety net: the skill tab is retired (SkillTabHide.cs). Any request that
        // still reaches the skill sheet opens the Characters sheet instead.
        if (Allowed && (requested == ESheetType.Skill || requested == ESheetType.Equip))
        {
            __0 = ESheetType.Character;
            host?.LoggerInstance.Warning($"{requested} sheet request redirected to the Characters sheet (tab retired).");
        }
        if (!Allowed || requested == ESheetType.Character) return;
        try { Release(false); }
        catch (Exception ex) { DeferRecovery(ex); }
    }
    private static void BeforeHide(UICharacterView __instance)
    {
        EquipCloseForSheet("view hidden");
        CloseBooks(); ClearBookBindings();
        if (!Allowed || state == null || state.View.Pointer != __instance.Pointer) return;
        try
        {
            var closing = state;
            Release(false);
            closing.ClearClosedDetails();
        }
        catch (Exception ex) { DeferRecovery(ex); }
    }
    private static void BeforeListDispose(GList __instance)
    {
        if (!Owns(__instance)) return;
        // Do not rebuild a list which is being disposed. Native disposal releases its children.
        try { Release(false, true); }
        catch (Exception ex) { DeferRecovery(ex); }
    }
    private static void LimitCount(GList __instance, ref int __0)
    {
        if (!Owns(__instance)) return;
        // Only this exact list instance is capped. The full roster/model is never sliced or replaced.
        __0 = state!.Page.VisibleCount;
    }
    private static bool AllowScroll(GList __instance) => !Owns(__instance);
    private static bool AllowArrow(GList __instance, ref int __result)
    {
        if (!Owns(__instance)) return true;
        __result = -1;
        return false;
    }
    private static bool Left(UICharacterCtrl __instance) => TurnPage(__instance, -1);
    private static bool Right(UICharacterCtrl __instance) => TurnPage(__instance, 1);
    private static bool TurnPage(UICharacterCtrl ctrl, int direction)
    {
        if (BooksOpen) CloseBooks();
        EquipCloseForSheet("page turn");
        if (!Allowed || ctrl._Model_k__BackingField == null || ctrl._Model_k__BackingField.SheetType != ESheetType.Character) return true;
        // During recovery, consume this request rather than unexpectedly selecting another character.
        if (recoveryPending) return false;
        try
        {
            var model = ctrl._Model_k__BackingField;
            var view = ctrl._View_k__BackingField;
            if (view == null || !view.IsInputActive || model.IsSelectEquipFilter || model.IsSelectSkillFilter) return false;
            var active = Ensure(view);
            if (active != null && active.Page.Move(direction))
            {
                active.List.numItems = active.Page.VisibleCount;
                active.Pin();
            }
        }
        catch (Exception ex) { DeferRecovery(ex); }
        return false;
    }

    private static void DeferRecovery(Exception ex)
    {
        if (!recoveryPending) host?.LoggerInstance.Error("Characters pagination error; restoring original UI on next update: " + ex);
        recoveryPending = true;
    }
    private static void CleanupFailed(Exception ex)
    {
        cleanupEnabled = false;
        host?.LoggerInstance.Warning("Characters resource cleanup disabled; pagination retained: " + ex);
    }
    private static void Release(bool redraw, bool disposing = false)
    {
        EquipCloseForSheet("release");
        CloseBooks(); ClearBookBindings();
        var old = state;
        state = null; // Lift instance interception before restoring the original renderer/count.
        old?.Restore(redraw, disposing);
    }
    public override void OnUpdate()
    {
        if (!Allowed) return;
        try
        {
            TickBooks();
            if (recoveryPending)
            {
                enabled = false;
                Release(true);
                RemoveHooks();
                LoggerInstance.Warning("Characters pagination disabled for this session after recovery.");
            }
            else if (state != null && (state.List.isDisposed || state.View._UIContent_k__BackingField == null))
                Release(false, true);
        }
        catch (Exception ex)
        {
            enabled = false;
            RemoveHooks();
            state = null;
            LoggerInstance.Error("Characters recovery failed: " + ex);
        }
    }
    public override void OnDeinitializeMelon()
    {
        enabled = false;
        try { Release(true); }
        finally { RemoveHooks(); host = null; }
    }

    private sealed class PageState
    {
        internal readonly UICharacterView View;
        internal readonly UICharacterModel Model;
        internal readonly GList List;
        internal readonly PageWindow Page = new();
        private readonly ListItemRenderer originalRenderer, pageRenderer;
        private readonly ListLayoutType layout;
        private readonly AlignType alignment;
        private readonly ListSelectionMode selection;
        private readonly bool scrollOnClick, touch, wheel;
        private readonly ScrollPane? pane;
        private int lastPoolCount = -1;

        internal PageState(UICharacterView view, UICharacterModel model, GList list)
        {
            View = view; Model = model; List = list;
            Page.Reconcile(model.ListRole.Count);
            originalRenderer = list.itemRenderer;
            pageRenderer = (ListItemRenderer)(Action<int, GObject>)Render;
            layout = list.layout;
            alignment = list.align;
            selection = list.selectionMode;
            scrollOnClick = list.scrollItemToViewOnClick;
            pane = list.scrollPane;
            touch = pane?.touchEffect ?? false;
            wheel = pane?.mouseWheelEnabled ?? false;
        }
        internal void Attach()
        {
            List.itemRenderer = pageRenderer;
            List.layout = ListLayoutType.SingleRow;
            List.align = AlignType.Center;
            List.selectionMode = ListSelectionMode.None;
            List.scrollItemToViewOnClick = false;
            if (pane != null)
            {
                pane.CancelDragging();
                pane.KillTween();
                pane.touchEffect = false;
                pane.mouseWheelEnabled = false;
            }
        }
        private void Render(int localIndex, GObject row)
        {
            try
            {
                int globalIndex = Page.GlobalIndex(localIndex);
                if (globalIndex < 0 || Model.ListRole == null || globalIndex >= Model.ListRole.Count)
                    throw new InvalidOperationException("Page slot is outside the current roster.");
                // The native renderer captures this global index in its existing mouse click delegate.
                // It also computes selection/job separators using the full roster index.
                originalRenderer.Invoke(globalIndex, row);
            }
            catch (Exception ex) { DeferRecovery(ex); }
        }
        internal void Pin()
        {
            List.EnsureBoundsCorrect();
            if (pane != null)
            {
                pane.KillTween();
                pane.SetPosX(0, false);
                pane.SetPosY(0, false);
            }
            ClearIdlePortraits(false);
        }
        private void ClearIdlePortraits(bool force)
        {
            if (!cleanupEnabled || List.isDisposed) return;
            try
            {
                int count = List.itemPool?.count ?? 0;
                if (!force && count == lastPoolCount) return;
                lastPoolCount = count;
                ReportCleanup("pool", PortraitResources.ClearPool(List));
            }
            catch (Exception ex) { CleanupFailed(ex); }
        }
        internal void ClearClosedDetails()
        {
            if (!cleanupEnabled) return;
            try
            {
                var content = View._UIContent_k__BackingField;
                if (content == null || content.isDisposed) return;
                // Only on closing this paginated view, never on a page turn or a tab switch.
                var result = PortraitResources.ClearLoader(content.loaderRole);
                var tip = content.roleInfo?.loaderRole;
                if (tip != null && tip.Pointer != content.loaderRole?.Pointer)
                    result += PortraitResources.ClearLoader(tip);
                ReportCleanup("closed-details", result);
            }
            catch (Exception ex) { CleanupFailed(ex); }
        }
        private static void ReportCleanup(string reason, PortraitResources.Result result)
        {
            if (result.Urls != 0 || result.Pending != 0)
                host?.LoggerInstance.Msg($"Characters cleanup {reason}: rows={result.Rows}, urls={result.Urls}, remainingOperationsReleased={result.Pending}. Not a freed-byte measurement.");
        }
        internal void Restore(bool redraw, bool disposing)
        {
            if (List.isDisposed) return;
            // Restore only renderer ownership; do not overwrite another mod's replacement.
            bool ownsRenderer = List.itemRenderer?.Pointer == pageRenderer.Pointer;
            if (ownsRenderer) List.itemRenderer = originalRenderer;
            if (disposing) return;
            if (List.layout == ListLayoutType.SingleRow) List.layout = layout;
            if (List.align == AlignType.Center) List.align = alignment;
            if (List.selectionMode == ListSelectionMode.None) List.selectionMode = selection;
            if (!List.scrollItemToViewOnClick) List.scrollItemToViewOnClick = scrollOnClick;
            if (pane != null)
            {
                if (!pane.touchEffect) pane.touchEffect = touch;
                if (!pane.mouseWheelEnabled) pane.mouseWheelEnabled = wheel;
            }
            if (ownsRenderer)
            {
                // Clear page rows at close/tab transition so no page-local slots outlive their binding.
                List.numItems = redraw && Model.ListRole != null ? Model.ListRole.Count : 0;
                ClearIdlePortraits(true);
                if (redraw && Model.ListRole != null && Model.RoleIndex >= 0 && Model.RoleIndex < Model.ListRole.Count)
                    List.ScrollToView(Model.RoleIndex, false, false);
            }
        }
    }
}


using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Restitutor.TabCharacters;

// 0.6.0: equipment panel on the Characters sheet. Clicking one of the four equipment
// slots under the portrait opens the native equipment list (4 columns), filter and
// item details beside the slots. Native equipment functions only run for
// SheetType 2 (analysis/character-equip-review/REVIEW.md), so the few native calls
// that change equipment run with SheetType set to 2 for that one synchronous call.
public sealed partial class EntryPoint
{
    private static EquipPanel? equip;
    private static bool equipFault;
    private static GList? equipSlotList;
    private static bool equipSlotListTouchable;

    partial void InstallEquip()
    {
        // Native RefreshTipsRoleInfo rewrites the HP/attack/attribute display. Re-apply the
        // hover preview after it while the panel is open (native equipment sheet parity).
        Patch(typeof(UICharacterView), "RefreshTipsRoleInfo", Type.EmptyTypes, null, nameof(EquipRoleInfoAfter));
    }
    private static void GuardEquip(Action action)
    {
        try { action(); }
        catch (Exception ex) { equipFault = true; host?.LoggerInstance.Error("Equipment panel stopped: " + ex); }
    }

    // Called after every native Refresh of the Characters sheet (RefreshBookBindings).
    static partial void BindEquipSlots(UICharacterView view)
    {
        if (!Allowed || !IsCharacter(view)) return;
        GuardEquip(() =>
        {
            var list = view.SheetCharacter.listEquip;
            if (list == null || list.isDisposed) return;
            if (equipSlotList == null || equipSlotList.Pointer != list.Pointer)
            {
                RestoreEquipSlotList();
                equipSlotList = list; equipSlotListTouchable = list.touchable;
            }
            // The native slots only display icons (RenderListEquipShow registers no events).
            list.touchable = true;
            for (int i = 0; i < list.numChildren; i++)
            {
                var slot = list.GetChildAt(i);
                BindBook(slot, () => OpenEquip(view, slot));
            }
            equipProbeView = view;
            BindSlotCatcher(view);
            TraceSlotBinding(list);
            equip?.SlotsRendered();
        });
    }
    static partial void ClearEquipSlotState() { RestoreEquipSlotList(); UnbindSlotCatcher(); }

    // 0.6.2: the 0.6.1 run showed every press over the slots hits UISheetCharacter.btnReturn
    // (a GGraph drawn above listEquip), so the slots never receive clicks. The press is
    // taken on btnReturn itself: only when it lands inside a slot is the click cancelled
    // (native btnReturn behaviour skipped) and the panel opened. Other presses pass through.
    private static GObject? slotCatcher;
    private static EventCallback1? catcherBegin, catcherEnd;
    private static int catcherSlot = -1;
    private static UICharacterView? catcherView;
    private static int SlotAtPoint(UICharacterView view, float x, float y)
    {
        var list = view.SheetCharacter.listEquip;
        if (list == null || list.isDisposed || !list.visible) return -1;
        for (int i = 0; i < list.numChildren; i++)
            if (EquipInsideAt(list.GetChildAt(i), x, y)) return list.ChildIndexToItemIndex(i);
        return -1;
    }
    private static void BindSlotCatcher(UICharacterView view)
    {
        var target = view.SheetCharacter.btnReturn;
        catcherView = view;
        if (target == null || target.isDisposed || slotCatcher?.Pointer == target.Pointer) return;
        UnbindSlotCatcher();
        catcherView = view;
        catcherBegin = (EventCallback1)(Action<EventContext>)(e => GuardEquip(() =>
        {
            catcherSlot = -1;
            var v = catcherView;
            if (!Allowed || v == null || e.inputEvent.button != 0 || equip != null || !IsCharacter(v)) return;
            int hit = SlotAtPoint(v, e.inputEvent.x, e.inputEvent.y);
            if (hit < 0) return;
            catcherSlot = hit;
            e.CaptureTouch();
            EquipTrace($"slot press taken from btnReturn: slot={hit}");
        }));
        catcherEnd = (EventCallback1)(Action<EventContext>)(e => GuardEquip(() =>
        {
            int pressed = catcherSlot; catcherSlot = -1;
            var v = catcherView;
            if (pressed < 0 || v == null || e.inputEvent.button != 0) return;
            // A press that started on a slot never reaches native btnReturn click.
            e.StopPropagation(); Stage.inst.CancelClick(e.inputEvent.touchId);
            if (SlotAtPoint(v, e.inputEvent.x, e.inputEvent.y) != pressed) return;
            var slots = v.SheetCharacter.listEquip;
            int child = slots.ItemIndexToChildIndex(pressed);
            if (child < 0 || child >= slots.numChildren) return;
            var slotObject = slots.GetChildAt(child);
            OpenEquip(v, slotObject);
        }));
        target.onTouchBegin.Add(catcherBegin);
        target.onTouchEnd.Add(catcherEnd);
        slotCatcher = target;
    }
    private static void UnbindSlotCatcher()
    {
        var target = slotCatcher; slotCatcher = null; catcherSlot = -1; catcherView = null;
        if (target != null && !target.isDisposed)
        {
            if (catcherBegin != null) target.onTouchBegin.Remove(catcherBegin);
            if (catcherEnd != null) target.onTouchEnd.Remove(catcherEnd);
        }
        catcherBegin = null; catcherEnd = null;
    }
    private static void RestoreEquipSlotList()
    {
        var list = equipSlotList; equipSlotList = null;
        if (list != null && !list.isDisposed) list.touchable = equipSlotListTouchable;
    }
    private static int EquipSlotIndex(UICharacterView view, GObject slot)
    {
        var list = view.SheetCharacter.listEquip;
        int child = list.GetChildIndex(slot);
        return child < 0 ? -1 : list.ChildIndexToItemIndex(child);
    }
    private static void OpenEquip(UICharacterView view, GObject slotObject)
    {
        EquipTrace($"slot click received: inputActive={view.IsInputActive} character={IsCharacter(view)}");
        if (!view.IsInputActive || !IsCharacter(view)) { EquipTrace("open refused: view not active or not the Characters sheet"); return; }
        int slot = EquipSlotIndex(view, slotObject);
        if (slot < 0) { EquipTrace("open refused: clicked object is no longer a slot of the list"); return; }
        if (equip != null && equip.View.Pointer == view.Pointer && equip.Valid) { GuardEquip(() => equip?.SelectSlot(slot)); return; }
        CloseEquip("reopened");
        if (books != null) CloseBooks("equipment panel opened");
        var ctrl = UICharacterCtrl.Instance;
        if (ctrl == null || ctrl._View_k__BackingField?.Pointer != view.Pointer) { EquipTrace("open refused: controller view differs"); return; }
        var model = view._model;
        if (model.RoleIndex < 0 || model.RoleIndex >= model.ListRole.Count || model.ListRole[model.RoleIndex].isSeaman) { EquipTrace($"open refused: role index {model.RoleIndex} invalid or seaman"); return; }
        var panel = new EquipPanel(ctrl, view, slot);
        equip = panel;
        GuardEquip(() => panel.Open());
        if (equip == panel) host?.LoggerInstance.Msg($"Characters equipment panel opened (slot={slot}).");
    }
    private static void CloseEquip(string reason)
    {
        var closing = equip; equip = null;
        if (closing == null) return;
        host?.LoggerInstance.Msg("Characters equipment panel closed: " + reason);
        try { closing.Dispose(); }
        catch (Exception ex) { host?.LoggerInstance.Error("Equipment panel cleanup: " + ex); }
    }
    static partial void EquipCloseForSheet(string reason) => CloseEquip(reason);
    static partial void EquipCloseForBooks(string reason) => CloseEquip(reason);
    static partial void ResetEquip() { CloseEquip("game reset"); RestoreEquipSlotList(); UnbindSlotCatcher(); equipConfirmLatch = false; }

    // Diagnostic only: a native Refresh must never run while SheetType is borrowed.
    static partial void EquipRefreshProbe(UICharacterView view)
    {
        if (equip != null && equip.NativeCallActive)
            host?.LoggerInstance.Warning("Characters equipment: view Refresh ran during a borrowed SheetType call.");
    }

    private static void EquipRoleInfoAfter(UICharacterView __instance)
    {
        var panel = equip;
        if (!Allowed || panel == null || panel.View.Pointer != __instance.Pointer) return;
        GuardEquip(() => panel.ReapplyRoleInfo());
    }
    static partial void EquipRowRendered(UICharacterView view, int index, GObject row)
    {
        var panel = equip;
        if (!Allowed || panel == null || panel.View.Pointer != view.Pointer) return;
        GuardEquip(() => panel.RowRendered(index, row));
    }

    // ---------- 0.6.1 diagnostics (slot click produced no reaction in the 0.6.0 run) ----------
    // Bounded: at most 40 lines per session. Read-only: names, touchable flags, hit target.
    private static int equipTraceLines, equipProbeLines;
    private static UICharacterView? equipProbeView;
    private static IntPtr equipTracedList;
    private static int equipTracedChildren = -1;
    private static void EquipTrace(string text)
    {
        if (equipTraceLines >= 40) return;
        equipTraceLines++;
        host?.LoggerInstance.Msg("[EquipTrace] " + text);
    }
    private static string Describe(GObject? obj)
    {
        if (obj == null) return "null";
        var container = obj.displayObject?.TryCast<Container>();
        return $"{obj.GetType().Name}('{obj.name}' touchable={obj.touchable} visible={obj.visible}" +
            (container != null ? $" touchChildren={container.touchChildren}" : "") + ")";
    }
    private static void TraceSlotBinding(GList list)
    {
        if (list.Pointer == equipTracedList && list.numChildren == equipTracedChildren) return;
        equipTracedList = list.Pointer; equipTracedChildren = list.numChildren;
        var children = new List<string>();
        for (int i = 0; i < list.numChildren; i++) children.Add(Describe(list.GetChildAt(i)));
        var ancestors = new List<string>();
        for (GObject? node = list.parent; node != null && ancestors.Count < 8; node = node.parent) ancestors.Add(Describe(node));
        EquipTrace($"slots bound: list={Describe(list)} originalTouchable={equipSlotListTouchable} children=[{string.Join(", ", children)}] ancestors=[{string.Join(" <- ", ancestors)}]");
    }
    // Left press inside the Characters-sheet slot list: which object FairyGUI hit.
    private static void ProbeSlotPress()
    {
        var view = equipProbeView;
        if (equip != null || equipProbeLines >= 3 || equipTraceLines >= 40 || view == null || Mouse.current?.leftButton.wasPressedThisFrame != true) return;
        if (view._UIContent_k__BackingField == null || view._UIContent_k__BackingField.isDisposed || !IsCharacter(view)) return;
        var list = view.SheetCharacter.listEquip;
        if (list == null || list.isDisposed || !EquipInside(list)) return;
        var chain = new List<string>();
        var target = Stage.inst.touchTarget;
        for (var node = target; node != null && chain.Count < 10; node = node.parent)
        {
            var owner = node.gOwner;
            chain.Add(owner != null ? Describe(owner) : $"DisplayObject('{node.name}')");
        }
        equipProbeLines++;
        EquipTrace($"left press over slots: inputActive={view.IsInputActive} books={books != null} hit=[{string.Join(" <- ", chain)}]");
    }

    private static bool equipConfirmLatch;
    static partial void TickEquip()
    {
        ProbeSlotPress();
        if (equipFault) { equipFault = false; CloseEquip("fault recovery; see preceding error"); }
        var panel = equip;
        if (panel == null) { if (equipConfirmLatch && Keyboard.current?.spaceKey.isPressed != true) equipConfirmLatch = false; return; } // 0.6.11: key read only while latched
        if (!panel.Valid) { CloseEquip("view/model/role changed"); return; }
        if (equipConfirmLatch && Keyboard.current?.spaceKey.isPressed != true && !bookHeldActions.Contains("Action_A")) equipConfirmLatch = false;
        if (Application.isFocused)
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            { closeKey = CloseKey.Escape; CloseEquip("physical Escape"); return; }
            if (Mouse.current?.rightButton.wasPressedThisFrame == true)
            {
                closeKey = CloseKey.RightMouse;
                GuardEquip(() => panel.RightPress());
                if (equip != panel) return;
            }
            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true) EquipConfirm("physical Space");
        }
        GuardEquip(() => panel.Tick());
    }
    private static void EquipConfirm(string source)
    {
        var panel = equip;
        if (panel == null || equipConfirmLatch) return;
        var focus = Stage.inst.focus;
        if (focus != null && focus.TryCast<InputTextField>() != null) return;
        equipConfirmLatch = true;
        GuardEquip(() => panel.Confirm(source));
    }
    // While the panel is open, the confirm/back/X/Y commands belong to it. Native
    // Characters-sheet handlers for them (focus change, back) must not run underneath.
    static partial void EquipCapture(UnityEngine.InputSystem.InputAction.CallbackContext context, ref bool handled, ref bool result)
    {
        var panel = equip;
        if (panel == null) return;
        string name = context.action?.name ?? "";
        if (name is not ("Action_A" or "Action_B" or "Action_X" or "Action_Y")) return;
        handled = true; result = false;
        if (!context.performed || !context.ReadValueAsButton()) return;
        bookHeldActions.Add(name);
        switch (name)
        {
            case "Action_B":
                // Right mouse and Escape are handled from the physical devices in TickEquip.
                if (Mouse.current?.rightButton.isPressed != true && Keyboard.current?.escapeKey.isPressed != true)
                { CloseEquip("Action_B"); }
                break;
            case "Action_A":
                if (Keyboard.current?.spaceKey.isPressed != true) EquipConfirm("Action_A");
                break;
            case "Action_Y":
                GuardEquip(() => panel.ToggleFilter()); // native OnAction_Y on the equipment sheet
                break;
        }
    }
}

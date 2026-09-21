using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using UnityEngine;

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint
{
    private sealed partial class EquipPanel : IDisposable
    {
        const int SheetEquip = 2;     // ESheetType value used by every native equipment function
        const int FocusWearSlot = 3;  // ECurFoucType value: OnTakeOffEquip takes CurHeroEquip[WearEquipIndex]
        const int FocusNeutral = 0;   // not 2/4: ChangeCurRoleIndex runs no focus-specific branch
        internal readonly UICharacterCtrl Ctrl;
        internal readonly UICharacterView View;
        readonly UICharacterModel model;
        readonly Il2CppCharacter.UISheetEquip sheet;
        readonly int roleIndex;
        readonly IntPtr rolePointer;
        // Model state owned by the native equipment sheet; restored on close.
        readonly int savedEquipIndex, savedWearIndex, savedFilterEquip;
        readonly bool savedFilterOpen;
        readonly ECurFoucType savedFocus;
        readonly ListItemRenderer originalListRenderer, originalFilterRenderer, listRenderer, filterRenderer;
        readonly List<(EventListener Listener, EventCallback1 Callback)> listeners = new();
        readonly Dictionary<IntPtr, GObject> touchedRows = new();
        int slot, hover = -1;
        GObject? hoverRow;
        bool disposed, refreshing;
        internal bool NativeCallActive;

        internal bool Valid => !disposed && View._UIContent_k__BackingField != null && !View._UIContent_k__BackingField.isDisposed
            && !sheet.isDisposed && View._model.Pointer == model.Pointer && IsCharacter(View) && model.RoleIndex == roleIndex
            && roleIndex >= 0 && roleIndex < model.ListRole.Count && model.ListRole[roleIndex].HeroData.Pointer == rolePointer
            && !model.ListRole[roleIndex].isSeaman;

        internal EquipPanel(UICharacterCtrl ctrl, UICharacterView view, int wearSlot)
        {
            Ctrl = ctrl; View = view; model = view._model; sheet = view.SheetEquip;
            roleIndex = model.RoleIndex; rolePointer = model.ListRole[roleIndex].HeroData.Pointer;
            slot = wearSlot;
            savedEquipIndex = model._equipIndex; savedWearIndex = model._wearEquipIndex;
            savedFilterOpen = model.IsSelectEquipFilter; savedFilterEquip = model._filterIndexEquip; savedFocus = model.CurFoucType;
            originalListRenderer = sheet.listEquip.itemRenderer;
            originalFilterRenderer = sheet.comFilter.listBtn.itemRenderer;
            listRenderer = (ListItemRenderer)(Action<int, GObject>)RenderRow;
            filterRenderer = (ListItemRenderer)(Action<int, GObject>)RenderFilter;
        }
        void Listen(EventListener listener, Action<EventContext> action)
        { var callback = (EventCallback1)action; listeners.Add((listener, callback)); listener.Add(callback); }

        internal void Open()
        {
            // Same data refresh the native sheet receives from SyncPlayerDataToModel/ChangeCurRoleIndex.
            Ctrl.InitEquipData(UICharacterCtrl.Data.PlayerEquipData);
            Ctrl.InitHeroEquipData(model.ListRole[roleIndex]);
            GroupList();
            model.IsSelectEquipFilter = false; model._filterIndexEquip = -1;
            // Fields, not the setters: set_EquipIndex/set_WearEquipIndex also MarkDirty.
            model._wearEquipIndex = slot; model._equipIndex = -1;
            BuildOverlay();
            Borrow();
            sheet.listEquip.itemRenderer = listRenderer;
            sheet.comFilter.listBtn.itemRenderer = filterRenderer;
            Listen(sheet.comFilter.btnMain.onClick, e => { e.StopPropagation(); GuardEquip(ToggleFilter); });
            GRoot.inst.AddChild(overlay!);
            Refresh();
            ScrollToSlotItem();
        }

        // ---------- data / refresh ----------
        EquipItem? SlotItem => model.TakeOffEquip; // CurHeroEquip[WearEquipIndex] or null
        Il2CppSystem.Collections.Generic.List<EquipItem>? Items => model.ListFilterEquip;
        internal void Refresh()
        {
            if (refreshing || !Valid || overlay == null) return;
            refreshing = true;
            try
            {
                int count = Items?.Count ?? 0;
                if (hover >= count) { hover = -1; hoverRow = null; }
                model._wearEquipIndex = slot; model._equipIndex = hover;
                sheet.noEquip.selectedIndex = count == 0 ? 1 : 0;
                sheet.listEquip.numItems = count;
                sheet.listEquip.selectedIndex = hover;
                RefreshFilter();
                RefreshTips();
                LayoutPanel();
            }
            finally { refreshing = false; }
            ApplyPreview();
        }
        void RefreshTips()
        {
            var items = Items;
            EquipItem? item = hover >= 0 && items != null && hover < items.Count ? items[hover] : SlotItem;
            if (item != null) View.RefreshEquipTips(item);
            sheet.noSelectEquip.selectedIndex = item == null ? 1 : 0;
            tipItem = item != null;
        }
        void RenderRow(int index, GObject obj)
        {
            GuardEquip(() =>
            {
                if (disposed) return;
                View.RenderListEquip(index, obj);
                UpdateCountBadge(index, obj);
                int captured = index;
                // The native renderer's callbacks call equipment-sheet functions that do
                // nothing on the Characters sheet; this panel replaces them per render.
                obj.onClick.Set((EventCallback1)(Action<EventContext>)(e => { e.StopPropagation(); GuardEquip(() => ClickItem(captured)); }));
                obj.onRollOver.Set((EventCallback1)(Action<EventContext>)(e => GuardEquip(() => HoverTo(captured, obj))));
                obj.onRollOut.Set((EventCallback1)(Action<EventContext>)(e => GuardEquip(() => HoverOut(captured))));
                touchedRows[obj.Pointer] = obj;
            });
        }
        void HoverTo(int index, GObject row)
        {
            if (!Valid || NativeCallActive) return;
            if (EnsureGrouped("hover")) return; // rows were redrawn; Tick re-derives the hover
            var items = Items;
            if (items == null || index < 0 || index >= items.Count) return;
            if (hover == index && hoverRow?.Pointer == row.Pointer) return;
            SetRowSelected(hover, false);
            hover = index; hoverRow = row;
            model._equipIndex = hover;
            SetRowSelected(hover, true);
            RefreshTips(); LayoutPanel(); ApplyPreview();
        }
        void HoverOut(int index)
        {
            if (!Valid || NativeCallActive || hover != index) return;
            SetRowSelected(hover, false);
            hover = -1; hoverRow = null; model._equipIndex = -1;
            RefreshTips(); LayoutPanel(); ApplyPreview();
        }
        void SetRowSelected(int index, bool selected)
        {
            if (index < 0) return;
            var list = sheet.listEquip;
            int child = list.ItemIndexToChildIndex(index);
            if (child < 0 || child >= list.numChildren) return;
            var row = list.GetChildAt(child).TryCast<Il2CppCharacter.UIBtn_Equip>();
            if (row != null) row.isSelect.selectedIndex = selected ? 1 : 0;
        }
        void ScrollToSlotItem()
        {
            var item = SlotItem; var items = Items;
            if (item == null || items == null) return;
            long guid = item.Guid;
            for (int i = 0; i < items.Count; i++)
                if (items[i].Guid == guid) { sheet.listEquip.ScrollToView(i, false, false); return; }
        }
        internal void SelectSlot(int wearSlot)
        {
            if (!Valid || wearSlot == slot) return;
            ClearPreview();
            slot = wearSlot; hover = -1; hoverRow = null;
            Refresh();
            ScrollToSlotItem();
        }
        internal void SlotsRendered() { if (Valid) LayoutPanel(); }

        // ---------- filter ----------
        void RefreshFilter()
        {
            var filter = sheet.comFilter;
            bool open = model.IsSelectEquipFilter;
            filter.isShow.selectedIndex = open ? 1 : 0;
            if (open)
            {
                filter.listBtn.numItems = model.ListFilterBtnEquip.Count;
                filter.listBtn.selectedIndex = model._filterIndexEquip;
            }
        }
        internal void ToggleFilter()
        {
            if (!Valid || NativeCallActive) return;
            // Same state change as native OnClickBtnFilter on the equipment sheet.
            bool open = !model.IsSelectEquipFilter;
            model.IsSelectEquipFilter = open; model._filterIndexEquip = open ? 0 : -1;
            RefreshFilter();
        }
        void RenderFilter(int index, GObject obj)
        {
            GuardEquip(() =>
            {
                if (disposed) return;
                View.RenderListFilter(index, obj);
                int captured = index;
                obj.onClick.Set((EventCallback1)(Action<EventContext>)(e => { e.StopPropagation(); GuardEquip(() => ApplyFilter(captured)); }));
                obj.onRollOver.Set((EventCallback1)(Action<EventContext>)(e => GuardEquip(() =>
                { model._filterIndexEquip = captured; sheet.comFilter.listBtn.selectedIndex = captured; })));
                touchedRows[obj.Pointer] = obj;
            });
        }
        void ApplyFilter(int index)
        {
            if (!Valid || NativeCallActive) return;
            var buttons = model.ListFilterBtnEquip;
            if (index < 0 || index >= buttons.Count) return;
            // Native OnClickListFilter minus its equipment-sheet-only parts: the filter id
            // becomes FilterIndex, the filtered list is rebuilt and the dropdown closes.
            model._filterIndex = buttons[index];
            model.SetFilterListEquip();
            GroupList();
            model.IsSelectEquipFilter = false; model._filterIndexEquip = -1;
            View.RefreshText(); // native writes the current filter name into texType here
            ClearPreview(); hover = -1; hoverRow = null;
            Refresh();
            sheet.listEquip.scrollPane?.SetPosY(0, false);
        }

        // ---------- native actions ----------
        void RunNative(string what, int focus, Action action)
        {
            if (NativeCallActive) return;
            ClearPreview();
            var sheetType = model._sheetType; var focusType = model.CurFoucType;
            bool filterOpen = model.IsSelectEquipFilter;
            // PlayChangeEquipAni reads the equipment sheet's own wear list at WearEquipIndex
            // (GetChildAt, no bounds check). It is empty until the equipment tab was shown.
            var wearList = sheet.listEquipWear;
            if (wearList.numChildren <= slot) wearList.numItems = Math.Max(slot + 1, UICharacterModel.MaxEquipWearCount);
            NativeCallActive = true;
            try
            {
                model.IsSelectEquipFilter = false;
                model._wearEquipIndex = slot;
                model._sheetType = (ESheetType)SheetEquip;
                model.CurFoucType = (ECurFoucType)focus;
                action();
            }
            finally
            {
                model._sheetType = sheetType; model.CurFoucType = focusType;
                model.IsSelectEquipFilter = filterOpen;
                NativeCallActive = false;
            }
            host?.LoggerInstance.Msg($"Characters equipment {what} (slot={slot}).");
            AfterNative();
        }
        void AfterNative()
        {
            if (!Valid) return;
            long keep = 0;
            var items = Items;
            if (hover >= 0 && items != null && hover < items.Count) keep = items[hover].Guid;
            // Same model refresh the equipment sheet gets from SyncPlayerDataToModel.
            Ctrl.InitEquipData(UICharacterCtrl.Data.PlayerEquipData);
            Ctrl.InitHeroEquipData(model.ListRole[roleIndex]);
            GroupList();
            hover = -1; hoverRow = null;
            items = Items;
            if (keep != 0 && items != null)
                for (int i = 0; i < items.Count; i++) if (items[i].Guid == keep) { hover = i; break; }
            Refresh();
            if (hover >= 0)
            {
                int child = sheet.listEquip.ItemIndexToChildIndex(hover);
                hoverRow = child >= 0 && child < sheet.listEquip.numChildren ? sheet.listEquip.GetChildAt(child) : null;
                if (hoverRow == null || !EquipInside(hoverRow)) { SetRowSelected(hover, false); hover = -1; hoverRow = null; model._equipIndex = -1; RefreshTips(); LayoutPanel(); ApplyPreview(); }
            }
        }
        // Left click / Space on a list item: native OnClickBtnListEquip (take off when it is
        // this slot's item, otherwise wear checks, tips and ChangeEquip into this slot).
        void ClickItem(int index)
        {
            if (!Valid || NativeCallActive) return;
            index = Resolve(index, "list click");
            var items = Items;
            if (items == null || index < 0 || index >= items.Count) return;
            if (model.IsSelectEquipFilter) { model.IsSelectEquipFilter = false; model._filterIndexEquip = -1; RefreshFilter(); }
            RunNative($"list click guid={items[index].Guid} wearRole={items[index].WearRoleId}", FocusNeutral, () => Ctrl.OnClickBtnListEquip(index));
        }
        internal void Confirm(string source)
        {
            if (!Valid) return;
            if (model.IsSelectEquipFilter)
            {
                int f = model._filterIndexEquip;
                if (f >= 0) ApplyFilter(f);
                return;
            }
            // Native OnAction_A: the list item under the pointer, otherwise nothing.
            if (hover >= 0) { host?.LoggerInstance.Msg("Characters equipment confirm by " + source); ClickItem(hover); }
        }
        // Right mouse: on the selected slot -> take off that slot's item (requested change);
        // on a list item -> native OnTakeOffEquip for the item under the pointer;
        // anywhere else -> close.
        internal void RightPress()
        {
            var pos = Stage.inst.touchPosition;
            var slotObject = SlotObject(slot);
            if (slotObject != null && EquipInsideAt(slotObject, pos.x, pos.y))
            {
                if (SlotItem == null) return;
                RunNative("slot take-off", FocusWearSlot, () => Ctrl.OnTakeOffEquip());
                return;
            }
            if (hover >= 0 && hoverRow != null && EquipInsideAt(hoverRow, pos.x, pos.y) && !model.IsSelectEquipFilter)
            {
                int index = Resolve(hover, "list take-off");
                var items = Items;
                if (items == null || index < 0 || index >= items.Count) return;
                RunNative($"list take-off guid={items[index].Guid} wearRole={items[index].WearRoleId}", FocusNeutral, () => { model._equipIndex = index; Ctrl.OnTakeOffEquip(); });
                return;
            }
            CloseEquip("physical right mouse");
        }

        internal void Tick()
        {
            if (overlay == null) return;
            EnsureGrouped("tick");
            if (hover >= 0 && hoverRow != null)
            {
                if (hoverRow.isDisposed || !EquipInside(hoverRow)) HoverOut(hover);
                else
                {
                    // A virtual list may reuse the hovered row for another item after scrolling.
                    int child = sheet.listEquip.GetChildIndex(hoverRow);
                    int item = child < 0 ? -1 : sheet.listEquip.ChildIndexToItemIndex(child);
                    if (item != hover) { if (item >= 0) HoverTo(item, hoverRow); else HoverOut(hover); }
                }
            }
            if (LayoutMoved()) LayoutPanel();
        }

        public void Dispose()
        {
            if (disposed) return;
            bool live = Valid;
            disposed = true;
            try
            {
                foreach (var entry in listeners) entry.Listener.Remove(entry.Callback);
                listeners.Clear();
                ClearPreview();
                DisposeCountBadges();
                if (!sheet.isDisposed)
                {
                    foreach (var row in touchedRows.Values)
                        if (!row.isDisposed) { row.onClick.Clear(); row.onRollOver.Clear(); row.onRollOut.Clear(); }
                    sheet.listEquip.numItems = 0;
                    sheet.listEquip.itemRenderer = originalListRenderer;
                    sheet.comFilter.listBtn.numItems = 0;
                    sheet.comFilter.listBtn.itemRenderer = originalFilterRenderer;
                    sheet.comFilter.isShow.selectedIndex = savedFilterOpen ? 1 : 0;
                }
                touchedRows.Clear();
                RestoreBorrowed();
                model._equipIndex = savedEquipIndex; model._wearEquipIndex = savedWearIndex;
                model.IsSelectEquipFilter = savedFilterOpen; model._filterIndexEquip = savedFilterEquip;
                model.CurFoucType = savedFocus;
                model.ListPropEquip?.Clear();
                if (live) UngroupList();
                if (live) View.RefreshTipsRoleInfo();
            }
            finally
            {
                // Even a failed restore must never let the overlay dispose native widgets.
                foreach (var widget in movedWidgets)
                    if (!widget.isDisposed && IsUnderOverlay(widget)) widget.RemoveFromParent();
                movedWidgets.Clear();
                previewBox?.Dispose(); previewBox = null;
                previewRows?.Dispose(); previewRows = null;
                // Borrowed native widgets are back in the sheet; never let the overlay dispose them.
                overlay?.Dispose(); overlay = null;
            }
        }
    }
}

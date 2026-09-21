using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using UnityEngine;

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint
{
    private static Rect EquipRootBounds(GObject obj)
    {
        if (obj.displayObject == null)
        {
            var owner = obj.parent ?? throw new InvalidOperationException("Detached UI helper has no coordinate owner.");
            return GRoot.inst.GlobalToLocal(owner.LocalToGlobal(new Rect(obj.x, obj.y, obj.width, obj.height)));
        }
        return GRoot.inst.GlobalToLocal(obj.LocalToGlobal(new Rect(0, 0, obj.width, obj.height)));
    }
    private static bool EquipInside(GObject? obj) => EquipInsideAt(obj, Stage.inst.touchPosition.x, Stage.inst.touchPosition.y);
    private static bool EquipInsideAt(GObject? obj, float x, float y)
    {
        if (obj == null || obj.isDisposed || !obj.visible) return false;
        var local = obj.GlobalToLocal(new Rect(x, y, 0, 0));
        return local.x >= 0 && local.y >= 0 && local.x <= obj.width && local.y <= obj.height;
    }
    private static void EquipGeometry(GObject item, Action action)
    {
        bool locked = item._gearLocked; item._gearLocked = true;
        try { action(); } finally { item._gearLocked = locked; }
    }

    private sealed partial class EquipPanel
    {
        // GRoot units. 0.6.3 (user request): 1.25x wider to the right (list 450 | details 250,
        // about 750) and 2x taller upward (bottom stays on the slots). Item icons scale with
        // the list width (1.25x). The list viewport is sized so the whole panel doubles:
        // 2 x (header + 4 rows at the 0.6.0 scale), re-expressed at the new scale.
        const float Pad = 16, ListWidth = 450, TipWidth = 250, Gap = 16, CloseHeight = 30, CloseWidth = 72;
        const float BaseRows = 4, GrowUp = 2, GrowRight = 1.25f;
        static readonly Color Fill = new(.96f, .95f, .93f, .97f), Border = new(.42f, .42f, .40f, 1), SlotFocus = new(1f, .80f, .22f, 1);
        GComponent? overlay, panel, listPane, tipPane;
        GGraph? outside, background, tail, slotFrame, closeButton;
        GTextField? closeLabel;
        bool tipItem, outsideArmed;
        int armedSlot = -1;
        readonly List<Borrowed> borrowed = new();
        float listNativeWidth, tipNativeWidth;

        sealed class Borrowed
        {
            internal readonly GObject Widget;
            internal readonly float Width, Height;
            readonly float x, y, scaleX, scaleY;
            readonly bool visible, touchable;
            readonly GGroup? group;
            readonly GComponent? parent;
            readonly int index;
            readonly GTextField? text;
            readonly AutoSizeType autoSize;
            readonly bool singleLine;
            readonly GObject relationStorage = new GObject();
            readonly uint displayLock;
            internal Borrowed(GObject widget)
            {
                Widget = widget; x = widget.x; y = widget.y; Width = widget.width; Height = widget.height;
                scaleX = widget.scaleX; scaleY = widget.scaleY; visible = widget.visible; touchable = widget.touchable;
                group = widget.group; parent = widget.parent; index = parent?.GetChildIndex(widget) ?? 0;
                text = widget.TryCast<GTextField>();
                if (text != null) { autoSize = text.autoSize; singleLine = text.singleLine; }
                // Keep the native relations aside; while borrowed they must not follow the sheet.
                relationStorage.relations.CopyFrom(widget.relations);
                widget.relations.ClearAll(); widget.group = null;
                // Moved out of the sheet, sheet controllers no longer reach this widget's
                // display gear. The lock lets the panel decide visibility; released on return.
                displayLock = widget.AddDisplayLock();
            }
            internal void RestoreGeometry()
            {
                if (Widget.isDisposed) return;
                if (parent != null && !parent.isDisposed && Widget.parent?.Pointer != parent.Pointer)
                    parent.AddChildAt(Widget, Math.Min(index, parent.numChildren));
                else if (parent == null || parent.isDisposed) Widget.RemoveFromParent();
                if (text != null) EquipGeometry(text, () => { text.autoSize = autoSize; text.singleLine = singleLine; });
                EquipGeometry(Widget, () => { Widget.SetSize(Width, Height); Widget.SetXY(x, y); Widget.SetScale(scaleX, scaleY); });
                Widget.visible = visible; Widget.touchable = touchable;
            }
            internal void RestoreRelations()
            {
                if (!Widget.isDisposed)
                {
                    Widget.ReleaseDisplayLock(displayLock);
                    Widget.group = group; Widget.relations.CopyFrom(relationStorage.relations);
                }
                relationStorage.Dispose();
            }
        }
        readonly List<GObject> movedWidgets = new();
        void Move(GObject widget, GComponent target)
        {
            if (widget == null || widget.isDisposed) return;
            var item = new Borrowed(widget);
            borrowed.Add(item); movedWidgets.Add(widget);
            target.AddChild(widget);
        }
        bool IsUnderOverlay(GObject widget)
        {
            for (var node = widget.parent; node != null; node = node.parent)
                if (node.Pointer == overlay?.Pointer) return true;
            return false;
        }
        void RestoreBorrowed()
        {
            var all = borrowed.ToArray(); borrowed.Clear();
            foreach (var item in all) item.RestoreGeometry();
            foreach (var item in all) item.RestoreRelations();
        }
        float NativeWidth(GObject widget, float fallback)
        {
            foreach (var item in borrowed) if (item.Widget.Pointer == widget.Pointer) return Math.Max(1, item.Width);
            return fallback;
        }

        GGraph Graph(GComponent host, float w, float h, Color color)
        {
            var g = new GGraph(); g.DrawRect(w, h, 0, Color.clear, color); host.AddChild(g); return g;
        }
        void BuildOverlay()
        {
            var root = GRoot.inst;
            overlay = new GComponent(); overlay.SetSize(root.width, root.height); overlay.sortingOrder = 32000;
            outside = Graph(overlay, root.width, root.height, Color.clear);
            Listen(outside.onTouchBegin, e =>
            {
                outsideArmed = false; armedSlot = -1;
                if (e.inputEvent.button != 0) return;
                float x = e.inputEvent.x, y = e.inputEvent.y;
                if (EquipInsideAt(panel, x, y)) return;
                e.CaptureTouch(); e.StopPropagation(); Stage.inst.CancelClick(e.inputEvent.touchId);
                int hit = SlotAt(x, y);
                if (hit >= 0) armedSlot = hit; else outsideArmed = true;
            });
            Listen(outside.onTouchEnd, e =>
            {
                bool armed = outsideArmed; int hitSlot = armedSlot;
                outsideArmed = false; armedSlot = -1;
                if (e.inputEvent.button != 0 || (!armed && hitSlot < 0)) return;
                e.StopPropagation(); Stage.inst.CancelClick(e.inputEvent.touchId);
                float x = e.inputEvent.x, y = e.inputEvent.y;
                if (hitSlot >= 0) { if (SlotAt(x, y) == hitSlot) GuardEquip(() => SelectSlot(hitSlot)); return; }
                if (EquipInsideAt(panel, x, y) || SlotAt(x, y) >= 0) return;
                GuardEquip(() => CloseEquip("outside left release"));
            });
            slotFrame = Graph(overlay, 1, 1, Color.clear); slotFrame.touchable = false;
            tail = Graph(overlay, 24, 24, Fill); tail.touchable = false;
            tail.SetPivot(.5f, .5f, true); tail.rotation = 45;
            panel = new GComponent(); overlay.AddChild(panel);
            background = Graph(panel, 1, 1, Fill);
            listPane = new GComponent(); panel.AddChild(listPane);
            tipPane = new GComponent(); panel.AddChild(tipPane);
            closeButton = Graph(panel, CloseWidth, CloseHeight, new Color(.84f, .84f, .82f, 1));
            // 0.6.4: the 0.6.0 fixed-size label showed no text in game (cause not confirmed).
            // Same construction as the book panel's working labels: auto-size, then centred.
            closeLabel = new GTextField { touchable = false, singleLine = true, autoSize = AutoSizeType.Both };
            var format = closeLabel.textFormat;
            format.font = View.SheetCharacter.texSkillDesc.textFormat.font;
            format.size = (int)Math.Round(CloseHeight * .62f);
            format.color = new Color(.18f, .18f, .18f, 1); format.align = AlignType.Center;
            closeLabel.textFormat = format; closeLabel.text = "닫기";
            panel.AddChild(closeLabel);
            Listen(closeButton.onClick, e => { e.StopPropagation(); GuardEquip(() => CloseEquip("close button")); });
            CreatePreviewBox();
        }
        void Borrow()
        {
            // List side: current filter name, filter button/dropdown and the 4-column list.
            Move(sheet.loaderType, listPane!);
            Move(sheet.texType, listPane!);
            Move(sheet.listEquip, listPane!);
            Move(sheet.TexTitleNoEquip, listPane!);
            Move(sheet.comFilter, listPane!); // last: its dropdown draws above the list
            // Detail side: native RefreshEquipTips writes these.
            Move(sheet.texName, tipPane!);
            Move(sheet.TexTitleEquipCondition, tipPane!);
            Move(sheet.listCondition, tipPane!);
            Move(sheet.texEquipDesc, tipPane!);
            Move(sheet.TexTitleEquipProp, tipPane!);
            Move(sheet.listEquipProp, tipPane!);
            Move(sheet.TexTitleNoSelectEquip, tipPane!);
            listNativeWidth = NativeWidth(sheet.listEquip, 440);
            tipNativeWidth = Math.Max(NativeWidth(sheet.texEquipDesc, 1), Math.Max(NativeWidth(sheet.listEquipProp, 1), NativeWidth(sheet.texName, 1)));
            if (tipNativeWidth <= 1) tipNativeWidth = 340;
        }

        GObject? SlotObject(int wearSlot)
        {
            var list = View.SheetCharacter.listEquip;
            if (list == null || list.isDisposed) return null;
            int child = list.ItemIndexToChildIndex(wearSlot);
            return child >= 0 && child < list.numChildren ? list.GetChildAt(child) : null;
        }
        int SlotAt(float x, float y)
        {
            var list = View.SheetCharacter.listEquip;
            if (list == null || list.isDisposed) return -1;
            for (int i = 0; i < list.numChildren; i++)
                if (EquipInsideAt(list.GetChildAt(i), x, y)) return list.ChildIndexToItemIndex(i);
            return -1;
        }

        static void Place(GObject item, float x, float y, float width, float height)
        { item.visible = true; EquipGeometry(item, () => { item.SetScale(1, 1); item.SetSize(width, height); item.SetXY(x, y); }); }
        static float PlaceText(GTextField text, float y, float width)
        {
            text.visible = true;
            EquipGeometry(text, () => { text.SetScale(1, 1); text.singleLine = false; text.autoSize = AutoSizeType.Height; text.SetSize(width, 30); text.SetXY(0, y); });
            float height = Math.Max(24, text.textHeight + 4);
            EquipGeometry(text, () => text.SetSize(width, height));
            return y + height + 8;
        }
        // Re-layout only when the screen, the slots or the selected slot moved.
        (float, float, Rect, Rect) layoutKey;
        bool LayoutMoved()
        {
            var slotObject = SlotObject(slot);
            var key = (GRoot.inst.width, GRoot.inst.height, EquipRootBounds(View.SheetCharacter.listEquip),
                slotObject == null ? default : EquipRootBounds(slotObject));
            if (key.Equals(layoutKey)) return false;
            return true;
        }
        void LayoutPanel()
        {
            if (overlay == null || panel == null || listPane == null || tipPane == null || background == null || !Valid) return;
            var root = GRoot.inst;
            {
                var keySlot = SlotObject(slot);
                layoutKey = (root.width, root.height, EquipRootBounds(View.SheetCharacter.listEquip), keySlot == null ? default : EquipRootBounds(keySlot));
            }
            overlay.SetSize(root.width, root.height);
            outside!.DrawRect(root.width, root.height, 0, Color.clear, Color.clear);

            // ----- list side, native units, then scaled to ListWidth -----
            float lw = listNativeWidth;
            var filterButton = sheet.comFilter.btnMain;
            float header = Math.Max(Math.Max(sheet.texType.height, sheet.loaderType.height), filterButton.height);
            float iconW = sheet.loaderType.width;
            Place(sheet.loaderType, 0, (header - sheet.loaderType.height) / 2, iconW, sheet.loaderType.height);
            float filterLeft = lw - (filterButton.x + filterButton.width);
            EquipGeometry(sheet.comFilter, () => { sheet.comFilter.SetScale(1, 1); sheet.comFilter.SetXY(filterLeft, (header - filterButton.height) / 2 - filterButton.y); });
            sheet.comFilter.visible = true;
            float textX = iconW + 6;
            Place(sheet.texType, textX, (header - sheet.texType.height) / 2, Math.Max(40, filterLeft + filterButton.x - textX - 8), sheet.texType.height);
            var list = sheet.listEquip;
            float cellHeight = 110;
            if (list.numChildren > 0) cellHeight = Math.Max(1, list.GetChildAt(0).height + list.lineGap);
            float baseHeight = header + 8 + BaseRows * cellHeight - list.lineGap;
            float listHeight = Math.Max(cellHeight, baseHeight * GrowUp / GrowRight - header - 8);
            int count = Items?.Count ?? 0;
            Place(list, 0, header + 8, lw, listHeight);
            list.visible = count > 0;
            if (count == 0) Place(sheet.TexTitleNoEquip, 0, header + 20, lw, sheet.TexTitleNoEquip.height);
            else sheet.TexTitleNoEquip.visible = false;
            float listPaneHeight = header + 8 + listHeight;
            listPane.SetSize(lw, listPaneHeight);
            float listScale = ListWidth / lw;
            listPane.SetScale(listScale, listScale); listPane.SetXY(Pad, Pad);

            // ----- detail side -----
            float tw = tipNativeWidth, y = 0;
            foreach (var widget in new GObject[] { sheet.texName, sheet.TexTitleEquipCondition, sheet.listCondition, sheet.texEquipDesc, sheet.TexTitleEquipProp, sheet.listEquipProp, sheet.TexTitleNoSelectEquip })
                widget.visible = false;
            if (!tipItem) y = PlaceText(sheet.TexTitleNoSelectEquip, y, tw);
            else
            {
                y = PlaceText(sheet.texName, y, tw);
                if (sheet.listCondition.numItems > 0)
                {
                    y = PlaceText(sheet.TexTitleEquipCondition, y, tw);
                    Place(sheet.listCondition, 0, y, NativeWidth(sheet.listCondition, tw), sheet.listCondition.height); y += sheet.listCondition.height + 8;
                }
                y = PlaceText(sheet.texEquipDesc, y, tw);
                if (sheet.listEquipProp.numItems > 0)
                {
                    y = PlaceText(sheet.TexTitleEquipProp, y, tw);
                    Place(sheet.listEquipProp, 0, y, NativeWidth(sheet.listEquipProp, tw), sheet.listEquipProp.height); y += sheet.listEquipProp.height + 8;
                }
            }
            tipPane.SetSize(tw, y);
            float tipScale = TipWidth / tw;
            tipPane.SetScale(tipScale, tipScale);
            tipPane.SetXY(Pad + ListWidth + Gap, Pad + CloseHeight + 8);

            float width = Pad + ListWidth + Gap + TipWidth + Pad;
            float height = Pad + Math.Max(listPaneHeight * listScale, CloseHeight + 8 + y * tipScale) + Pad;
            closeButton!.SetXY(width - Pad - CloseWidth, Pad);
            closeButton.DrawRect(CloseWidth, CloseHeight, 1, Border, new Color(.84f, .84f, .82f, 1));
            // Centre the auto-sized text on the button; keep it above the button graphic.
            panel.SetChildIndex(closeLabel!, panel.numChildren - 1);
            closeLabel!.SetXY(width - Pad - CloseWidth + (CloseWidth - closeLabel.width) / 2, Pad + (CloseHeight - closeLabel.height) / 2);
            background.DrawRect(width, height, 1, Border, Fill);
            panel.SetSize(width, height);

            // ----- place beside the Characters sheet equipment slots, bottom-aligned -----
            var slots = View.SheetCharacter.listEquip;
            var anchor = EquipRootBounds(slots);
            var beside = SidePanelPlacement.Beside(root.width, root.height, anchor.x, anchor.y, anchor.width, width, height, true);
            float scale = beside.Scale;
            float margin = Math.Min(root.width, root.height) * .012f;
            float panelY = Math.Clamp(anchor.y + anchor.height - height * scale, margin, Math.Max(margin, root.height - margin - height * scale));
            panel.SetScale(scale, scale); panel.SetXY(beside.X, panelY);

            // ----- selected slot focus and speech-bubble tail -----
            var slotObject = SlotObject(slot);
            if (slotObject != null)
            {
                var r = EquipRootBounds(slotObject);
                slotFrame!.visible = true; slotFrame.SetXY(r.x - 3, r.y - 3);
                slotFrame.DrawRect(r.width + 6, r.height + 6, 4, SlotFocus, Color.clear);
                float tailY = Math.Clamp(r.y + r.height / 2, panelY + 24 * scale, panelY + (height - 24) * scale);
                tail!.visible = true; tail.DrawRect(24, 24, 1, Border, Fill);
                tail.SetScale(scale, scale);
                tail.SetXY(beside.Right ? beside.X : beside.X + width * scale, tailY);
            }
            else { slotFrame!.visible = false; tail!.visible = false; }
            LayoutBox(beside.X, panelY, width, scale, beside.Right);
        }
    }
}

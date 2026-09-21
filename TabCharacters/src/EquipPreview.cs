using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using UnityEngine;

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint
{
    private sealed partial class EquipPanel
    {
        // Native hover preview (equipment sheet): InitEquipPropChange fills ListPropEquip with
        // -1 x (slot item) and +1 x (hovered item); RefreshPropChangeEffect then writes
        // HP/attack/attributes into the shared roleInfo (RefreshPropEffect) and skill
        // levels into content.listSkill rows (RefreshSkillPropEffect).
        const int SkillProperty = 23; // ChangeProp.PropertyType routed to RefreshSkillPropEffect (0x102CEC0)
        static readonly Color PreviewGreen = new(.24f, .62f, .20f, 1), PreviewRed = new(.75f, .20f, .22f, 1);
        readonly record struct SkillPreview(string Add, Color AddColor, int AddState, int Value);
        sealed record RowSnapshot(GObject Object, Il2CppCharacter.UIBtn_SkillItem Row, string Text, Color Color, int AddState);
        readonly Dictionary<int, SkillPreview> skillPreview = new();
        readonly Dictionary<IntPtr, RowSnapshot> rowSnapshots = new();
        GList? previewRows;
        GComponent? previewBox;
        GGraph? previewBackground;
        bool roleInfoPreviewed, applying;

        void ApplyPreview()
        {
            if (NativeCallActive || applying || disposed) return;
            if (EnsureGrouped("preview")) return; // Refresh inside it applied the preview
            ClearPreview();
            if (hover < 0 || !Valid) { RebuildBox(); return; }
            applying = true;
            try
            {
                model._equipIndex = hover; model._wearEquipIndex = slot;
                var sheetType = model._sheetType;
                // Data only (clears and fills ListPropEquip); returns early unless SheetType is 2.
                try { model._sheetType = (ESheetType)SheetEquip; Ctrl.InitEquipPropChange(); }
                finally { model._sheetType = sheetType; }
                var props = model.ListPropEquip;
                if (props == null || props.Count == 0) return;
                RunPropChangeEffect();
                roleInfoPreviewed = true;
                for (int i = 0; i < props.Count; i++)
                {
                    var prop = props[i];
                    if (prop == null || (int)prop.PropertyType != SkillProperty || prop.Value == 0) continue;
                    if (!model.DictSkill.ContainsKey(prop.Id)) continue;
                    int index = model.DictSkill[prop.Id];
                    if (index < 0 || index >= previewRows!.numChildren) continue;
                    var row = previewRows.GetChildAt(index).TryCast<Il2CppCharacter.UICom_SkillItem>();
                    if (row == null) continue;
                    skillPreview[prop.Id] = new SkillPreview(row.texAddLevel.text, row.texAddLevel.color, row.isAdd.selectedIndex, prop.Value);
                }
                var list = View.SheetCharacter.listSkill;
                for (int child = 0; child < list.numChildren; child++)
                    ApplyRow(list.ChildIndexToItemIndex(child), list.GetChildAt(child));
            }
            finally { applying = false; RebuildBox(); }
        }
        // Unrendered package rows stand in for content.listSkill during the native call,
        // so the preview never renders the equipment sheet's own skill list (no 3D holder).
        void RunPropChangeEffect()
        {
            var content = View._UIContent_k__BackingField;
            var original = content.listSkill;
            if (previewRows == null) previewRows = new GList();
            while (previewRows.numChildren < model.DictSkill.Count)
                previewRows.AddChild(Il2CppCharacter.UICom_SkillItem.CreateInstance());
            try { content.listSkill = previewRows; View.RefreshPropChangeEffect(); }
            finally { content.listSkill = original; }
        }
        internal void ReapplyRoleInfo()
        {
            if (NativeCallActive || applying || disposed || !roleInfoPreviewed) return;
            var props = model.ListPropEquip;
            if (props == null || props.Count == 0) return;
            applying = true;
            try { RunPropChangeEffect(); }
            finally { applying = false; }
        }
        void ApplyRow(int itemIndex, GObject obj)
        {
            if (skillPreview.Count == 0 || itemIndex < 0) return;
            if (!skillPreview.TryGetValue(model.GetSkillIdByIndex(itemIndex), out var preview)) return;
            var row = obj.TryCast<Il2CppCharacter.UIBtn_SkillItem>();
            if (row == null) return;
            if (!rowSnapshots.TryGetValue(obj.Pointer, out var snapshot))
            {
                snapshot = new RowSnapshot(obj, row, row.texAddLevel.text, row.texAddLevel.color, row.isAdd.selectedIndex);
                rowSnapshots[obj.Pointer] = snapshot;
            }
            row.isAdd.selectedIndex = preview.AddState;
            row.texAddLevel.text = preview.Add;
            row.texAddLevel.color = preview.Value > 0 ? IncreaseColor(preview.AddColor) : snapshot.Color;
        }
        // Native state 2 (increase) colour from the package row; a fixed green only if the
        // unrendered row did not apply its colour gear.
        static Color IncreaseColor(Color native) => native.g > native.r * 1.2f && native.g > native.b * 1.1f ? native : PreviewGreen;
        internal void RowRendered(int index, GObject row)
        {
            // The native renderer just rewrote this row: its old snapshot is stale.
            rowSnapshots.Remove(row.Pointer);
            ApplyRow(index, row);
        }
        void ClearPreview()
        {
            foreach (var snapshot in rowSnapshots.Values)
                if (!snapshot.Object.isDisposed)
                {
                    snapshot.Row.isAdd.selectedIndex = snapshot.AddState;
                    snapshot.Row.texAddLevel.text = snapshot.Text;
                    snapshot.Row.texAddLevel.color = snapshot.Color;
                }
            rowSnapshots.Clear();
            skillPreview.Clear();
            if (roleInfoPreviewed)
            {
                roleInfoPreviewed = false;
                if (!disposed && Valid) { applying = true; try { View.RefreshTipsRoleInfo(); } finally { applying = false; } }
            }
        }

        // ---------- box beside the panel: only the skills this item changes ----------
        const float BoxWidth = 300, BoxRow = 38;
        void CreatePreviewBox()
        {
            previewBox = new GComponent { touchable = false, visible = false };
            previewBackground = new GGraph();
            previewBox.AddChild(previewBackground);
            overlay!.AddChild(previewBox);
        }
        GTextField BoxText(GTextField source, string text, float x, float y, float width, AlignType align, Color color)
        {
            var t = new GTextField { touchable = false, singleLine = true, autoSize = AutoSizeType.None };
            var format = t.textFormat;
            format.font = source.textFormat.font; format.size = 20; format.color = color; format.align = align;
            t.textFormat = format; t.text = text;
            t.SetSize(width, BoxRow - 8); t.SetXY(x, y + 6);
            previewBox!.AddChild(t);
            return t;
        }
        void RebuildBox()
        {
            if (previewBox == null || previewBackground == null) return;
            if (disposed || !Valid) { previewBox.visible = false; return; }
            while (previewBox.numChildren > 1) previewBox.RemoveChildAt(1, true);
            float y = 10;
            if (skillPreview.Count > 0)
            {
                var list = View.SheetCharacter.listSkill;
                for (int child = 0; child < list.numChildren; child++)
                {
                    int item = list.ChildIndexToItemIndex(child);
                    if (item < 0 || !skillPreview.TryGetValue(model.GetSkillIdByIndex(item), out var preview)) continue;
                    var row = list.GetChildAt(child).TryCast<Il2CppCharacter.UIBtn_SkillItem>();
                    if (row == null) continue;
                    var icon = new GLoader { touchable = false, fill = FillType.Scale, url = row.loaderIcon.url };
                    icon.SetSize(30, 30); icon.SetXY(12, y + 4); previewBox.AddChild(icon);
                    BoxText(row.texName, row.texName.text, 50, y, 120, AlignType.Left, row.texName.color);
                    // Level from the sheet row (unchanged by equipment), bonus from the native preview.
                    BoxText(row.texLevel, row.texLevel.text, 170, y, 66, AlignType.Right, row.texLevel.color);
                    // Sheet rows keep the native look (increase green, decrease plain). The box
                    // also marks a decrease in red, and shows +0 when the bonus disappears.
                    string add = string.IsNullOrEmpty(preview.Add) ? "+0" : preview.Add;
                    BoxText(row.texAddLevel, add, 238, y, 52, AlignType.Left, preview.Value > 0 ? IncreaseColor(preview.AddColor) : PreviewRed);
                    y += BoxRow;
                }
            }
            previewBox.visible = y > 10;
            if (!previewBox.visible) return;
            float height = y + 10;
            previewBackground.DrawRect(BoxWidth, height, 1, Border, Fill);
            previewBox.SetSize(BoxWidth, height);
            LayoutBox(lastPanelX, lastPanelY, lastPanelWidth, lastScale, lastRight);
        }
        float lastPanelX, lastPanelY, lastPanelWidth, lastScale = 1;
        bool lastRight = true;
        void LayoutBox(float panelX, float panelY, float panelWidth, float scale, bool right)
        {
            lastPanelX = panelX; lastPanelY = panelY; lastPanelWidth = panelWidth; lastScale = scale; lastRight = right;
            if (previewBox == null || !previewBox.visible) return;
            var root = GRoot.inst;
            float gap = 12 * scale, width = BoxWidth * scale;
            float x = right ? panelX + panelWidth * scale + gap : panelX - gap - width;
            x = Math.Clamp(x, 0, Math.Max(0, root.width - width));
            previewBox.SetScale(scale, scale); previewBox.SetXY(x, panelY);
        }
    }
}

using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using UnityEngine;
using EquipList = Il2CppSystem.Collections.Generic.List<Il2CppClient.UILogic.UICharacter.EquipItem>;

namespace Restitutor.TabCharacters;

// 0.6.3: display grouping in the equipment panel list, same policy as Item Rebuild 0.1.17
// bag equipment groups (ItemRebuild/src/EquipmentGroups.cs + Rules.cs): unworn, non-exclusive,
// non-commerce records with no effect GUIDs and equal (ItemId, BuyPrice, Supply, Quality,
// IsSuper, state) share one entry with an "xN" badge. Worn records stay separate.
// Only the panel's view of model.ListFilterEquip changes; GUIDs and saved records do not.
// The first record of each group (native list order) represents it in native calls.
internal static class EquipGroupRules
{
    // Copy of Item Rebuild 0.1.17 Rules.Equipment (types 31/32/33/34; 33062-33070 excluded).
    // Keep in sync with ItemRebuild/src/Rules.cs.
    static readonly HashSet<int> clothes = new() {
        10111,33001,33002,33005,33006,33007,33008,33009,33010,33013,33014,33015,33023,33024,33025,33026,33027,33029,33030,33031,33032,33033,33035,33043,33044,
        33045,33046,33047,33048,33049,33050,33051,33052,33053,33054,33055,33056,33057,33058,33059,33060,33061,33071,33072,33073,33074,34001,34002,34006,34007,
        34009,34010,34011,34014,34026,34033,34038,34039,34042
    };
    static readonly HashSet<int> equipment = new() {
        10120,10142,10143,10157,10175,12028,1314029,31000,31001,31002,31003,31004,31005,31006,31007,31008,31009,31010,31011,31012,31013,31014,31015,31016,31017,31018,31019,31020,31021,31022,31023,34000,34018,34019,34020,34021,34022,34023,34024,34027,34031,34032,34034,34044,34046,
        32000,32001,32002,32003,32004,32005,32006,32007,32008,32009,32010,32011,32012,32013,32014,32015,32016,34025,
        10215,33000,33003,33004,33011,33012,33016,33017,33018,33019,33020,33021,33022,33028,33034,33036,33037,33038,33039,33040,33041,33042,34003,34004,34005,34008,34012,34015,34017,34028,34029,34030,34035,34036,34037,34040,34041,34043,34045
    };
    internal static bool Equipment(int id) => equipment.Contains(id) || clothes.Contains(id);
    internal readonly record struct Key(int Id, int Price, int Supply, int Quality, bool Super, int State);
    internal static Key KeyOf(PlayerItemData x) => new(x.ItemId, x.BuyPrice, x.Supply, x.Quality, x.IsSuper, (int)x.state);
}

public sealed partial class EntryPoint
{
    private sealed partial class EquipPanel
    {
        readonly Dictionary<long, int> groupCounts = new();   // representative GUID -> records
        readonly Dictionary<IntPtr, GTextField> countBadges = new();
        bool grouped;

        bool Groupable(EquipItem item, PlayerEquipDB db)
        {
            var record = item.Equip;
            if (record == null || record.IsCommerce || item.WearRoleId != 0 || item.IsExclusive || !EquipGroupRules.Equipment(record.ItemId)) return false;
            // Same test as Item Rebuild Unworn(): the persistent equipment record must agree.
            var data = db.GetEquip(record.Guid);
            return data != null && data.Equip != null && data.Equip.Guid == record.Guid && data.Equip.ItemId == record.ItemId
                && data.WearRoleId == 0 && data.EquipEffectGuid == 0 && data.SpecialEffectGuid == 0 && data.ExclusiveEffectGuid == 0;
        }
        // Call after anything that rebuilds model.ListFilterEquip (InitEquipData, SetFilterListEquip).
        void GroupList()
        {
            groupCounts.Clear();
            var source = model.ListFilterEquip;
            var db = UICharacterCtrl.Data?.PlayerEquipData;
            if (source == null || db == null) return;
            var result = new EquipList();
            var first = new Dictionary<EquipGroupRules.Key, long>();
            int hidden = 0;
            for (int i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (item == null) continue;
                if (!Groupable(item, db)) { result.Add(item); continue; }
                var key = EquipGroupRules.KeyOf(item.Equip);
                if (first.TryGetValue(key, out long representative)) { groupCounts[representative]++; hidden++; continue; }
                first.Add(key, item.Guid); groupCounts[item.Guid] = 1; result.Add(item);
            }
            model.ListFilterEquip = result;
            shownList = result;
            grouped = true;
            if (!groupLogged) { groupLogged = true; host?.LoggerInstance.Msg($"Characters equipment list grouped: {source.Count} records -> {result.Count} entries ({hidden} unworn duplicates folded)."); }
        }
        static bool groupLogged;

        // 0.6.5: every equipment change marks PlayerEquipDB dirty (TakeOffEquip/PutOnEquip ->
        // PlayerModuleDB.MarkDBDirty). The change event runs later (ProcessDBDirty), and the
        // Characters controller's SyncPlayerDataToModel then calls InitEquipData ->
        // SetFilterListEquip, which puts the native ungrouped list back into the model. The
        // rows on screen still show the grouped list, so the next row index pointed at a
        // different record (0.6.4 user report: every right-click take-off needed two clicks).
        // Every index use now goes through the list the rows were drawn from, by GUID.
        EquipList? shownList;
        static int regroupLogs;
        bool EnsureGrouped(string where)
        {
            if (!grouped || disposed || NativeCallActive) return false;
            var current = model.ListFilterEquip;
            if (current != null && shownList != null && current.Pointer == shownList.Pointer) return false;
            if (regroupLogs < 20) { regroupLogs++; host?.LoggerInstance.Msg($"Characters equipment: model list rebuilt by the game ({current?.Count ?? -1} records) before {where}; regrouped."); }
            long keep = 0;
            if (hover >= 0 && shownList != null && hover < shownList.Count && shownList[hover] != null) keep = shownList[hover].Guid;
            GroupList();
            hover = -1; hoverRow = null;
            var items = Items;
            if (keep != 0 && items != null) for (int i = 0; i < items.Count; i++) if (items[i].Guid == keep) { hover = i; break; }
            model._equipIndex = hover;
            Refresh();
            if (hover >= 0)
            {
                int child = sheet.listEquip.ItemIndexToChildIndex(hover);
                hoverRow = child >= 0 && child < sheet.listEquip.numChildren ? sheet.listEquip.GetChildAt(child) : null;
                if (hoverRow == null) { hover = -1; model._equipIndex = -1; }
            }
            return true;
        }
        // Row index (from the drawn list) -> index in the current model list, or -1.
        int Resolve(int shownIndex, string where)
        {
            var before = shownList;
            long guid = before != null && shownIndex >= 0 && shownIndex < before.Count && before[shownIndex] != null ? before[shownIndex].Guid : 0;
            EnsureGrouped(where);
            var items = Items;
            if (guid == 0 || items == null) return items != null && shownIndex < items.Count ? shownIndex : -1;
            for (int i = 0; i < items.Count; i++) if (items[i].Guid == guid) return i;
            host?.LoggerInstance.Msg($"Characters equipment {where}: record {guid} is no longer in the list; ignored.");
            return -1;
        }
        // Give the native model its full list back (native equipment tab, other readers).
        void UngroupList()
        {
            if (!grouped) return;
            grouped = false; groupCounts.Clear();
            model.SetFilterListEquip();
        }

        // ---------- "xN" badge, bag style (same rules as BookCount.cs / Item Rebuild BagBadge) ----------
        static TextFormat? equipCountFormat;
        TextFormat EquipCountFormat()
        {
            if (equipCountFormat != null) return equipCountFormat;
            var source = new TextFormat();
            Il2CppCommon.UIcompSlotCommon? template = null;
            try
            {
                template = Il2CppCommon.UIcompSlotCommon.CreateInstance();
                if (template?.title == null) throw new InvalidOperationException("Common slot template has no title.");
                source.CopyFrom(template.title.textFormat);
            }
            catch (Exception ex)
            {
                host?.LoggerInstance.Warning("Equipment count style fallback: " + ex.Message);
                source.CopyFrom(View.SheetCharacter.texSkillDesc.textFormat); source.size = 36;
                template?.Dispose();
                return FinishCount(source);
            }
            template?.Dispose();
            equipCountFormat = FinishCount(source);
            return equipCountFormat;
        }
        static TextFormat FinishCount(TextFormat format)
        {
            format.size = Math.Max(1, (int)Math.Round(Math.Round(format.size / 2.0) * 1.1));
            format.color = Color.white; format.gradientColor = null; format.align = AlignType.Right;
            return format;
        }
        void UpdateCountBadge(int index, GObject obj)
        {
            var items = Items;
            int count = 0;
            if (items != null && index >= 0 && index < items.Count && items[index] != null)
                groupCounts.TryGetValue(items[index].Guid, out count);
            countBadges.TryGetValue(obj.Pointer, out var label);
            if (count <= 0) { if (label != null && !label.isDisposed) label.visible = false; return; }
            var slot = obj.TryCast<GComponent>();
            if (slot == null) return;
            if (label == null || label.isDisposed)
            {
                var format = new TextFormat(); format.CopyFrom(EquipCountFormat());
                label = new GTextField { touchable = false, sortingOrder = int.MaxValue, autoSize = AutoSizeType.Shrink, singleLine = true };
                label.textFormat = format; label.align = AlignType.Right; label.verticalAlign = VertAlignType.Bottom;
                label.stroke = 1; label.strokeColor = Color.black;
                slot.AddChild(label);
                countBadges[obj.Pointer] = label;
            }
            var icon = obj.TryCast<Il2CppCharacter.UIBtn_Equip>()?.loaderItem;
            float left, top, w, h;
            if (icon != null && !icon.isDisposed) { left = icon.xMin; top = icon.yMin; w = icon.actualWidth; h = icon.actualHeight; }
            else { left = 0; top = 0; w = obj.width; h = obj.height; }
            if (w <= 0 || h <= 0) { label.visible = false; return; }
            float inset = Math.Min(14, Math.Min(w, h) / 5);
            float height = Math.Min(h - 2 * inset, label.textFormat.size * 1.6f + 4);
            label.SetSize(w - 2 * inset, height);
            label.SetXY(left + inset, top + h - inset - height);
            string text = "x" + count;
            if (label.text != text) label.text = text;
            label.visible = true;
        }
        void DisposeCountBadges()
        {
            foreach (var label in countBadges.Values) if (!label.isDisposed) label.Dispose();
            countBadges.Clear();
        }
    }
}

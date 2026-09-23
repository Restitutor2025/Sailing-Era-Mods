using System.Runtime.CompilerServices;
using MelonLoader;
using Restitutor.Core;
using Il2CppClient.PlayerStore.StorageHistory;
using Il2CppClient.UILogic.UISystem;
using Il2CppLitJson;
using Il2CppFairyGUI;
using Il2CppUISystem;
using Il2CppClient.Utils;

[assembly: MelonInfo(typeof(Restitutor.RebalanceSaveSlots.EntryPoint), "Restitutor Rebalance SaveSlots", "0.2.4", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.RebalanceSaveSlots;

// Save slots 10 -> 101 (2 auto + 99 manual). Evidence (GameAssembly, 2026-09-22):
//  - StorageHistoryManager.InitializeStorageHistories (0x9CE7F0) builds 10 StorageHistory (index 0,1 isAutoSave).
//  - <LoadHistoryData>b__19_1 (0x9D00B0) parses PlayerDataHistoryDb.HistoryJson (whole list) but copies only i < 10.
//  - SaveStorageHistories (0x9CFA30) writes JsonMapper.ToJson(_storageHistories) = the whole list.
//  - UIStorageView.Refresh (0x6638B0) sets listStorage.numItems = 10 (immediate); RenderStorageItem indexes the manager list.
//  - Save/Load/Delete/Move go through TryGetStorageByArchiveIndex (list[index]) and PlayerData file <index>; no range check found.
// 0.1.1 (game test 2026-09-22: rows 11+ blank): RenderStorageItem (0x663C50) plays aniReset (hide) then
//  aniruchang with delay index*0.1 s while the view is opening; ShowHook then sets _isShowing and MarkDirty
//  -> Refresh sets numItems 10 (rows 10+ leave the stage; Transition.OnOwnerRemovedFromStage stops them in
//  the hidden state) and this mod sets 101 again (rows come back without animation = stay hidden).
//  Fix: RenderStorageItem postfix shows rows 10+ at their end state. Refresh postfix also re-applies the
//  selection (set_selectedIndex clears it for index >= child count) and the count text (original: N/10).
// 0.2.0 (user request): the screen opens on the slot used in this run (save or load; else the game's own
//  latest-save slot) instead of row 0, Q / E move the selection 5 rows (page turn), and a hint line under
//  the list shows both keys. Details in src/Nav.cs.
// Eight postfixes + one shared input handler; originals always run. The history file format is unchanged: without this mod the game reads the first 10.
public sealed class EntryPoint : MelonMod
{
    internal const string Version = "0.2.4";
    private static MelonLogger.Instance log = null!;
    private static bool enabled, scrollLogged;
    private static string? lastError;
    private HookSet? hookSet;

    public override void OnInitializeMelon()
    {
        log = LoggerInstance;
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { enabled = false; log.Error("Restitutor.Core.dll is missing from UserLibs; Rebalance SaveSlots stays disabled."); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install()
    {
        if (!CoreInfo.Require(log, "0.2.0")) return;   // 0.2.0: HookSet.Input (Q / E)
        hookSet = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (!hookSet.InstallAll(log, "Rebalance SaveSlots install", () =>
        {
            hookSet.Hook(typeof(StorageHistoryManager), "InitializeStorageHistories", postfix: nameof(AfterInitialize));
            hookSet.Hook(typeof(StorageHistoryManager), "_LoadHistoryData_b__19_1", postfix: nameof(AfterLoad));
            hookSet.Hook(typeof(UIStorageView), "Refresh", postfix: nameof(AfterRefresh));
            hookSet.Hook(typeof(UIStorageView), "RenderStorageItem", postfix: nameof(AfterRenderItem));
            hookSet.Hook(typeof(UIStorageView), "ShowHook", postfix: nameof(AfterShow));
            hookSet.Hook(typeof(UIStorageView), "HideHook", postfix: nameof(AfterHide));
            hookSet.Hook(typeof(UIStorageView), "OnListNavigationItemChanged", postfix: nameof(AfterNav));
            hookSet.Hook(typeof(UIStorageView), "OnStorageItemIndexChanged", postfix: nameof(AfterClick));
            hookSet.Hook(typeof(UISystemCtrl), "ReadStorage", postfix: nameof(AfterRead));
            hookSet.Hook(typeof(UISystemCtrl), "SaveStorage", postfix: nameof(AfterSave));
            Nav.Init(log);
            hookSet.Input("Rebalance SaveSlots", Nav.OnKey);
            enabled = true;
        })) { enabled = false; return; }
        log.Msg($"Rebalance SaveSlots {Version} loaded (Restitutor.Core {CoreInfo.Version}); 10 hooks. Slots {Rules.OriginalCount} -> {Rules.Total} ({Rules.AutoCount} auto + {Rules.ManualCount} manual).");
    }

    private static void AfterInitialize(StorageHistoryManager __instance)
    {
        if (!enabled) return;
        try { Pad(__instance, "initialize"); }
        catch (Exception ex) { Fail("InitializeStorageHistories postfix", ex); }
    }

    private static void AfterLoad(StorageHistoryManager __instance)
    {
        if (!enabled) return;
        try
        {
            var list = __instance._storageHistories;
            if (list == null) return;
            int loaded = list.Count, restored = 0;
            var json = __instance._playerHistoryDb?.HistoryJson;
            if (!string.IsNullOrEmpty(json) && loaded < Rules.Total)
            {
                var parsed = JsonMapper.ToObject<Il2CppSystem.Collections.Generic.List<StorageHistory>>(json);
                if (parsed != null)
                    foreach (var i in Rules.ToRestore(parsed.Count, loaded))
                    {
                        var h = parsed[i];
                        if (h == null) break;
                        h.storageIndex = i;
                        h.isAutoSave = Rules.IsAuto(i);
                        list.Add(h);
                        restored++;
                    }
                log.Msg($"history loaded: game kept {loaded}, file had {parsed?.Count ?? 0}, restored {restored}.");
            }
            Pad(__instance, "load");
        }
        catch (Exception ex) { Fail("LoadHistoryData postfix", ex); }
    }

    private static void Pad(StorageHistoryManager m, string when)
    {
        var list = m._storageHistories;
        if (list == null) return;
        int before = list.Count, added = 0;
        foreach (var i in Rules.Missing(before))
        {
            var h = new StorageHistory();
            h.storageIndex = i;
            h.isAutoSave = Rules.IsAuto(i);
            list.Add(h);
            added++;
        }
        if (added > 0) log.Msg($"{when}: slots {before} -> {list.Count}.");
    }

    private static void AfterRefresh(UIStorageView __instance)
    {
        if (!enabled) return;
        try
        {
            var panel = __instance.UIContent;
            var gl = panel?.listStorage;
            var list = StorageHistoryManager.Instance?._storageHistories;
            if (gl == null || list == null) return;
            if (gl.numItems != list.Count) gl.numItems = list.Count;
            var model = __instance._model;
            if (model != null)
            {
                int sel = Rules.ReselectAfterGrow(model.SelectedStorageIndex, gl.numItems);
                if (sel >= 0 && gl.selectedIndex != sel) gl.selectedIndex = sel;
                var txt = panel!.txtStorageNum;
                var fmt = TextLibUtils.Text("UIStatic_System_Storage_StorageNum", "");
                if (txt != null && !string.IsNullOrEmpty(fmt))
                    txt.text = string.Format(fmt, model.CurrentStorageCount, list.Count);
            }
            Nav.Refreshed(__instance, panel!, gl);
            if (!scrollLogged)
            {
                scrollLogged = true;
                log.Msg($"storage list: numItems {gl.numItems}, scrollPane {(gl.scrollPane != null ? "yes" : "NO — rows past the panel may be clipped")}.");
            }
        }
        catch (Exception ex) { Fail("UIStorageView.Refresh postfix", ex); }
    }

    private static void AfterShow(UIStorageView __instance)
    {
        if (!enabled) return;
        try { Nav.Shown(__instance); } catch (Exception ex) { Fail("ShowHook postfix", ex); }
    }

    private static void AfterHide(UIStorageView __instance)
    {
        if (!enabled) return;
        try { Nav.Hidden(); } catch (Exception ex) { Fail("HideHook postfix", ex); }
    }

    private static void AfterNav()
    {
        if (!enabled) return;
        try { Nav.NavChanged(); Nav.SelectionChanged(); } catch (Exception ex) { Fail("OnListNavigationItemChanged postfix", ex); }
    }

    private static void AfterClick()
    {
        if (!enabled) return;
        try { Nav.SelectionChanged(); } catch (Exception ex) { Fail("OnStorageItemIndexChanged postfix", ex); }
    }

    private static void AfterRead(UISystemCtrl __instance)
    {
        if (!enabled) return;
        try { Nav.Used(__instance, "load"); } catch (Exception ex) { Fail("ReadStorage postfix", ex); }
    }

    private static void AfterSave(UISystemCtrl __instance)
    {
        if (!enabled) return;
        try { Nav.Used(__instance, "save"); } catch (Exception ex) { Fail("SaveStorage postfix", ex); }
    }

    // Rows 10+: finish the hide transition, then run the enter transition to its end at once.
    private static void AfterRenderItem(int index, GObject itemGObject)
    {
        if (!enabled || !Rules.ShowInstantly(index) || itemGObject == null) return;
        try
        {
            var item = itemGObject.TryCast<UIBtnStorageItem>();
            if (item == null) return;
            item.aniReset?.Stop(true, false);
            var enter = item.aniruchang;
            if (enter != null) { enter.Play(); enter.Stop(true, false); }
        }
        catch (Exception ex) { Fail("RenderStorageItem postfix", ex); }
    }

    private static void Fail(string what, Exception ex)
    {
        var key = what + ":" + ex.Message;
        if (lastError == key) return;
        lastError = key; log.Error($"[{what}] {ex}");
    }

    public override void OnDeinitializeMelon()
    {
        enabled = false;
        hookSet?.RemoveAll();
    }
}

using System.Runtime.CompilerServices;
using MelonLoader;
using Restitutor.Core;
using Il2CppClient.PlayerStore.StorageHistory;
using Il2CppClient.UILogic.UISystem;
using Il2CppLitJson;

[assembly: MelonInfo(typeof(Restitutor.RebalanceSaveSlots.EntryPoint), "Restitutor Rebalance SaveSlots", "0.1.0", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.RebalanceSaveSlots;

// Save slots 10 -> 101 (2 auto + 99 manual). Evidence (GameAssembly, 2026-09-22):
//  - StorageHistoryManager.InitializeStorageHistories (0x9CE7F0) builds 10 StorageHistory (index 0,1 isAutoSave).
//  - <LoadHistoryData>b__19_1 (0x9D00B0) parses PlayerDataHistoryDb.HistoryJson (whole list) but copies only i < 10.
//  - SaveStorageHistories (0x9CFA30) writes JsonMapper.ToJson(_storageHistories) = the whole list.
//  - UIStorageView.Refresh (0x6638B0) sets listStorage.numItems = 10 (immediate); RenderStorageItem indexes the manager list.
//  - Save/Load/Delete/Move go through TryGetStorageByArchiveIndex (list[index]) and PlayerData file <index>; no range check found.
// Three postfixes; originals always run. The history file format is unchanged: without this mod the game reads the first 10.
public sealed class EntryPoint : MelonMod
{
    internal const string Version = "0.1.0";
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
        if (!CoreInfo.Require(log, "0.1.0")) return;
        hookSet = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (!hookSet.InstallAll(log, "Rebalance SaveSlots install", () =>
        {
            hookSet.Hook(typeof(StorageHistoryManager), "InitializeStorageHistories", postfix: nameof(AfterInitialize));
            hookSet.Hook(typeof(StorageHistoryManager), "_LoadHistoryData_b__19_1", postfix: nameof(AfterLoad));
            hookSet.Hook(typeof(UIStorageView), "Refresh", postfix: nameof(AfterRefresh));
            enabled = true;
        })) { enabled = false; return; }
        log.Msg($"Rebalance SaveSlots {Version} loaded (Restitutor.Core {CoreInfo.Version}); 3 hooks. Slots {Rules.OriginalCount} -> {Rules.Total} ({Rules.AutoCount} auto + {Rules.ManualCount} manual).");
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
            if (!scrollLogged)
            {
                scrollLogged = true;
                log.Msg($"storage list: numItems {gl.numItems}, scrollPane {(gl.scrollPane != null ? "yes" : "NO — rows past the panel may be clipped")}.");
            }
        }
        catch (Exception ex) { Fail("UIStorageView.Refresh postfix", ex); }
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

using System.Runtime.CompilerServices;
using MelonLoader;
using Restitutor.Core;
using Il2CppGyyx.Template;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

[assembly: MelonInfo(typeof(Restitutor.RebalanceNpcFleets.EntryPoint), "Restitutor Rebalance NpcFleets", "0.1.0", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.RebalanceNpcFleets;

// NPC side of the large Fu ship (대형 복선) removal. Data only: TemplateManager keeps each table in a static
// dictionary read by every caller, so rows are edited once per table load (InitGameConst / InitNpcShipTeam
// postfix; also once at install in case a table is already loaded). No per-frame or per-call work.
// - NPC_BUY_SHIP_LIST loses 300 (NpcManager.OrderShip picks from it when a roaming NPC enters a port).
// - 7 NPC fleets: pirates -> 간증선 flagship + 해창선/개랑선, trade fleets -> 복선 (Rules.Teams).
// Fleets already created in a save keep their ships (the save stores them); new saves / new spawns use the table.
// See handoff/BIG_FUCHUAN_REMOVAL.md and docs/mods/rebalance-npcfleets/.
public sealed class EntryPoint : MelonMod
{
    internal const string Version = "0.1.0";
    private static MelonLogger.Instance log = null!;
    private static bool enabled;
    private static string? lastError, loggedConst, loggedTeams;
    private HookSet? hookSet;

    public override void OnInitializeMelon()
    {
        log = LoggerInstance;
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { enabled = false; log.Error("Restitutor.Core.dll is missing from UserLibs; Rebalance NpcFleets stays disabled."); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install()
    {
        if (!CoreInfo.Require(log, "0.1.0")) return;
        hookSet = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (!hookSet.InstallAll(log, "Rebalance NpcFleets install", () =>
        {
            hookSet.Hook(typeof(TemplateManager), "InitGameConst", postfix: nameof(AfterGameConst));
            hookSet.Hook(typeof(TemplateManager), "InitNpcShipTeam", postfix: nameof(AfterNpcShipTeam));
            enabled = true;
        })) { enabled = false; return; }
        log.Msg($"Rebalance NpcFleets {Version} loaded (Restitutor.Core {CoreInfo.Version}); 2 hooks (table load only). {Rules.BuyListKey} without {Rules.BannedBuy}; {Rules.Teams.Length} NPC fleets re-shipped.");
        ApplyConst("init");
        ApplyTeams("init");
    }

    private static void AfterGameConst() { if (enabled) ApplyConst("InitGameConst"); }
    private static void AfterNpcShipTeam() { if (enabled) ApplyTeams("InitNpcShipTeam"); }

    private static void ApplyConst(string why)
    {
        try
        {
            var dict = TemplateManager._gameConst;
            if (dict == null || dict.Count == 0) return;
            string line;
            if (!dict.ContainsKey(Rules.BuyListKey)) line = $"{Rules.BuyListKey} missing (unchanged)";
            else
            {
                var row = dict[Rules.BuyListKey];
                var now = Rules.BuyListWithout(row.value);
                if (now == null) line = $"{Rules.BuyListKey} has no {Rules.BannedBuy} ({(row.value ?? "").Split('|').Length} ships, unchanged)";
                else { int before = row.value!.Split('|').Length; row.value = now; line = $"{Rules.BuyListKey}: removed {Rules.BannedBuy} ({before} -> {now.Split('|').Length} ships)"; }
            }
            if (loggedConst != line) { loggedConst = line; log.Msg($"GameConst ({why}): {line}"); }
        }
        catch (Exception ex) { Fail("game const", ex); }
    }

    private static void ApplyTeams(string why)
    {
        try
        {
            var dict = TemplateManager._npcShipTeam;
            if (dict == null || dict.Count == 0) return;
            var parts = new List<string>();
            foreach (var t in Rules.Teams)
            {
                if (!dict.ContainsKey(t.Tid)) { parts.Add($"{t.Tid} missing"); continue; }
                var row = dict[t.Tid];
                var arr = row.otherShips;
                var others = new List<int>();
                if (arr != null) for (int i = 0; i < arr.Length; i++) others.Add(arr[i]);
                switch (Rules.Check(t, row.flagShip, others))
                {
                    case Rules.Result.AlreadyChanged: parts.Add($"{t.Tid} ok"); break;
                    case Rules.Result.Differs:
                        parts.Add($"{t.Tid} differs ({row.flagShip}|{string.Join(",", others)}), unchanged"); break;
                    default:
                        var na = new Il2CppStructArray<int>(t.NewOthers.Length);
                        for (int i = 0; i < t.NewOthers.Length; i++) na[i] = t.NewOthers[i];
                        row.otherShips = na;
                        row.flagShip = t.NewFlag;
                        parts.Add($"{t.Tid} {t.Flag}|{string.Join(",", t.Others)} -> {t.NewFlag}|{string.Join(",", t.NewOthers)}");
                        break;
                }
            }
            var line = string.Join("; ", parts);
            if (loggedTeams != line) { loggedTeams = line; log.Msg($"NpcShipTeam ({why}): {line}"); }
        }
        catch (Exception ex) { Fail("npc ship team", ex); }
    }

    private static void Fail(string what, Exception ex)
    {
        var key = what + ":" + ex.Message;
        if (lastError == key) return;
        lastError = key; log.Error($"[{what}] {ex}");
    }

    public override void OnDeinitializeMelon() { enabled = false; hookSet?.RemoveAll(); }
}

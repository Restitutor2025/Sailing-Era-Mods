using System.Runtime.CompilerServices;
using MelonLoader;
using Restitutor.Core;
using Il2CppClient.PlayerStore;
using Il2CppGyyx.Template;

[assembly: MelonInfo(typeof(Restitutor.RebalanceShipyard.EntryPoint), "Restitutor Rebalance Shipyard", "0.1.0", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.RebalanceShipyard;

// Shipyard "Buy" list = lowest-grade ships only (Ship.Technical 0: cog / sloop / Chinese sloop by culture).
// One postfix on WorldPortBuildShipData.GetBuyShipList: the only source of ships for the natural refill
// (new-game first stock and every production line). Saves are not touched; intended for new saves.
// See handoff/SHIPYARD_REBALANCE.md and docs/mods/rebalance-shipyard/.
public sealed class EntryPoint : MelonMod
{
    internal const string Version = "0.1.0";
    private const int DetailLines = 60;   // per-call log lines before going quiet (then counters only)
    private static MelonLogger.Instance log = null!;
    private static bool enabled;
    private static string? lastError;
    private static int filtered, fallbacks, detailed;

    private HookSet? hookSet;

    public override void OnInitializeMelon()
    {
        log = LoggerInstance;
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { enabled = false; log.Error("Restitutor.Core.dll is missing from UserLibs; Rebalance Shipyard stays disabled."); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install()
    {
        if (!CoreInfo.Require(log, "0.1.0")) return;
        hookSet = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (!hookSet.InstallAll(log, "Rebalance Shipyard install", () =>
        {
            hookSet.Hook(typeof(WorldPortBuildShipData), "GetBuyShipList", postfix: nameof(AfterBuyShipList));
            enabled = true;
        })) { enabled = false; return; }
        log.Msg($"Rebalance Shipyard {Version} loaded (Restitutor.Core {CoreInfo.Version}); 1 hook. Buy stock keeps Ship.Technical <= {Rules.MaxTechnical} only (new saves).");
    }

    // Postfix only: the original always runs; its list is edited in place. Exceptions never reach the game.
    private static void AfterBuyShipList(WorldPortBuildShipData __instance, Il2CppSystem.Collections.Generic.List<int> __result)
    {
        if (!enabled || __result == null) return;
        try
        {
            int n = __result.Count;
            var ids = new List<int>(n);
            for (int i = 0; i < n; i++) ids.Add(__result[i]);
            var keep = Rules.Filter(ids, Technical);
            if (keep == null)
            {
                if (ids.Count > 0 && ids.TrueForAll(id => Technical(id) is int t && t > Rules.MaxTechnical))
                {
                    fallbacks++;
                    log.Warning($"port {__instance.PortID}: no ship with Technical <= {Rules.MaxTechnical} among [{string.Join(",", ids)}]; original list kept (fallback #{fallbacks}).");
                }
                return;
            }
            __result.Clear();
            foreach (var id in keep) __result.Add(id);
            filtered++;
            if (detailed < DetailLines)
            {
                detailed++;
                log.Msg($"port {__instance.PortID}: buy candidates [{string.Join(",", ids)}] -> [{string.Join(",", keep)}] (#{filtered}{(detailed == DetailLines ? "; further calls not logged" : "")})");
            }
        }
        catch (Exception ex) { Fail("GetBuyShipList postfix", ex); }
    }

    private static int? Technical(int shipId)
    {
        var s = TemplateManager.GetShip(shipId);
        return s == null ? null : (int?)s.technical;
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
        log.Msg($"Rebalance Shipyard: filtered {filtered} candidate lists, {fallbacks} fallbacks this run.");
        hookSet?.RemoveAll();
    }
}

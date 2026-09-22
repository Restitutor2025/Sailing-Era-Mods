using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using MelonLoader;
using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.PlayerStore.Property;
using Il2CppClient.UILogic.UIMap;
using Il2CppClient.UILogic.UIWharf;
using Il2CppClient.UILogic.UISailReady;

using Il2CppFairyGUI;

using SceneManager = Il2CppCore.SceneSystem.SceneManager;
using Restitutor.Cheats.Interface;
[assembly: MelonInfo(typeof(Restitutor.Cheats.Contribution.EntryPoint), "Restitutor Cheats Contribution","1.1.4", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
[assembly: MelonAdditionalDependencies("Restitutor_Cheats_Interface")]
namespace Restitutor.Cheats.Contribution;
public sealed class EntryPoint : MelonMod {
    internal static PlayerData? Player => Host.Player;
    internal static bool Enabled => loaded && Host.Enabled;
    private static bool loaded;
    internal static MelonLogger.Instance Log = null!;
    internal static string Unavailable = "도시에서만 사용 가능";
    internal static readonly ContributionPanel Panel = new();
    // 1.1.4: no native hooks at all. 1.1.2 prefixed BaseObjectData.GetPointProperty/GetProperty for the whole
    // session; 1.1.3 patched them only during Apply, but Il2CppInterop keeps the native detour after UnpatchSelf, so
    // after one Apply every call still paid a managed transition (monthly market refresh: ~65,000 calls, 19 ms -> 300 ms).
    // Now the native UpdateInfluence runs unmodified and the delta is chosen so the result lands on the target
    // (WorldPortHoldDB.UpdateInfluence 0xB78FA0: gain = trunc((1 + rate21/100) * delta) after an optional culture
    // bonus on positive deltas (point 133), clamped to 0..1000). Remaining differences are corrected with more calls.
    public override void OnInitializeMelon() {
        Log=LoggerInstance;
        loaded=true; Host.Register(Panel);
        Log.Msg("Cheats Contribution 1.1.4 loaded (panel writes only on change; no native hooks, native UpdateInfluence with compensated delta).");
    }
    private const int MaxCalls = 8;
    private static int Bracket(int v) => v / 100; // contribution unlock thresholds are multiples of 100
    // Native model (0xB78FA0): positive delta may first become RoundToInt(culture% * delta * 0.01f) when the player
    // stays in a matching culture (point 133); then gain = (int)(((float)rate21 / 100f + 1f) * delta); result clamped 0..1000.
    private sealed class Model {
        internal float Rate = 1f; internal int? Culture; internal bool CultureKnown;
        internal int Plain(int d) => (int)(Rate * d);
        internal int WithCulture(int d) => (int)(Rate * (float)Math.Round((float)(Culture!.Value * d) * 0.01f, MidpointRounding.ToEven));
        internal IEnumerable<int> Results(int cur, int d) {
            if (d > 0 && Culture.HasValue && Culture.Value != 100) {
                if (!CultureKnown || CultureApplies) yield return Math.Clamp(cur + WithCulture(d), 0, 1000);
                if (!CultureKnown || !CultureApplies) yield return Math.Clamp(cur + Plain(d), 0, 1000);
            } else yield return Math.Clamp(cur + Plain(d), 0, 1000);
        }
        internal bool CultureApplies;
    }
    // Picks the delta whose every possible result is closest to the target without moving into a different
    // 100-bracket than the start or the target (crossing a threshold and coming back would grant or revoke unlocks
    // the player did not ask for). Deltas with no effect are skipped.
    private static int Choose(Model m, int cur, int target) {
        int best = Math.Sign(target - cur), bestDist = int.MaxValue;
        for (int k = 1; k <= 1200 && bestDist > 0; k++)
            foreach (int d in new[] { k, -k }) {
                int dist = 0; bool allowed = true, moves = false;
                foreach (int v in m.Results(cur, d)) {
                    if (v != cur) moves = true;
                    bool between = v >= Math.Min(cur, target) && v <= Math.Max(cur, target);
                    bool away = (v - cur) * (target - cur) < 0;
                    if (!(between || Bracket(v) == Bracket(target) || (away && Bracket(v) == Bracket(cur)))) { allowed = false; break; }
                    dist = Math.Max(dist, Math.Abs(target - v));
                }
                if (!allowed || !moves) continue;
                if (dist < bestDist) { bestDist = dist; best = d; }
            }
        return best;
    }
    private static Model ReadModel() {
        var m = new Model();
        try { var r = Player!.PlayerCommander.CommanderData.GetProperty(21); if (r != null) m.Rate = (float)r.PropertyValue / 100f + 1f; } catch { }
        try { var c = Player!.PlayerCommander.CommanderData.GetPointProperty(133); if (c != null) m.Culture = c.PropertyValue; } catch { }
        return m;
    }
    internal static void ResetState() { }
    internal static bool MapOrRouteOpen() {
        var opened = UIManager.Instance?._alreadyOpenedUICtrls;
        if (opened == null) return true; // No usable UI state yet.
        foreach (var entry in opened) {
            var ctrl = entry.Value;
            if (ctrl == null) continue;
            if (ctrl.TryCast<UIMapCtrl>() == null && ctrl.TryCast<UIMapLineCtrl>() == null &&
                ctrl.TryCast<UISailLineCtrl>() == null && ctrl.TryCast<UISailReadyCtrl>() == null) continue;
            var view=ctrl.DisplayView;
            if (view?._state == null) continue;
            GObject? content=view.TryCast<UIMapView>()?.UIContent;
            content ??= view.TryCast<UIMapLineView>()?.UIContent;
            content ??= view.TryCast<UISailLineView>()?.UIContent;
            content ??= view.TryCast<UISailReadyView>()?.UIContent;
            if (UiPresence.IsDisplayed(view.IsOpen(), content)) return true;
        }
        return false;
    }
    internal static bool IsInCity() {
        if (!Enabled || Player == null || PlayerDataManager.Instance?.Data?.Pointer != Player.Pointer) return false;
        var scene = SceneManager.Instance;
        if (scene == null || !scene.IsSceneEntered || scene.IsInLoadingOrStarting ||
            !scene.IsInHarborScene || Player.PlayerPort?.IsStayInPort != true) return false;
        return true;
    }
    internal static int? Resolve() {
        Unavailable = "도시에서만 사용 가능";
        if(!IsInCity())return null;
        if (MapOrRouteOpen()) { Unavailable = "지도·항로도에서는 사용 불가"; return null; }
        var id = Player!.PlayerPort.StayInPortId;
        return Player.WorldPort?.GetPortData(id) != null ? id : null;
    }
    internal static void Apply(int? shown, string input) {
        if (!Enabled || Player == null) return;
        try {
            int? id = Resolve();
            if (!id.HasValue || id != shown) { Panel.Message("현재 도시가 변경되었거나 치트 사용 불가 상태입니다."); return; }
            if (!Rules.TryTarget(input, out int target)) { Panel.Message("0~1000 정수를 입력하세요."); return; }
            var port = Player.WorldPort.GetPortData(id.Value);
            if (port == null) throw new InvalidOperationException("Selected city data missing");
            int before = port.Influence;
            if (before == target) { Panel.Message("현재값과 같습니다. 호출 없음."); return; }
            if (before < 0 || before > 1000) throw new InvalidOperationException("Unexpected contribution range");
            var model = ReadModel(); int calls = 0;
            while (port.Influence != target && calls < MaxCalls) {
                int cur = port.Influence;
                int d = target == 1000 ? 2000 : target == 0 ? -2000 : Choose(model, cur, target);
                Player.WorldPort.UpdateInfluence(id.Value, d); calls++;
                int now = port.Influence;
                if (d > 0 && !model.CultureKnown && model.Culture.HasValue && model.Culture.Value != 100 && now < 1000) {
                    int with = Math.Clamp(cur + model.WithCulture(d), 0, 1000), plain = Math.Clamp(cur + model.Plain(d), 0, 1000);
                    if (with != plain && (now == with || now == plain)) { model.CultureKnown = true; model.CultureApplies = now == with; }
                }
            }
            Log.Msg($"UpdateInfluence calls={calls} (native, unhooked; rate x{model.Rate:0.###}, culture {(model.Culture?.ToString() ?? "-")}{(model.CultureKnown ? (model.CultureApplies ? " applied" : " not applied") : "")})");
            int after = port.Influence;
            Log.Msg($"Cheat city={id} before={before} target={target} actual={after}");
            if (after != target) { Log.Warning($"Target not reachable exactly: target={target}, actual={after} after {MaxCalls} calls"); Panel.Message($"보정 때문에 정확히 못 맞춤: {before} → {after} (목표 {target})"); return; }
            Panel.Message($"적용 완료: {before} → {after}");
        } catch (Exception ex) { Log.Error(ex.ToString()); Panel.Message("적용 오류. 로그와 실제 수치를 확인하세요."); }
    }
    public override void OnDeinitializeMelon() { loaded=false; Host.Unregister(Panel); ResetState(); }
}


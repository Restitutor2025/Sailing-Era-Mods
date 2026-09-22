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
[assembly: MelonInfo(typeof(Restitutor.Cheats.Contribution.EntryPoint), "Restitutor Cheats Contribution","1.1.3", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
[assembly: MelonAdditionalDependencies("Restitutor_Cheats_Interface")]
namespace Restitutor.Cheats.Contribution;
public sealed class EntryPoint : MelonMod {
    internal static PlayerData? Player => Host.Player;
    internal static bool Enabled => loaded && Host.Enabled;
    private static bool loaded;
    internal static MelonLogger.Instance Log = null!;
    internal static string Unavailable = "도시에서만 사용 가능";
    private static PointProperty? neutral;
    internal static readonly ContributionPanel Panel = new();
    // 1.1.3: the two modifier hooks exist only while Apply runs UpdateInfluence. Before this, they were
    // installed at load and every native GetPointProperty/GetProperty call in the whole game paid a managed
    // transition (the monthly market refresh makes ~65,000 such calls in one frame: 19 ms native, ~170 ms hooked).
    private static HarmonyLib.Harmony? harmony;
    private static bool keepHooks; // set if an unpatch ever fails: stay patched rather than risk a half-state
    public override void OnInitializeMelon() {
        Log=LoggerInstance; harmony=HarmonyInstance;
        loaded=true; Host.Register(Panel);
        Log.Msg("Cheats Contribution 1.1.3 loaded (panel writes only on change; GetPointProperty/GetProperty hooked only during Apply).");
    }
    private static bool hooked;
    private static void InstallHooks() {
        if (hooked) return;
        try {
            Host.Hook(harmony!,typeof(EntryPoint),typeof(BaseObjectData),"GetPointProperty",nameof(Point));
            Host.Hook(harmony!,typeof(EntryPoint),typeof(BaseObjectData),"GetProperty",nameof(Rate));
            hooked=true;
        } catch { try { harmony!.UnpatchSelf(); } catch (Exception un) { keepHooks=true; Log.Error("Unpatch after failed install: " + un); } throw; }
    }
    private static void RemoveHooks() {
        if (!hooked || keepHooks) return;
        try { harmony!.UnpatchSelf(); hooked=false; }
        catch (Exception ex) { keepHooks=true; Log.Error("Modifier hooks could not be removed; they stay installed for this session: " + ex); }
    }
    internal static void ResetState() { ModifierScope.Current?.Dispose(); neutral=null; }
    // One-shot, matching commander only. Consumed before native update emits events.
    private static bool Point(BaseObjectData __instance, int __0, ref PointProperty? __result) {
        if (ModifierScope.Current?.Take(__instance.Pointer, __0, true) != true) return true;
        __result = null; return false;
    }
    private static bool Rate(BaseObjectData __instance, int __0, ref BaseProperty? __result) {
        if (ModifierScope.Current?.Take(__instance.Pointer, __0, false) != true) return true;
        __result = neutral; return false;
    }
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
            neutral ??= new PointProperty(21, 0);
            if (neutral.PropertyValue != 0) throw new InvalidOperationException("Neutral property validation failed");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            InstallHooks();
            try {
                using (var scope = new ModifierScope(Player.PlayerCommander.CommanderData.Pointer)) {
                    Player.WorldPort.UpdateInfluence(id.Value, target - before);
                    if (!scope.Complete) throw new InvalidOperationException("Native modifier interception was not observed");
                }
            } finally { RemoveHooks(); }
            Log.Msg($"Apply hooks: installed+removed in {sw.Elapsed.TotalMilliseconds:0} ms (still installed={hooked})");
            int after = port.Influence;
            Log.Msg($"Cheat city={id} before={before} target={target} actual={after}");
            if (after != target) throw new InvalidOperationException($"Target mismatch: target={target}, actual={after}; no retry");
            Panel.Message($"적용 완료: {before} → {after}");
        } catch (Exception ex) { Log.Error(ex.ToString()); Panel.Message("적용 오류. 로그와 실제 수치를 확인하세요."); }
    }
    public override void OnDeinitializeMelon() { loaded=false; if (hooked) { try { HarmonyInstance.UnpatchSelf(); } catch { } } Host.Unregister(Panel); ResetState(); }
}


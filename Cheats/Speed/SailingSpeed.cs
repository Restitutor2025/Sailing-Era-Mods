using Il2CppClient.Const;
using Il2CppClient.Manager;
using Il2CppClient.WorldLogic.Entity.Component.Boat;
using Il2CppClient.WorldLogic.Entity.Data;
using Il2CppClient.WorldLogic.Scenes;
using Il2CppClient.WorldLogic.Scenes.SceneState;
using SceneManager = Il2CppCore.SceneSystem.SceneManager;

namespace Restitutor.Cheats.Speed;
internal static class SailingSpeed {
    internal static int Multiplier { get; private set; } = 1;
    private static string lastError = "";
    private static string lastGate = "";
    internal static string Unavailable { get; private set; } = "항해 준비 중";
    internal static void Reset() { Multiplier = 1; RestoreApplied(); lastError = lastGate = ""; Unavailable = "항해 준비 중"; }
    internal static void Update() {
        // Map/pause keep selection available. Ending the sailing state (including battle and
        // EnterPort) discards the choice even while the ocean scene still exists.
        if (!VoyageActive()) Multiplier = 1;
        bool available=CanUse();
        string gate=available ? "활성" : Unavailable;
        if (gate != lastGate) { lastGate=gate; EntryPoint.Log.Msg($"Sailing gate: {gate}; selected=X{Multiplier}"); }
        Reconcile();
    }
    // 1.2.0: no hook on BoatEntityOceanDriver.FixedUpdate (it ran for all 40-67 boats, 2,000-2,900 calls/s).
    // Once per frame (this Update is called from the panel's Refresh) the player's flagship driver gets
    // forwardPowerFactorByEscape = original x multiplier; it is restored when the choice or the gate ends, the
    // flagship changes, or the cheat is reset. The game only writes this field on events (ctor 1.0, escape 1.5),
    // and a value the game wrote is kept as the new original. Pause needs no gate: the native speed product
    // includes GameModuleSpeedScale, which is 0 while paused.
    private static BoatEntityOceanDriver? applied;
    private static SpeedFrame frame;
    private static BoatEntityOceanDriver? Flagship() {
        var ocean = SceneManager.Instance?.CurScene()?.TryCast<OceanScene>();
        var driver = ocean?.FocusBoatReference?.LeaderDirectionMove;
        var boat = driver?.boatData; var team = boat?.Team;
        if (driver == null || boat == null || team == null || boat.boatState != EBoatState.DependOnTeam ||
            !team.isPlayer || team.State != EBoatTeamState.Free || team.Pointer != ocean?._focusTeam?.Pointer) return null;
        return driver;
    }
    private static void Reconcile() {
        try {
            var target = Multiplier > 1 && VoyageActive() ? Flagship() : null;
            if (applied != null && (target == null || applied.Pointer != target.Pointer)) RestoreApplied();
            if (target == null) return;
            float current = target.forwardPowerFactorByEscape;
            if (applied != null && frame.Changed && current == frame.Applied && frame.Applied == frame.Original * Multiplier) return;
            // New driver, a new multiplier, or the game wrote its own value (kept as the new original).
            float original = applied != null && frame.Changed && current == frame.Applied ? frame.Original : current;
            var next = new SpeedFrame(original, Multiplier);
            if (!next.Changed) { if (applied != null) RestoreApplied(); return; }
            target.forwardPowerFactorByEscape = next.Applied; frame = next; applied = target;
        } catch (Exception ex) { Fail(ex); }
    }
    private static void RestoreApplied() {
        var d = applied; applied = null;
        if (d == null) return;
        try { d.forwardPowerFactorByEscape = frame.Restore(d.forwardPowerFactorByEscape); } catch { } // destroyed with its scene: nothing to restore
        frame = default;
    }
    private static bool Deny(string reason) { Unavailable=reason; return false; }
    private static bool VoyageActive() {
        if (!EntryPoint.Enabled) return Deny("모드 비활성");
        if (EntryPoint.Player == null || PlayerDataManager.Instance?.Data?.Pointer != EntryPoint.Player.Pointer)
            return Deny("플레이어 준비 중");
        var scene = SceneManager.Instance;
        if (scene == null || !scene.IsSceneEntered || scene.IsInLoadingOrStarting) return Deny("장면 전환 중");
        if (!scene.IsInOceanScene) return Deny("항해 장면 아님");
        if (EntryPoint.Player.PlayerPort == null) return Deny("항구 정보 준비 중");
        if (EntryPoint.Player.PlayerPort.IsStayInPort) return Deny("도시 체류 중");
        var ocean = scene.CurScene()?.TryCast<OceanScene>();
        if (ocean == null) return Deny("항해 장면 준비 중");
        if (ocean.IsInBattle) return Deny("전투 중");
        if (ocean.SceneStateType != SceneStateType.Sailing) return Deny("일반 항해 상태 아님");
        if (ocean._focusTeam?.isPlayer != true) return Deny("플레이어 함대 준비 중");
        if (ocean._focusTeam.State != EBoatTeamState.Free) return Deny("입항·함대 전환 중");
        return true;
    }
    internal static bool CanUse() {
        if (!VoyageActive()) return false;
        Unavailable=""; return true;
    }
    internal static void Select(int value) {
        try { if (value >= 1 && value <= 5 && CanUse()) Multiplier = value; }
        catch (Exception ex) { Fail(ex); }
    }
    private static void Fail(Exception ex) {
        Multiplier = 1; RestoreApplied();
        if (lastError == ex.Message) return;
        lastError = ex.Message;
        EntryPoint.Log.Error("Sailing movement cheat suspended: " + ex);
    }
}

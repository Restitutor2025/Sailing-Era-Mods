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
    internal static void Reset() { Multiplier = 1; lastError = lastGate = ""; Unavailable = "항해 준비 중"; }
    internal static void Update() {
        // Map/pause keep selection available. Ending the sailing state (including battle and
        // EnterPort) discards the choice even while the ocean scene still exists.
        if (!VoyageActive()) Multiplier = 1;
        bool available=CanUse();
        string gate=available ? "활성" : Unavailable;
        if (gate != lastGate) { lastGate=gate; EntryPoint.Log.Msg($"Sailing gate: {gate}; selected=X{Multiplier}"); }
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
    internal static void Before(BoatEntityOceanDriver driver, out SpeedFrame state) {
        state = default;
        try {
            if (Multiplier == 1) return;
            if (!VoyageActive()) { Multiplier=1; return; }
            // UI selection remains available in an ocean Tab menu, but never
            // apply movement while the game is paused. No map-UI dependency:
            // UIMapCtrl is also used for the sailing minimap render texture.
            if (GameManager.IsGamePaused) return;
            var boat = driver.boatData;
            var team = boat?.Team;
            var ocean = SceneManager.Instance.CurScene()?.TryCast<OceanScene>();
            if (boat == null || boat.boatState != EBoatState.DependOnTeam || team == null ||
                !team.isPlayer || team.State != EBoatTeamState.Free || team.Pointer != ocean?._focusTeam?.Pointer) return;
            state = new SpeedFrame(driver.forwardPowerFactorByEscape, Multiplier);
            if (state.Changed) driver.forwardPowerFactorByEscape = state.Applied;
        } catch (Exception ex) { Fail(ex); }
    }
    internal static void After(BoatEntityOceanDriver driver, SpeedFrame state) {
        if (!state.Changed) return;
        try { driver.forwardPowerFactorByEscape = state.Restore(driver.forwardPowerFactorByEscape); }
        catch (Exception ex) { Fail(ex); }
        // Harmony finalizer returns void: original exceptions are never suppressed.
    }
    private static void Fail(Exception ex) {
        Multiplier = 1;
        if (lastError == ex.Message) return;
        lastError = ex.Message;
        EntryPoint.Log.Error("Sailing movement cheat suspended: " + ex);
    }
}

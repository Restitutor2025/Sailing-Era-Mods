using Il2CppClient.Manager;
using Il2CppClient.WorldLogic.Scenes;
using Il2CppClient.WorldLogic.Scenes.SceneState;
using SceneManager=Il2CppCore.SceneSystem.SceneManager;
namespace Restitutor.Cheats.Interface;

internal static class PlayLocation {
    // 1.6.1: true only while the loaded save is being played: scene entered, not loading, and a city, sea or
    // land scene (false on the title/entry screen and in pure story scenes).
    // 1.6.2: Host.SyncSession() runs first in the same OnUpdate and sets Player to the current
    // PlayerDataManager.Data, so Player is that object here; Data is read once per frame (in SyncSession).
    internal static bool InGame() {
        var player=Host.Player;
        if(player==null) return false;
        var scene=SceneManager.Instance;
        return scene!=null && scene.IsSceneEntered && !scene.IsInLoadingOrStarting && scene.IsInHarborOceanOrLand;
    }
    // 0 = outside a ready city/voyage; -1 = sailing; positive = current city.
    internal static int Current() {
        var player=Host.Player;
        var scene=SceneManager.Instance;
        if(player==null ||
            scene==null || !scene.IsSceneEntered || scene.IsInLoadingOrStarting)return 0;
        if(scene.IsInHarborScene && player.PlayerPort?.IsStayInPort==true)
            return player.PlayerPort.StayInPortId;
        if(scene.IsInOceanScene && player.PlayerPort?.IsStayInPort==false) {
            var ocean=scene.CurScene()?.TryCast<OceanScene>();
            if(ocean!=null && !ocean.IsInBattle && ocean.SceneStateType==SceneStateType.Sailing)return -1;
        }
        return 0;
    }
}

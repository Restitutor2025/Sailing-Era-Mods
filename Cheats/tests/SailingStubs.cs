using Restitutor.Cheats.Contribution;
// Managed test doubles. These do not load Unity, IL2CPP, the game or Steam.
namespace Il2CppClient.Const { enum EBoatTeamState { Free, EnterPort, Battle, Failed } }
namespace Il2CppClient.WorldLogic.Scenes.SceneState { enum SceneStateType { Default, Sailing, OceanBattle, MeleeBattle, GameOver } }
namespace Il2CppClient.WorldLogic.Scenes {
    class OceanScene {
        public SceneState.SceneStateType SceneStateType = SceneState.SceneStateType.Sailing;
        public bool IsInBattle;
        public Team? _focusTeam = new();
        public T? TryCast<T>() where T:class => this as T;
    }
    class Team { public IntPtr Pointer = (IntPtr)99; public bool isPlayer = true; public Il2CppClient.Const.EBoatTeamState State; }
}
namespace Il2CppClient.WorldLogic.Entity.Data {
    enum EBoatState { DependOnTeam, Failed }
    class Boat { public EBoatState boatState; public Il2CppClient.WorldLogic.Scenes.Team? Team; }
}
namespace Il2CppClient.WorldLogic.Entity.Component.Boat {
    class BoatEntityOceanDriver {
        public Il2CppClient.WorldLogic.Entity.Data.Boat? boatData;
        public float forwardPowerFactorByEscape = 1;
    }
}
namespace Il2CppCore.SceneSystem {
    class SceneManager {
        public static SceneManager Instance = new();
        public bool IsSceneEntered = true, IsInLoadingOrStarting, IsInOceanScene = true;
        public Il2CppClient.WorldLogic.Scenes.OceanScene? Ocean = new();
        public Il2CppClient.WorldLogic.Scenes.OceanScene? CurScene() => Ocean;
    }
}
namespace Il2CppClient.Manager {
    class Player { public IntPtr Pointer = (IntPtr)7; public Port? PlayerPort = new(); }
    class Port { public bool IsStayInPort; }
    class PlayerDataManager { public static PlayerDataManager Instance = new(); public Player? Data; }
    static class GameManager { public static bool IsGamePaused; }
}
namespace Restitutor.Cheats.Speed {
    static class EntryPoint {
        public static bool Enabled = true, MapOpen;
        public static Il2CppFairyGUI.GObject? MapContent;
        public static Il2CppClient.Manager.Player? Player;
        public static Logger Log = new();
        public static bool MapOrRouteOpen() => MapOpen || UiPresence.IsDisplayed(true,MapContent);
    }
    class Logger { public void Error(string value) {} public void Msg(string value) {} }
}
namespace Il2CppFairyGUI {
    class GObject {
        public bool isDisposed, onStage=true, internalVisible=true, internalVisible2=true;
        public GObject? parent;
    }
}

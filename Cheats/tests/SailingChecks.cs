using Restitutor.Cheats.Contribution;
using Il2CppClient.Const;
using Il2CppClient.Manager;
using Il2CppClient.WorldLogic.Entity.Component.Boat;
using Il2CppClient.WorldLogic.Entity.Data;
using Il2CppClient.WorldLogic.Scenes;
using Il2CppClient.WorldLogic.Scenes.SceneState;
using Il2CppCore.SceneSystem;
namespace Restitutor.Cheats.Speed;
static class SailingChecks {
    internal static void Run(Action<bool> check) {
        var player = new Player(); EntryPoint.Player=player; PlayerDataManager.Instance.Data=player;
        var scene=SceneManager.Instance; var ocean=scene.Ocean!;
        var driver=new BoatEntityOceanDriver { boatData=new Boat { Team=ocean._focusTeam } };
        check(SailingSpeed.CanUse());
        foreach(int choice in Enumerable.Range(1,5)) {
            SailingSpeed.Select(choice); driver.forwardPowerFactorByEscape=.7f;
            SailingSpeed.Before(driver,out var frame);
            check(Math.Abs(driver.forwardPowerFactorByEscape-.7f*choice)<.00001f);
            SailingSpeed.After(driver,frame); check(driver.forwardPowerFactorByEscape==.7f);
        }
        SailingSpeed.Select(5); SailingSpeed.Select(0); SailingSpeed.Select(6); check(SailingSpeed.Multiplier==5);
        void Block(Action on, Action off, bool endsVoyage=true) {
            on(); check(!SailingSpeed.CanUse()); SailingSpeed.Select(2); check(SailingSpeed.Multiplier==5);
            SailingSpeed.Before(driver,out var frame); check(!frame.Changed);
            check(SailingSpeed.Multiplier==(endsVoyage ? 1 : 5));
            off(); check(SailingSpeed.CanUse()); SailingSpeed.Select(5);
        }
        Block(()=>EntryPoint.Enabled=false,()=>EntryPoint.Enabled=true);
        Block(()=>scene.IsSceneEntered=false,()=>scene.IsSceneEntered=true);
        Block(()=>scene.IsInLoadingOrStarting=true,()=>scene.IsInLoadingOrStarting=false);
        Block(()=>scene.IsInOceanScene=false,()=>scene.IsInOceanScene=true);
        Block(()=>player.PlayerPort!.IsStayInPort=true,()=>player.PlayerPort!.IsStayInPort=false);
        Block(()=>ocean.IsInBattle=true,()=>ocean.IsInBattle=false);
        Block(()=>ocean.SceneStateType=SceneStateType.OceanBattle,()=>ocean.SceneStateType=SceneStateType.Sailing);
        Block(()=>ocean._focusTeam!.State=EBoatTeamState.EnterPort,()=>ocean._focusTeam!.State=EBoatTeamState.Free);
        EntryPoint.MapOpen=true; GameManager.IsGamePaused=true;
        check(SailingSpeed.CanUse()); SailingSpeed.Select(4); check(SailingSpeed.Multiplier==4);
        SailingSpeed.Before(driver,out var paused); check(!paused.Changed);
        SailingSpeed.Update(); check(SailingSpeed.Multiplier==4);
        GameManager.IsGamePaused=false;
        // A minimap keeps a map object open after Tab closes; selection and
        // movement must resume even while that map predicate is still true.
        SailingSpeed.Before(driver,out var resumed); check(resumed.Changed);
        SailingSpeed.After(driver,resumed); check(driver.forwardPowerFactorByEscape==.7f);
        scene.IsInOceanScene=false; player.PlayerPort!.IsStayInPort=true;
        check(!SailingSpeed.CanUse()); SailingSpeed.Update(); check(SailingSpeed.Multiplier==1);
        SailingSpeed.Select(5); check(SailingSpeed.Multiplier==1);
        player.PlayerPort.IsStayInPort=false; scene.IsInOceanScene=true;
        EntryPoint.MapOpen=false; SailingSpeed.Select(5);
        Block(()=>PlayerDataManager.Instance.Data=new Player { Pointer=(IntPtr)8 },()=>PlayerDataManager.Instance.Data=player);
        foreach(var team in new Team?[]{null,new Team{Pointer=(IntPtr)100},new Team{isPlayer=false},new Team{State=EBoatTeamState.Battle}}) {
            driver.boatData!.Team=team; SailingSpeed.Before(driver,out var frame); check(!frame.Changed);
        }
        driver.boatData!.Team=ocean._focusTeam;
        driver.boatData.boatState=EBoatState.Failed; SailingSpeed.Before(driver,out var failed); check(!failed.Changed);
        driver.boatData.boatState=EBoatState.DependOnTeam;
        foreach(float value in new[]{0f,-1f,float.NaN,float.PositiveInfinity,float.MaxValue}) {
            driver.forwardPowerFactorByEscape=value; SailingSpeed.Before(driver,out var frame); check(!frame.Changed);
        }
        driver.forwardPowerFactorByEscape=1;
        SailingSpeed.Before(driver,out var outer);
        try { throw new InvalidOperationException("native failure stand-in"); }
        catch { SailingSpeed.After(driver,outer); }
        check(driver.forwardPowerFactorByEscape==1);
        SailingSpeed.Before(driver,out var changed); driver.forwardPowerFactorByEscape=.25f;
        SailingSpeed.After(driver,changed); check(driver.forwardPowerFactorByEscape==.25f);
        scene.IsInOceanScene=false; SailingSpeed.Update(); check(SailingSpeed.Multiplier==1);
        scene.IsInOceanScene=true; SailingSpeed.Select(4); SailingSpeed.Reset(); check(SailingSpeed.Multiplier==1);
    }
}

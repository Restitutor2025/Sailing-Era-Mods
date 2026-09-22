using Restitutor.Cheats.Contribution;
using Il2CppClient.Const;
using Il2CppClient.Manager;
using Il2CppClient.WorldLogic.Entity.Component.Boat;
using Il2CppClient.WorldLogic.Entity.Data;
using Il2CppClient.WorldLogic.Scenes;
using Il2CppClient.WorldLogic.Scenes.SceneState;
using Il2CppCore.SceneSystem;
namespace Restitutor.Cheats.Speed;
// 1.2.0: no FixedUpdate hook. SailingSpeed.Update (called once per frame by the panel) writes the player
// flagship driver's forwardPowerFactorByEscape = original x multiplier and restores it when the choice ends.
static class SailingChecks {
    internal static void Run(Action<bool> check) {
        var player = new Player(); EntryPoint.Player=player; PlayerDataManager.Instance.Data=player;
        var scene=SceneManager.Instance; var ocean=scene.Ocean!;
        var driver=new BoatEntityOceanDriver { boatData=new Boat { Team=ocean._focusTeam } };
        var npc=new BoatEntityOceanDriver { boatData=new Boat { Team=new Team{Pointer=(IntPtr)55,isPlayer=false} }, forwardPowerFactorByEscape=1.5f };
        ocean.FocusBoatReference!.LeaderDirectionMove=driver;
        bool Near(float a,float b)=>Math.Abs(a-b)<.00001f;
        check(SailingSpeed.CanUse());
        driver.forwardPowerFactorByEscape=.7f;
        foreach(int choice in Enumerable.Range(1,5)) {
            SailingSpeed.Select(choice); SailingSpeed.Update();
            check(Near(driver.forwardPowerFactorByEscape,.7f*choice)); check(npc.forwardPowerFactorByEscape==1.5f);
        }
        SailingSpeed.Select(1); SailingSpeed.Update(); check(driver.forwardPowerFactorByEscape==.7f);   // X1 restores
        SailingSpeed.Select(5); SailingSpeed.Select(0); SailingSpeed.Select(6); check(SailingSpeed.Multiplier==5);
        SailingSpeed.Update(); check(Near(driver.forwardPowerFactorByEscape,3.5f));
        void Block(Action on, Action off, bool endsVoyage=true) {
            on(); SailingSpeed.Update(); check(!SailingSpeed.CanUse()); SailingSpeed.Select(2); check(SailingSpeed.Multiplier==(endsVoyage?1:5));
            check(driver.forwardPowerFactorByEscape==.7f);                                                // gate closed: restored
            off(); check(SailingSpeed.CanUse()); SailingSpeed.Select(5); SailingSpeed.Update(); check(Near(driver.forwardPowerFactorByEscape,3.5f));
        }
        Block(()=>EntryPoint.Enabled=false,()=>EntryPoint.Enabled=true);
        Block(()=>scene.IsSceneEntered=false,()=>scene.IsSceneEntered=true);
        Block(()=>scene.IsInLoadingOrStarting=true,()=>scene.IsInLoadingOrStarting=false);
        Block(()=>scene.IsInOceanScene=false,()=>scene.IsInOceanScene=true);
        Block(()=>player.PlayerPort!.IsStayInPort=true,()=>player.PlayerPort!.IsStayInPort=false);
        Block(()=>ocean.IsInBattle=true,()=>ocean.IsInBattle=false);
        Block(()=>ocean.SceneStateType=SceneStateType.OceanBattle,()=>ocean.SceneStateType=SceneStateType.Sailing);
        Block(()=>ocean._focusTeam!.State=EBoatTeamState.EnterPort,()=>ocean._focusTeam!.State=EBoatTeamState.Free);
        Block(()=>PlayerDataManager.Instance.Data=new Player { Pointer=(IntPtr)8 },()=>PlayerDataManager.Instance.Data=player);
        // Pause and an open minimap keep the selection; the native speed product is 0 while paused.
        EntryPoint.MapOpen=true; GameManager.IsGamePaused=true;
        SailingSpeed.Select(4); SailingSpeed.Update(); check(SailingSpeed.Multiplier==4); check(Near(driver.forwardPowerFactorByEscape,2.8f));
        GameManager.IsGamePaused=false; EntryPoint.MapOpen=false;
        // Flagship changes: the old driver is restored, the new one gets the multiplier.
        var other=new BoatEntityOceanDriver { boatData=new Boat { Team=ocean._focusTeam }, forwardPowerFactorByEscape=1f };
        ocean.FocusBoatReference.LeaderDirectionMove=other; SailingSpeed.Update();
        check(driver.forwardPowerFactorByEscape==.7f); check(Near(other.forwardPowerFactorByEscape,4f));
        ocean.FocusBoatReference.LeaderDirectionMove=driver; SailingSpeed.Update();
        check(other.forwardPowerFactorByEscape==1f); check(Near(driver.forwardPowerFactorByEscape,2.8f));
        // Not the player's free focused team, or failed boat: nothing applied, previous value restored.
        foreach(var team in new Team?[]{null,new Team{Pointer=(IntPtr)100},new Team{isPlayer=false},new Team{State=EBoatTeamState.Battle}}) {
            driver.boatData!.Team=team; SailingSpeed.Update(); check(driver.forwardPowerFactorByEscape==.7f);
        }
        driver.boatData!.Team=ocean._focusTeam;
        driver.boatData.boatState=EBoatState.Failed; SailingSpeed.Update(); check(driver.forwardPowerFactorByEscape==.7f);
        driver.boatData.boatState=EBoatState.DependOnTeam;
        // Invalid originals are never scaled.
        foreach(float value in new[]{0f,-1f,float.NaN,float.PositiveInfinity,float.MaxValue}) {
            SailingSpeed.Select(1); SailingSpeed.Update(); driver.forwardPowerFactorByEscape=value; SailingSpeed.Select(4); SailingSpeed.Update();
            check(driver.forwardPowerFactorByEscape.Equals(value));
        }
        // The game writes its own value while applied (e.g. escape 1.5): kept as the new original, restored to it.
        SailingSpeed.Select(1); SailingSpeed.Update(); driver.forwardPowerFactorByEscape=1f; SailingSpeed.Select(3); SailingSpeed.Update();
        check(Near(driver.forwardPowerFactorByEscape,3f));
        driver.forwardPowerFactorByEscape=1.5f; SailingSpeed.Update(); check(Near(driver.forwardPowerFactorByEscape,4.5f));
        SailingSpeed.Select(1); SailingSpeed.Update(); check(driver.forwardPowerFactorByEscape==1.5f);
        // Changing the multiplier re-bases on the stored original, not on the scaled value.
        SailingSpeed.Select(5); SailingSpeed.Update(); SailingSpeed.Select(2); SailingSpeed.Update(); check(Near(driver.forwardPowerFactorByEscape,3f));
        // Leaving the ocean and Reset restore.
        scene.IsInOceanScene=false; SailingSpeed.Update(); check(SailingSpeed.Multiplier==1); check(driver.forwardPowerFactorByEscape==1.5f);
        scene.IsInOceanScene=true; SailingSpeed.Select(4); SailingSpeed.Update(); SailingSpeed.Reset();
        check(SailingSpeed.Multiplier==1); check(driver.forwardPowerFactorByEscape==1.5f); check(npc.forwardPowerFactorByEscape==1.5f);
    }
}

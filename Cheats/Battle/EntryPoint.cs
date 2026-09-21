using MelonLoader;
using Il2Cpp;
using Il2CppClient.WorldLogic.Scenes;
using Il2CppClient.WorldLogic.Scenes.SceneState;
using Il2CppClient.WorldLogic.Entity.Component.Boat.BoatAttack;
using Restitutor.Cheats.Interface;
[assembly: MelonInfo(typeof(Restitutor.Cheats.Battle.EntryPoint),"Restitutor Cheats Battle","1.0.1","Restitutor")]
[assembly: MelonGame("bolingo","SailingEra")]
[assembly: MelonAdditionalDependencies("Restitutor_Cheats_Interface")]
namespace Restitutor.Cheats.Battle;

public sealed class EntryPoint : MelonMod {
    internal static bool Loaded;
    internal static MelonLogger.Instance Log=null!;
    private static readonly BattlePanel panel=new();
    public override void OnInitializeMelon() {
        Log=LoggerInstance;
        try {
            Host.Hook(HarmonyInstance,typeof(EntryPoint),typeof(OceanScene),"ChangeInputState",nameof(ChangeState));
            Host.Hook(HarmonyInstance,typeof(EntryPoint),typeof(OceanScene),"OnExit",nameof(Reset));
            Host.Hook(HarmonyInstance,typeof(EntryPoint),typeof(MeleeBattleController),"StartEnterBattle",after:nameof(Bind));
            // ClientBattle.Tick is inlined here in this binary; hook the actual caller.
            Host.Hook(HarmonyInstance,typeof(EntryPoint),typeof(MeleeBattleController),"OnUpdate",nameof(BeforeMelee));
            Host.Hook(HarmonyInstance,typeof(EntryPoint),typeof(MeleeBattleController),"ExitBattle",nameof(EndMelee));
            Host.Hook(HarmonyInstance,typeof(EntryPoint),typeof(MeleeBattleController),"OnBattleEndedTipsClose",nameof(EndMelee));
            // AttackInfo.Init and Gun.Shoot are inlined in this path. DelayFire is a real call.
            Host.Hook(HarmonyInstance,typeof(EntryPoint),typeof(BoatEntityGun),"DelayFire",nameof(BeforeCannon));
            Loaded=true;Host.Register(panel);
            Log.Msg("Cheats Battle 1.0.1 loaded (no per-frame lookups outside the ocean scene): battle-only navigator stats and controlled-ship cannon hull damage; reset X1 at end.");
        } catch(Exception ex) { Loaded=false;HarmonyInstance.UnpatchSelf();BattleRuntime.Reset();Log.Error(ex.ToString()); }
    }
    public override void OnUpdate()=>BattleRuntime.Update();
    private static void ChangeState(SceneStateType __0) {
        if(__0!=SceneStateType.OceanBattle && __0!=SceneStateType.MeleeBattle)BattleRuntime.Reset();
    }
    private static void Reset()=>BattleRuntime.Reset();
    private static void Bind(MeleeBattleController __instance)=>BattleRuntime.Bind(__instance);
    private static void BeforeMelee(MeleeBattleController __instance)=>BattleRuntime.BeforeMelee(__instance);
    private static void EndMelee(MeleeBattleController __instance)=>BattleRuntime.EndMelee(__instance);
    private static void BeforeCannon(BoatEntityGun __instance,AttackInfo __4)=>BattleRuntime.Cannon(__instance,__4);
    public override void OnDeinitializeMelon() { Loaded=false;BattleRuntime.Reset();HarmonyInstance.UnpatchSelf();Host.Unregister(panel); }
}

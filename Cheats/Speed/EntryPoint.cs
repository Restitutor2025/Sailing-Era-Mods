using MelonLoader;
using Il2CppClient.PlayerStore;
using Il2CppClient.WorldLogic.Entity.Component.Boat;
using Restitutor.Cheats.Interface;
[assembly: MelonInfo(typeof(Restitutor.Cheats.Speed.EntryPoint), "Restitutor Cheats Speed","1.1.2", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
[assembly: MelonAdditionalDependencies("Restitutor_Cheats_Interface")]
namespace Restitutor.Cheats.Speed;
public sealed class EntryPoint : MelonMod {
    internal static PlayerData? Player=>Host.Player;
    internal static bool Enabled=>loaded && Host.Enabled;
    private static bool loaded;
    internal static MelonLogger.Instance Log=null!;
    private static readonly SpeedPanel panel=new();
    public override void OnInitializeMelon() {
        Log=LoggerInstance;
        try {
            Host.Hook(HarmonyInstance,typeof(EntryPoint),typeof(BoatEntityOceanDriver),"FixedUpdate",nameof(BeforeMovement),final:nameof(AfterMovement));
            loaded=true; Host.Register(panel); Log.Msg("Cheats Speed 1.1.2 loaded (panel redraws only on change).");
        } catch(Exception ex) { loaded=false; HarmonyInstance.UnpatchSelf(); Log.Error(ex.ToString()); }
    }
    private static void BeforeMovement(BoatEntityOceanDriver __instance,out SpeedFrame __state)=>SailingSpeed.Before(__instance,out __state);
    private static void AfterMovement(BoatEntityOceanDriver __instance,SpeedFrame __state)=>SailingSpeed.After(__instance,__state);
    public override void OnDeinitializeMelon() { loaded=false; HarmonyInstance.UnpatchSelf(); Host.Unregister(panel); SailingSpeed.Reset(); }
}

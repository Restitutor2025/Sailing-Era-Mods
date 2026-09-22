using MelonLoader;
using Il2CppClient.PlayerStore;
using Restitutor.Cheats.Interface;
[assembly: MelonInfo(typeof(Restitutor.Cheats.Speed.EntryPoint), "Restitutor Cheats Speed","1.2.0", "Restitutor")]
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
        // 1.2.0: no native hooks (1.1.x prefixed BoatEntityOceanDriver.FixedUpdate for every boat).
        loaded=true; Host.Register(panel); Log.Msg("Cheats Speed 1.2.0 loaded (panel redraws only on change; no native hooks, player flagship driver only).");
    }
    public override void OnDeinitializeMelon() { loaded=false; Host.Unregister(panel); SailingSpeed.Reset(); }
}

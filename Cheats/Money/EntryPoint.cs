using MelonLoader;
using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Restitutor.Cheats.Interface;
using SceneManager=Il2CppCore.SceneSystem.SceneManager;
[assembly: MelonInfo(typeof(Restitutor.Cheats.Money.EntryPoint),"Restitutor Cheats Money","1.0.1","Restitutor")]
[assembly: MelonGame("bolingo","SailingEra")]
[assembly: MelonAdditionalDependencies("Restitutor_Cheats_Interface")]
namespace Restitutor.Cheats.Money;
public sealed class EntryPoint:MelonMod {
    private static bool loaded,applying;
    private static MelonLogger.Instance log=null!;
    private static readonly MoneyPanel panel=new();
    public override void OnInitializeMelon() { log=LoggerInstance;Host.Register(panel);loaded=true;log.Msg("Cheats Money 1.0.1 loaded (panel writes only on change)."); }
    internal static PlayerCurrencyDB? Resolve() {
        var player=Host.Player;
        if(!loaded || !Host.Enabled || player==null || PlayerDataManager.Instance?.Data?.Pointer!=player.Pointer) return null;
        var scene=SceneManager.Instance;
        if(scene==null || !scene.IsSceneEntered || scene.IsInLoadingOrStarting) return null;
        var currency=player.PlayerCurrency;
        return currency?.FindCurrency(1)!=null ? currency : null;
    }
    internal static void Apply(IntPtr shownPlayer,string text) {
        if(applying) return;
        try {
            var currency=Resolve();
            if(currency==null || Host.Player?.Pointer!=shownPlayer) { panel.Message("데이터 준비 후 다시 적용하세요.");return; }
            if(!Rules.TryTarget(text,out int target)) { panel.Message(Rules.Hint);return; }
            applying=true;
            long before=currency.GetCoinCurrencyAmount();
            long after=Rules.Apply(target,()=>currency.GetCoinCurrencyAmount(),value=>currency.ModifyAmount(1,value));
            panel.Message($"적용 완료: {after:N0}");
            log.Msg($"Money before={before} target={target} actual={after}");
        } catch(Exception ex) { log.Error(ex.ToString());panel.Message("적용 오류 · 현재 금액과 로그 확인"); }
        finally { applying=false; }
    }
    public override void OnDeinitializeMelon() { loaded=false;Host.Unregister(panel); }
}

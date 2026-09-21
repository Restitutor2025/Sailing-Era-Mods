using MelonLoader;
using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIDrunkery;
using Il2CppClient.UILogic.UIHeroLevelUp;
using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
using SceneManager=Il2CppCore.SceneSystem.SceneManager;
[assembly: MelonInfo(typeof(Restitutor.Cheats.Exp.EntryPoint),"Restitutor Cheats Exp","1.0.1","Restitutor")]
[assembly: MelonGame("bolingo","SailingEra")]
[assembly: MelonAdditionalDependencies("Restitutor_Cheats_Interface")]
namespace Restitutor.Cheats.Exp;
public sealed class EntryPoint:MelonMod {
    private static bool loaded,applying;
    private static MelonLogger.Instance log=null!;
    private static readonly ExpPanel panel=new();
    public override void OnInitializeMelon() {
        log=LoggerInstance;
        if(!Host.Enabled) { log.Error("Cheats Interface disabled; Exp panel not registered.");return; }
        Host.Register(panel);loaded=true;
        log.Msg("Cheats Exp 1.0.1 loaded (panel writes only on change); no native hooks; tavern screen only; max=2147483647.");
    }
    private static bool Visible(bool open,GObject? content) {
        if(!open || content==null || content.isDisposed || !content.onStage) return false;
        for(int depth=0;content!=null;content=content.parent,depth++)
            if(depth>=128 || content.isDisposed || !content.internalVisible || !content.internalVisible2) return false;
        return true;
    }
    // Visible only while the tavern screen of the current city is shown.
    // Hidden while the level-up window is open (it keeps its own copy of the exp value).
    // No barmaid logic here: barmaid code belongs exclusively to Restitutor_Cheats_Bargirls.
    internal static bool InTavern() {
        var p=Host.Player;var scene=SceneManager.Instance;
        if(!loaded || !Host.Enabled || p==null || PlayerDataManager.Instance?.Data?.Pointer!=p.Pointer || scene==null || !scene.IsSceneEntered || scene.IsInLoadingOrStarting || !scene.IsInHarborScene || p.PlayerPort?.IsStayInPort!=true) return false;
        int city=p.PlayerPort.StayInPortId;
        var opened=UIManager.Instance?._alreadyOpenedUICtrls;
        if(opened==null) return false;
        bool tavern=false;
        foreach(var entry in opened) {
            var ctrl=entry.Value;
            var bar=ctrl?.TryCast<UIDrunkeryCtrl>();
            if(bar?.View?._state!=null && bar.Model?.HarborId==city && Visible(bar.View.IsOpen(),bar.View.UIContent)) tavern=true;
            var hero=ctrl?.TryCast<UIHeroLevelUpCtrl>();
            if(hero?.View?._state!=null && hero.View.IsOpen()) return false;
        }
        return tavern;
    }
    internal static PlayerCurrencyData? Resolve() {
        if(!InTavern()) return null;
        return Host.Player!.PlayerCurrency?.FindCurrency(Rules.CurrencyId);
    }
    internal static void Apply(IntPtr shownPlayer,int target) {
        if(applying) return;
        try {
            applying=true;
            var data=Resolve();var db=Host.Player?.PlayerCurrency;
            if(data==null || db==null || Host.Player?.Pointer!=shownPlayer) { panel.Message("대상이 바뀌었습니다. 다시 적용하세요.");return; }
            long before=data.Amount;
            long after=Rules.Apply(target,()=>db.FindCurrency(Rules.CurrencyId)?.Amount??-1,value=>db.ModifyAmount(Rules.CurrencyId,value));
            panel.Message(after==before?$"이미 {after:N0}입니다":$"적용 완료: {after:N0}");
            log.Msg($"Exp before={before} target={target} actual={after}");
        } catch(Exception ex) { log.Error(ex.ToString());panel.Message("적용 오류 · 현재 경험치와 로그 확인"); }
        finally { applying=false; }
    }
    public override void OnDeinitializeMelon() { loaded=false;Host.Unregister(panel); }
}

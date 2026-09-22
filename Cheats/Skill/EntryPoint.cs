using MelonLoader;
using Restitutor.Core;
using Il2CppClient.Manager;
using Il2CppClient.UILogic.UIDrunkery;
using Il2CppClient.UILogic.UIHeroLevelUp;
using Il2CppClient.PlayerStore;
using Il2CppFairyGUI;
using Restitutor.Cheats.Interface;
using SceneManager=Il2CppCore.SceneSystem.SceneManager;
[assembly: MelonInfo(typeof(Restitutor.Cheats.Skill.EntryPoint),"Restitutor Cheats Skill","1.1.1","Restitutor")]
[assembly: MelonGame("bolingo","SailingEra")]
[assembly: MelonAdditionalDependencies("Restitutor_Cheats_Interface")]
namespace Restitutor.Cheats.Skill;
public sealed class EntryPoint:MelonMod {
    private static bool loaded;
    private static int originalLogged=int.MinValue;
    private static MelonLogger.Instance log=null!;
    private static readonly SkillPanel panel=new();
    public override void OnInitializeMelon() {
        log=LoggerInstance;
        string state=Settings.Load();
        hooks=new HookSet(HarmonyInstance,typeof(EntryPoint));
        if(!hooks.InstallAll(log,"Cheats Skill install",() => {
            // Native getter reads the game const for "levels per skill point".
            // Callers (RVA): CanUpSkill, OnClickBtnLevelUp, OnClickBtnLevelUpFive,
            // UIHeroLevelUpView.GetNextPointsLevel, UIHeroLevelUpView.RefreshRoleData.
            if(!Host.Enabled) throw new InvalidOperationException("Cheats Interface disabled");
            Hook(typeof(UIHeroLevelUpModel),"get_LevelGetSkill",after:nameof(Interval));
            // Multi-level (10-level) button: the original grants at most 1 point per click
            // (IsSpecialLevelUp is a bool). Grant the remaining multiples after the original point.
            Hook(typeof(UIHeroLevelUpCtrl),"OnClickBtnLevelUp",nameof(ClearPending));
            Hook(typeof(UIHeroLevelUpCtrl),"OnClickBtnLevelUpFive",nameof(BeforeMulti),nameof(AfterMulti));
            Hook(typeof(UIHeroLevelUpCtrl),"ChangeRewardSkillState",nameof(RewardBefore),nameof(GrantExtra));
            Host.Register(panel);loaded=true;
        })) return;
        log.Msg($"Cheats Skill 1.1.1 loaded (Restitutor.Core {CoreInfo.Version}); 4 patched methods; interval setting: {state}.");
    }
    private static void Interval(ref int __result) {
        // 1.0.3: Interface O-off returns the game's own value; the saved setting is kept for O-on.
        if(!loaded || !Host.CheatsOn) return;
        if(originalLogged!=__result) { originalLogged=__result; log.Msg($"Original LevelGetSkill={__result}; applied={Settings.Interval}"); }
        __result=Settings.Interval;
    }
    // Own lookup: declared members (public or not), parameterless, name must be unique (as 1.0.4).
    private static HookSet? hooks;
    private void Hook(Type target,string method,string? prefix=null,string? after=null)
        => hooks!.Hook(target,method,prefix:prefix,postfix:after,args:Type.EmptyTypes);
    private static bool rewardWasSet;
    private static void RewardBefore(UIHeroLevelUpCtrl __instance) {
        try { rewardWasSet=__instance.Model?.IsRewardSkill!=false; } catch { rewardWasSet=true; }
    }
    private sealed record Multi(IntPtr Ctrl,int Role,int From,int Count,int Interval,int Points);
    private static Multi? before,pending;
    private static void ClearPending() { before=null;pending=null; }
    // 1.1.0: Restitutor Rebalance Growth grants the 10-level extra points itself, using the effective
    // LevelGetSkill (this mod's override included). While it is loaded this mod only changes the interval,
    // otherwise both would add the extra points.
    // 1.1.1: "loaded" is not enough - Rebalance Growth disables its rules when the GameAssembly check fails,
    // and then nobody would grant the extra points. Read its public static ExtraGrantActive instead.
    private static System.Reflection.PropertyInfo? growthFlag;
    private static bool growthLooked;
    private static bool GrowthLoaded {
        get {
            if(!growthLooked) {
                growthLooked=true;
                var m=MelonBase.RegisteredMelons.FirstOrDefault(x=>x.Info.Name=="Restitutor Rebalance Growth");
                growthFlag=m?.MelonAssembly?.Assembly?.GetType("Restitutor.RebalanceGrowth.EntryPoint")?.GetProperty("ExtraGrantActive");
            }
            try { return growthFlag?.GetValue(null) is true; } catch { return false; }
        }
    }
    private static void BeforeMulti(UIHeroLevelUpCtrl __instance) {
        before=pending=null;
        if(!loaded || !Host.CheatsOn || GrowthLoaded) return;
        try {
            var model=__instance.Model;var role=model?.CurRole;
            if(model==null || role==null || model.BanTouch) return;
            int interval=Settings.Interval,from=role.Level,count=model.MaxContinueLevel;
            before=new(__instance.Pointer,role.RoleId,from,count,interval,Rules.PointsInRange(from,count,interval));
        } catch(Exception ex) { log.Error(ex.ToString()); }
    }
    private static void AfterMulti(UIHeroLevelUpCtrl __instance) {
        var b=before;before=null;
        if(b==null || !loaded) return;
        try {
            var model=__instance.Model;
            // Proceeded only if the original locked input and entered continue mode.
            if(model==null || __instance.Pointer!=b.Ctrl || !model.BanTouch || !model.IsClickContinueBtn || model.CurRole?.RoleId!=b.Role) return;
            if(model.IsSpecialLevelUp!=(b.Points>0)) { log.Warning($"Multi-level check mismatch: from={b.From} count={b.Count} interval={b.Interval} points={b.Points} special={model.IsSpecialLevelUp}; no extra"); return; }
            if(b.Points>1) pending=b;
        } catch(Exception ex) { log.Error(ex.ToString()); }
    }
    private static void GrantExtra(UIHeroLevelUpCtrl __instance) {
        var p=pending;pending=null;
        if(p==null || !loaded || __instance.Pointer!=p.Ctrl) return;
        try {
            var model=__instance.Model;
            // The original point was awarded by this call only if IsRewardSkill went false -> true.
            if(model==null || rewardWasSet || !model.IsRewardSkill || model.CurRole?.RoleId!=p.Role) { log.Warning("Original reward not observed; no extra point");return; }
            var db=PlayerDataManager.Instance?.Data?.PlayerRole;
            var data=db?.FindHoldRole(p.Role);
            if(db==null || data==null) { log.Warning("Role data not found; no extra point"); return; }
            int prev=data.SkillPoints,extra=p.Points-1;
            if(!db.UpdateRoleSkillPoint(p.Role,extra) || data.SkillPoints!=prev+extra) throw new InvalidOperationException($"UpdateRoleSkillPoint mismatch before={prev} extra={extra} after={data.SkillPoints}");
            log.Msg($"Multi-level role={p.Role} levels {p.From}..{p.From+p.Count-1} interval={p.Interval}: original +1, extra +{extra}; points {prev}->{data.SkillPoints}");
        } catch(Exception ex) { log.Error(ex.ToString()); }
    }
    private static bool Visible(bool open,GObject? content) {
        if(!open || content==null || content.isDisposed || !content.onStage) return false;
        for(int depth=0;content!=null;content=content.parent,depth++)
            if(depth>=128 || content.isDisposed || !content.internalVisible || !content.internalVisible2) return false;
        return true;
    }
    // Tavern screen, or the level-up window opened while in the tavern.
    // Barmaid logic belongs exclusively to Restitutor_Cheats_Bargirls.
    private static bool levelUpFromTavern;
    private static int tavernCity;
    private static double tavernSeen;
    private static double Now=>System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency;
    internal static bool InTavern() {
        var p=Host.Player;var scene=SceneManager.Instance;
        if(!loaded || !Host.Enabled || p==null || PlayerDataManager.Instance?.Data?.Pointer!=p.Pointer || scene==null || !scene.IsSceneEntered || scene.IsInLoadingOrStarting || !scene.IsInHarborScene || p.PlayerPort?.IsStayInPort!=true) { levelUpFromTavern=false;tavernCity=0;return false; }
        int city=p.PlayerPort.StayInPortId;
        var opened=UIManager.Instance?._alreadyOpenedUICtrls;
        bool tavernShown=false,tavernOpen=false,levelUp=false;
        if(opened!=null) foreach(var entry in opened) {
            var ctrl=entry.Value;
            var bar=ctrl?.TryCast<UIDrunkeryCtrl>();
            if(bar?.View?._state!=null && bar.Model?.HarborId==city && bar.View.IsOpen()) {
                tavernOpen=true;
                if(Visible(true,bar.View.UIContent)) tavernShown=true;
            }
            var hero=ctrl?.TryCast<UIHeroLevelUpCtrl>();
            if(hero?.View?._state!=null && Visible(hero.View.IsOpen(),hero.View.UIContent)) levelUp=true;
        }
        if(tavernShown) { tavernCity=city;tavernSeen=Now; }
        // Latch when the level-up window appears: the tavern of this city is still open underneath,
        // or it was shown within the last 2 s (screen transition). Released when the window closes.
        if(!levelUp) levelUpFromTavern=false;
        else if(!levelUpFromTavern) levelUpFromTavern=tavernOpen || (tavernCity==city && Now-tavernSeen<2.0);
        return tavernShown || levelUpFromTavern;
    }
    internal static void Select(int value) {
        try {
            int before=Settings.Interval;
            if(!Settings.Save(value)) { panel.Message("허용되지 않는 값");return; }
            panel.Message($"Lv{value}마다 1포인트로 저장됨");
            log.Msg($"Skill interval {before}->{value} (saved)");
        } catch(Exception ex) { log.Error(ex.ToString());panel.Message("저장 오류 · 로그 확인"); }
    }
    public override void OnDeinitializeMelon() { loaded=false;Host.Unregister(panel); }
}

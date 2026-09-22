using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using MelonLoader;
using MelonLoader.Utils;
using Restitutor.Core;
using Il2CppGyyx.Template;
using Il2CppClient.UILogic.UIHeroLevelUp;
using Il2CppClient.UILogic.UICharacter;
using Il2CppClient.PlayerStore;

[assembly: MelonInfo(typeof(Restitutor.RebalanceGrowth.EntryPoint), Restitutor.RebalanceGrowth.EntryPoint.MelonName, Restitutor.RebalanceGrowth.EntryPoint.Version, "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.RebalanceGrowth;

// Navigator growth rebalance: skill point every 5 levels (was 15), max level 200 (was 99),
// ability cap 500 (was 99). Table values are changed once when the tables load (no per-frame work);
// four display/grant spots that hardcode the old values are corrected by postfixes.
// Cheats Skill (Interface O on) still overrides the interval through its LevelGetSkill postfix,
// and yields its own 10-level extra grant to this mod. See docs/mods/rebalance-growth/.
public sealed partial class EntryPoint : MelonMod {
    public const string MelonName="Restitutor Rebalance Growth";
    public const string Version="0.3.0";
    private const string Baseline="50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA";
    private static MelonLogger.Instance log=null!;
    private static bool enabled;
    private static string? lastError;
    private static int hooks;
    private HookSet? hookSet,tableSet;

    public override void OnInitializeMelon() {
        log=LoggerInstance;
        try { Install(); }
        catch(FileNotFoundException ex) when(ex.FileName?.StartsWith("Restitutor.Core",StringComparison.Ordinal)==true)
        { enabled=false; log.Error("Restitutor.Core.dll is missing from UserLibs; Rebalance Growth stays disabled."); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install() {
        if(!CoreInfo.Require(log,"0.2.0")) return;   // 0.3.0: HookSet.Input (Q/E on the level slider)
        bool baseline;
        using(var s=File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory,"GameAssembly.dll")))
        using(var sha=SHA256.Create())
            baseline=Convert.ToHexString(sha.ComputeHash(s))==Baseline;
        // 0.3.0 (user): the level table extension is data only and is installed whatever the GameAssembly build,
        // with its own Harmony instance so a failed main install does not remove it (and vice versa).
        tableSet=new HookSet(new HarmonyLib.Harmony(HarmonyInstance.Id+".tables"),typeof(EntryPoint));
        if(tableSet.InstallAll(log,"Rebalance Growth level table install",() => {
            tableSet.Hook(typeof(TemplateManager),"InitRoleLevel",postfix:nameof(AfterRoleLevel));
            tableSet.Hook(typeof(PlayerRoleData),"UpdateMaxHpValue",prefix:nameof(CheckRow));
            tableSet.Hook(typeof(PlayerRoleData),"UpdateAttack",prefix:nameof(CheckRow));
            tableSet.Hook(typeof(PlayerHoldRoleDB),"Deserialize",postfix:nameof(AfterRolesLoaded));
            tablesOn=true;
            ApplyLevels("init");   // table may already be loaded
        })) log.Msg($"Level table: rows {Rules.OriginalMaxLevel+1}..{Rules.MaxLevel} added on table load (independent of the GameAssembly check); missing rows are logged as [level row].");
        else tablesOn=false;
        if(!baseline) { enabled=false; log.Error("GameAssembly baseline differs; Rebalance Growth rules (skill interval, stat cap, growth %, slider, luck, HP/attack, weight) disabled. Only the level table extension stays active."); return; }
        hookSet=new HookSet(HarmonyInstance,typeof(EntryPoint));
        if(!hookSet.InstallAll(log,"Rebalance Growth install",() => {
            var tm=typeof(TemplateManager);
            var lu=typeof(UIHeroLevelUpCtrl);
            Patch(tm,"InitGameConst",postfix:nameof(AfterGameConst));
            Patch(lu,"OnClickBtnLevelUp",prefix:nameof(ClearPending),postfix:nameof(AfterSingle));
            Patch(lu,"OnClickBtnLevelUpFive",prefix:nameof(BeforeMulti),postfix:nameof(AfterMulti));
            Patch(lu,"ChangeRewardSkillState",prefix:nameof(RewardBefore),postfix:nameof(GrantExtra));
            Patch(typeof(UIHeroLevelUpView),"RefreshRoleData",postfix:nameof(AfterLevelUpRefresh));
            Patch(typeof(UICharacterView),"RefreshTipsRoleInfo",postfix:nameof(AfterTipRefresh));
            // 0.2.0
            Patch(typeof(PlayerRoleData),"GetLuckyValue",postfix:nameof(AfterLuckyValue));
            Patch(typeof(PlayerRoleData),"GetRolePropByID",postfix:nameof(AfterRolePropById));
            Patch(typeof(PlayerRoleData),"UpdateMaxHpValue",postfix:nameof(AfterMaxHp));
            Patch(typeof(PlayerRoleData),"UpdateAttack",postfix:nameof(AfterAttack));
            Patch(typeof(Il2CppClient.UILogic.UILandExplore.UILandExploreCtrl),"InitPlayerModelList",postfix:nameof(AfterRoleList));
            Patch(typeof(Il2CppClient.UILogic.UILandExplore.UILandExploreCtrl),"InitSeamenModelList",postfix:nameof(AfterRoleList));
            // 0.3.0 growth % and level slider
            Patch(lu,"AniResult",postfix:nameof(AfterAniResult));
            Patch(lu,"InitMaxContinueLevel",postfix:nameof(AfterMaxContinue));
            Patch(typeof(CurRoleData),"GetNeedExpAfterEffect",postfix:nameof(AfterNeedExp));
            Patch(typeof(UIHeroLevelUpView),"RefreshNotMax",postfix:nameof(AfterRefreshNotMax));
            Patch(typeof(UIHeroLevelUpView),"RefreshMax",postfix:nameof(AfterRefreshMax));
            hookSet.Input("Rebalance Growth",OnKey);
            enabled=true;
            ApplyConsts("init");   // tables may already be loaded
        })) { enabled=false; return; }
        log.Msg($"Rebalance Growth {Version} loaded (Restitutor.Core {CoreInfo.Version}); {hooks} hooks + 4 level-table hooks. Skill point every {Rules.SkillInterval} levels (was {Rules.OriginalSkillInterval}), max level {Rules.MaxLevel} (was {Rules.OriginalMaxLevel}), ability cap {Rules.StatMax} (was {Rules.OriginalStatMax}); growth per level S/A/B/C/D = 100/77.5/55/32.5/10 % (cumulative, stored in <stat>_Exp); level-up = experience slider (partial exp kept on the role); luck <= {Rules.LuckyMax} incl. event checks; HP/attack gain physical/{Rules.PhysicalDivisor} (was /{Rules.OriginalPhysicalDivisor}); carry weight <= {Rules.WeightMax}.");
    }

    // Read by other Restitutor mods by reflection (Stat Rank, Cheats Skill): true once this mod's rules run.
    public static bool GrowthActive=>enabled;
    public static bool ExtraGrantActive=>enabled;

    // Declared-only lookup; each name is unique on its type (Init<table>(ByteBuffer) static, the rest parameterless).
    private void Patch(Type type,string name,string? prefix=null,string? postfix=null) {
        hookSet!.Hook(type,name,prefix:prefix,postfix:postfix);
        hooks++;
    }

    private static void Fail(string what,Exception ex) {
        var key=what+":"+ex.Message;
        if(lastError==key) return;
        lastError=key; log.Error($"[{what}] {ex}");
    }

    public override void OnDeinitializeMelon() { enabled=false; tablesOn=false; hookSet?.RemoveAll(); tableSet?.RemoveAll(); }
}

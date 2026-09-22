using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using Restitutor.Core;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UICharacter;
using Il2CppClient.UILogic.UIHeroLevelUp;
using Il2CppGyyx.Template;

[assembly: MelonInfo(typeof(Restitutor.DevilFruits.EntryPoint), "Restitutor Devil Fruits", "0.1.3", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.DevilFruits;

// "(스탯명) 악마의 열매" x5: raise one navigator's growth grade (S~D) for one ability by one step.
// Item rows are added at runtime; the effect is recorded in the save (ListSkillBooks) and
// re-applied to the shared Hero template on load. See docs/mods/devil-fruits/.
public sealed partial class EntryPoint : MelonMod {
    internal const string Version="0.1.3";
    private const string Baseline="50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA";
    private static MelonLogger.Instance log=null!;
    private static bool enabled;
    private static string? lastError;
    private static int hooks;

    private HookSet? hookSet;

    public override void OnInitializeMelon() {
        log=LoggerInstance;
        // Everything that touches Restitutor.Core sits in Install(): without Restitutor.Core.dll in
        // UserLibs the failure surfaces here and only this mod stays disabled.
        try { Install(); }
        catch(FileNotFoundException ex) when(ex.FileName?.StartsWith("Restitutor.Core",StringComparison.Ordinal)==true)
        { enabled=false; log.Error("Restitutor.Core.dll is missing from UserLibs; Devil Fruits stays disabled."); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install() {
        if(!CoreInfo.Require(log,"0.1.0")) return;
        hookSet=new HookSet(HarmonyInstance,typeof(EntryPoint));
        if(!hookSet.InstallAll(log,"Devil Fruits install",() => {
            using(var s=File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory,"GameAssembly.dll")))
                if(Convert.ToHexString(SHA256.Create().ComputeHash(s))!=Baseline) throw new InvalidOperationException("GameAssembly baseline differs; Devil Fruits disabled.");
            var tm=typeof(TemplateManager);
            foreach(var init in new[]{"InitTextLib","InitTextLib_English","InitTextLib_Japanese","InitTextLib_ChineseTraditional","InitItemType","InitItem"})
                Patch(tm,init,postfix:nameof(AfterTableInit));
            Patch(typeof(PlayerHoldRoleDB),"Deserialize",postfix:nameof(AfterRolesLoaded));
            Patch(typeof(PlayerHoldRoleDB),"InitHook",postfix:nameof(AfterRolesInit));
            Patch(typeof(UIHeroLevelUpCtrl),"OnClickBtnLevelUp",prefix:nameof(BeforeLevelUp));
            Patch(typeof(UICharacterView),"RefreshTipsRoleInfo",prefix:nameof(BeforeRoleInfo));
            Patch(typeof(UICharacterView),"HideHook",postfix:nameof(AfterHide));
            enabled=true;
            EnsureTemplates("init");   // tables may already be loaded
        })) { enabled=false; return; }
        log.Msg($"Devil Fruits {Version} loaded (Restitutor.Core {CoreInfo.Version}); {hooks} hooks. Items {Rules.ItemTid(0)}..{Rules.ItemTid(Rules.Count-1)}, type {Rules.ItemTypeTid}; use = click a Stat Rank grade letter on the Tab character sheet (fruit panel under the Stat Rank tip, alpha 0.8).");
    }

    // Declared-only lookup by name; every target name is unique on its type (checked against the
    // interop metadata: Init<table>(ByteBuffer) static, Deserialize(PlayerDataSerializer), the rest parameterless).
    private void Patch(Type type,string name,string? prefix=null,string? postfix=null) {
        hookSet!.Hook(type,name,prefix:prefix,postfix:postfix);
        hooks++;
    }

    public override void OnSceneWasInitialized(int buildIndex,string sceneName)=>EnsureTemplates("scene "+sceneName);

    public override void OnUpdate() { if(enabled) TickDialog(); }

    private static void Guard(string what,Action a){ try{a();}catch(Exception ex){Fail(what,ex);} }
    private static void Fail(string what,Exception ex) {
        var key=what+":"+ex.Message;
        if(lastError==key) return;
        lastError=key; log.Error($"[{what}] {ex}");
    }

    public override void OnDeinitializeMelon() { enabled=false; CloseDialog(); Unbind(); HarmonyInstance.UnpatchSelf(); }
}

using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UICharacter;
using Il2CppClient.UILogic.UIHeroLevelUp;
using Il2CppGyyx.Template;

[assembly: MelonInfo(typeof(Restitutor.DevilFruits.EntryPoint), "Restitutor Devil Fruits", "0.1.2", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.DevilFruits;

// "(스탯명) 악마의 열매" x5: raise one navigator's growth grade (S~D) for one ability by one step.
// Item rows are added at runtime; the effect is recorded in the save (ListSkillBooks) and
// re-applied to the shared Hero template on load. See docs/mods/devil-fruits/.
public sealed partial class EntryPoint : MelonMod {
    internal const string Version="0.1.2";
    private const string Baseline="50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA";
    private static MelonLogger.Instance log=null!;
    private static bool enabled;
    private static string? lastError;
    private static int hooks;

    public override void OnInitializeMelon() {
        log=LoggerInstance;
        try {
            using(var s=File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory,"GameAssembly.dll")))
                if(Convert.ToHexString(SHA256.Create().ComputeHash(s))!=Baseline) throw new InvalidOperationException("GameAssembly baseline differs; Devil Fruits disabled.");
            var tm=typeof(TemplateManager);
            foreach(var init in new[]{"InitTextLib","InitTextLib_English","InitTextLib_Japanese","InitTextLib_ChineseTraditional","InitItemType","InitItem"})
                Patch(tm,init,1,postfix:nameof(AfterTableInit));
            Patch(typeof(PlayerHoldRoleDB),"Deserialize",1,postfix:nameof(AfterRolesLoaded));
            Patch(typeof(PlayerHoldRoleDB),"InitHook",0,postfix:nameof(AfterRolesInit));
            Patch(typeof(UIHeroLevelUpCtrl),"OnClickBtnLevelUp",0,prefix:nameof(BeforeLevelUp));
            Patch(typeof(UICharacterView),"RefreshTipsRoleInfo",0,prefix:nameof(BeforeRoleInfo));
            Patch(typeof(UICharacterView),"HideHook",0,postfix:nameof(AfterHide));
            enabled=true;
            EnsureTemplates("init");   // tables may already be loaded
            log.Msg($"Devil Fruits {Version} loaded; {hooks} hooks. Items {Rules.ItemTid(0)}..{Rules.ItemTid(Rules.Count-1)}, type {Rules.ItemTypeTid}; use = click a Stat Rank grade letter on the Tab character sheet (fruit panel under the Stat Rank tip, alpha 0.8).");
        } catch(Exception ex) { enabled=false; HarmonyInstance.UnpatchSelf(); log.Error(ex.ToString()); }
    }

    // Declared-only lookup by name and parameter count (Deserialize(PlayerDataSerializer) is
    // declared on PlayerHoldRoleDB itself; Init<table>(ByteBuffer) are static).
    private void Patch(Type type,string name,int args,string? prefix=null,string? postfix=null) {
        var m=type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly)
                  .FirstOrDefault(x=>x.Name==name && x.GetParameters().Length==args)
              ?? throw new MissingMethodException(type.FullName,name);
        HarmonyMethod? H(string? n)=>n==null?null:new HarmonyMethod(typeof(EntryPoint).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!);
        HarmonyInstance.Patch(m,prefix:H(prefix),postfix:H(postfix));
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

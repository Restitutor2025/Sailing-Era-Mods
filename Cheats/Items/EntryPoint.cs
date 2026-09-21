using MelonLoader;
using Il2CppClient.Manager;
using Il2CppClient.UILogic.UICharacter;
using Il2CppClient.Utils;
using Il2CppFairyGUI;
using Il2CppGyyx.Template;
using Restitutor.Cheats.Interface;
[assembly: MelonInfo(typeof(Restitutor.Cheats.Items.EntryPoint),"Restitutor Items","0.1.0","Restitutor")]
[assembly: MelonGame("bolingo","SailingEra")]
[assembly: MelonAdditionalDependencies("Restitutor_Cheats_Interface")]
namespace Restitutor.Cheats.Items;

public sealed class EntryPoint:MelonMod {
    private static bool loaded,adding;
    private static MelonLogger.Instance log=null!;
    private static readonly ItemsPanel panel=new();
    public override void OnInitializeMelon() {
        log=LoggerInstance;
        if(!Host.Enabled) { log.Error("Cheats Interface disabled; Items panel not registered.");return; }
        Host.Register(panel);loaded=true;
        log.Msg("Restitutor Items 0.1.0 loaded; no native hooks; Tab character sheet only; equipment/books from the item table, devil fruits "+string.Join(",",DevilFruits.Ids)+" when registered; RewardUtils.GainItem(id,1).");
    }
    public override void OnDeinitializeMelon() { loaded=false;Host.Unregister(panel); }

    private static bool Shown(GObject? content) {
        if(content==null || content.isDisposed || !content.onStage) return false;
        for(int depth=0;content!=null;content=content.parent,depth++)
            if(depth>=128 || content.isDisposed || !content.internalVisible || !content.internalVisible2) return false;
        return true;
    }
    // Once per frame: Host calls Refresh and Visible in the same frame.
    private static int frame=-1;private static UICharacterCtrl? cached;
    internal static UICharacterCtrl? CharacterSheet() {
        int now=UnityEngine.Time.frameCount;
        if(now==frame) return cached;
        frame=now;cached=null;
        try {
            var p=Host.Player;
            if(!loaded || !Host.Enabled || p==null || PlayerDataManager.Instance?.Data?.Pointer!=p.Pointer) return null;
            var opened=UIManager.Instance?._alreadyOpenedUICtrls;
            if(opened==null) return null;
            foreach(var entry in opened) {
                var ctrl=entry.Value?.TryCast<UICharacterCtrl>();
                if(ctrl==null) continue;
                var view=ctrl.View;var model=ctrl.Model;
                if(view?._state==null || model==null || !view.IsOpen() || !Shown(view.UIContent)) continue;
                if(model.SheetType!=ESheetType.Character) continue;
                cached=ctrl;break;
            }
        } catch(Exception ex) { log.Error(ex.ToString()); }
        return cached;
    }

    internal static string Localize(string? key,string fallback) {
        if(string.IsNullOrEmpty(key)) return fallback;
        try { var s=TextLibUtils.Text(key,key); return string.IsNullOrWhiteSpace(s)?fallback:s; } catch { return fallback; }
    }
    private static Item? Find(int id) { try { return TemplateManager.GetItem(id,out var item)?item:null; } catch { return null; } }

    // Item table snapshot per tab (the table is fixed for the process; fruits may be registered later).
    private static Dictionary<Tab,List<Entry>>? catalog;
    internal static List<Entry> Catalog(Tab tab) {
        if(catalog==null) {
            var built=new Dictionary<Tab,List<Entry>>{[Tab.Equipment]=new(),[Tab.Book]=new()};
            var typeNames=new Dictionary<int,string>();
            foreach(var item in TemplateManager.ItemValues) {
                if(item==null) continue;
                var t=Rules.TabOf(item.type,item.tid);
                if(t==null || t==Tab.DevilFruit) continue;
                if(!typeNames.TryGetValue(item.type,out var tn)) { tn=Localize(TemplateManager.GetItemType(item.type)?.name,"#"+item.type);typeNames[item.type]=tn; }
                built[t.Value].Add(new Entry(item.tid,Localize(item.name,"#"+item.tid),tn,item.type));
            }
            foreach(var k in built.Keys.ToArray()) built[k]=Rules.Sort(k,built[k]);
            if(built[Tab.Equipment].Count==0 && built[Tab.Book].Count==0) return new(); // table not ready yet; retry later
            catalog=built;
            log.Msg($"Item catalog: equipment {built[Tab.Equipment].Count}, books {built[Tab.Book].Count}.");
        }
        if(tab!=Tab.DevilFruit) return catalog[tab];
        var fruits=new List<Entry>();
        foreach(int id in DevilFruits.Ids) {
            var item=Find(id);
            if(item!=null) fruits.Add(new Entry(id,Localize(item.name,"#"+id),"악마의 열매",item.type));
        }
        return fruits;
    }
    internal static bool FruitsRegistered()=>DevilFruits.Ids.Any(id=>Find(id)!=null);

    internal static void Add(Entry entry) {
        if(adding) return;
        try {
            adding=true;
            var ctrl=CharacterSheet();
            if(ctrl==null) { panel.Message("인물 탭에서만 추가할 수 있습니다.");return; }
            if(Find(entry.Id)==null) { panel.Message($"아이템 표에 없음: {entry.Name} ({entry.Id})");return; }
            // Same call the game's GM GetItem command uses; true = native "item obtained" tip.
            bool ok=RewardUtils.GainItem(entry.Id,1,true,false);
            if(!ok) { panel.Message($"추가 실패: {entry.Name} (가방이 가득 찼는지 확인)");log.Warning($"GainItem({entry.Id},1) returned false");return; }
            Redraw(ctrl);
            panel.Message($"추가: {entry.Name} ×1");
            log.Msg($"Added item {entry.Id} ({entry.Name}) x1");
        } catch(Exception ex) { log.Error(ex.ToString());panel.Message("추가 오류 · 로그 확인"); }
        finally { adding=false; }
    }
    // Same data refresh Tab Characters uses after book changes, plus the equipment list.
    private static void Redraw(UICharacterCtrl ctrl) {
        try {
            var data=UICharacterCtrl.Data;
            if(data?.PlayerBag!=null) ctrl.InitBookData(data.PlayerBag);
            if(data?.PlayerEquipData!=null) ctrl.InitEquipData(data.PlayerEquipData);
            ctrl.Model?.MarkDirty();
        } catch(Exception ex) { log.Warning("Character sheet redraw failed (item already added): "+ex.Message); }
    }
    internal static void ResetCatalog()=>catalog=null;
}

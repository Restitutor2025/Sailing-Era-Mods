using Il2CppGyyx.Template;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Restitutor.DevilFruits;

// Adds the new rows to TemplateManager's static dictionaries (Gyyx.Template.TemplateManager
// _itemType / _item / _textLib*). Native GetItem(int) (0x3CB800) is a plain dictionary lookup
// on the static field, so a row added here is what every Get* caller sees.
// Called after every Init<table> postfix and on later safety points; adds only what is missing.
public sealed partial class EntryPoint {
    private static bool loggedTypes,loggedItems,loggedTexts;

    // Allocated like any il2cpp object; the native constructor (which reads a FlatBuffers row)
    // is not run, so every reference field is set explicitly below.
    private static T New<T>(Func<IntPtr,T> wrap)=>wrap(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<T>.NativeClassPtr));

    private static void EnsureTemplates(string why) {
        if(!enabled) return;
        try { EnsureTexts(why); } catch(Exception ex) { Fail("texts",ex); }
        try { EnsureItemType(why); } catch(Exception ex) { Fail("item type",ex); }
        try { EnsureItems(why); } catch(Exception ex) { Fail("items",ex); }
    }

    private static void EnsureItemType(string why) {
        var dict=TemplateManager._itemType;
        if(dict==null) return;
        var parent=dict.ContainsKey(Rules.ParentItemType) ? dict[Rules.ParentItemType] : null;
        // 0.1.1: rows added at "init" before the table was filled used fallbacks (user log:
        // parent 1000 found=False); copy the native values once the source row exists.
        if(dict.ContainsKey(Rules.ItemTypeTid)) { if(parent!=null) dict[Rules.ItemTypeTid].icon=parent.icon; return; }
        var row=New(p=>new ItemType(p));
        row.tid=Rules.ItemTypeTid;
        row.code="devil_fruit";
        row.name=Rules.TypeNameKey;
        row.icon=parent?.icon ?? Rules.Icon;
        row.parentType=Rules.ParentItemType;
        row.canUseInBag=false;               // used from the character sheet, not the bag
        row.bagHint=Rules.TypeHintKey;
        dict[Rules.ItemTypeTid]=row;
        if(!loggedTypes){loggedTypes=true;log.Msg($"ItemType {Rules.ItemTypeTid} added ({why}); parent {Rules.ParentItemType} found={parent!=null}.");}
    }

    private static void EnsureItems(string why) {
        var dict=TemplateManager._item;
        if(dict==null) return;
        var source=dict.ContainsKey(Rules.IconSourceItem) ? dict[Rules.IconSourceItem] : null;
        int added=0;
        for(int s=0;s<Rules.Count;s++) {
            int tid=Rules.ItemTid(s);
            if(dict.ContainsKey(tid)) {
                if(source!=null){var r=dict[tid];r.iconDeck=source.iconDeck;r.heavy=source.heavy;}   // 0.1.1: see EnsureItemType
                continue;
            }
            var row=New(p=>new Item(p));
            row.tid=tid;
            row.name=Rules.NameKey(s);
            row.desc=Rules.DescKey(s);
            row.longDesc=Rules.LongDescKey(s);
            row.icon=Rules.Icon;
            row.type=Rules.ItemTypeTid;
            row.price=0;                      // not specified by the user: no price
            row.tradeable=0;                  // not specified: not sellable
            row.pileNum=1;                    // every original row is 1 (InnerAddNum arithmetic)
            row.iconDeck=source?.iconDeck ?? "Assets/AssetsPackage/Img/ItemIcon/deck/ui_common_frame_brown.png";
            row.only=0;
            row.heavy=source?.heavy ?? .7f;
            row.table=0;
            row.discardable=1;
            row.tag=new Il2CppStructArray<int>(new[]{1,2}); // non-cargo default in the table
            row.showIcon="0";
            row.bookTitle="0"; row.bookAuthor="0"; row.bookContent="0";
            dict[tid]=row;
            added++;
        }
        if(added>0 && !loggedItems){loggedItems=true;log.Msg($"Items {Rules.ItemTid(0)}..{Rules.ItemTid(Rules.Count-1)} added ({why}); frame/weight source {Rules.IconSourceItem} found={source!=null}.");}
    }

    // Korean text goes into the base TextLib (R11: chunk 196 = Korean) and, so the name never
    // shows as a raw key, into the other three language tables as well.
    private static void EnsureTexts(string why) {
        int added=0;
        foreach(var (key,text) in Rules.Texts()) {
            var a=TemplateManager._textLib;
            if(a!=null && !a.ContainsKey(key)){var r=New(p=>new TextLib(p));r.tid=key;r.text=text;a[key]=r;added++;}
            var b=TemplateManager._textLib_English;
            if(b!=null && !b.ContainsKey(key)){var r=New(p=>new TextLib_English(p));r.tid=key;r.text=text;b[key]=r;added++;}
            var c=TemplateManager._textLib_Japanese;
            if(c!=null && !c.ContainsKey(key)){var r=New(p=>new TextLib_Japanese(p));r.tid=key;r.text=text;c[key]=r;added++;}
            var d=TemplateManager._textLib_ChineseTraditional;
            if(d!=null && !d.ContainsKey(key)){var r=New(p=>new TextLib_ChineseTraditional(p));r.tid=key;r.text=text;d[key]=r;added++;}
        }
        if(added>0 && !loggedTexts && TemplateManager._textLib!=null){loggedTexts=true;log.Msg($"TextLib entries added ({why}): {added}.");}
    }

    // Init<table> postfixes (static methods; one postfix for all).
    private static void AfterTableInit() => EnsureTemplates("table init");
}

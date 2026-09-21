using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;

namespace Restitutor.TabCharacters;

// 0.6.8: the 0.6.7 run (22:55 log) showed every left press over the language text and its
// title hit UISheetCharacter.btnReturn (GGraph, bounds (0,349)-(2560,1419)) and no
// "click received" line, so the onClick bindings on texLanguage/TexTitleCharacterLanguage
// never fire. Same cause and same remedy as the equipment slots (0.6.2, EquipPanel.cs):
// the press is taken on btnReturn itself; only a press and release inside the language
// row cancel the native btnReturn click and open the language book panel. Other presses
// pass through unchanged.
public sealed partial class EntryPoint
{
    private static GObject? langCatcher;
    private static EventCallback1? langBegin, langEnd;
    private static bool langArmed;
    private static UICharacterView? langCatcherView;

    private static bool LanguageAt(UICharacterView view, float x, float y)
    {
        var sheet = view.SheetCharacter;
        return EquipInsideAt(sheet.texLanguage, x, y) || EquipInsideAt(sheet.TexTitleCharacterLanguage, x, y);
    }

    static partial void BindLanguageCatcher(UICharacterView view)
    {
        var target = view.SheetCharacter.btnReturn;
        langCatcherView = view;
        if (target == null || target.isDisposed || langCatcher?.Pointer == target.Pointer) return;
        UnbindLanguageCatcher();
        langCatcherView = view;
        langBegin = (EventCallback1)(Action<EventContext>)(e => GuardBook(() =>
        {
            langArmed = false;
            var v = langCatcherView;
            if (!Allowed || v == null || e.inputEvent.button != 0 || books != null || equip != null || !IsCharacter(v)) return;
            if (!LanguageAt(v, e.inputEvent.x, e.inputEvent.y)) return;
            langArmed = true;
            e.CaptureTouch();
            LangTrace($"language press taken from btnReturn at ({e.inputEvent.x:0},{e.inputEvent.y:0})");
        }));
        langEnd = (EventCallback1)(Action<EventContext>)(e => GuardBook(() =>
        {
            bool armed = langArmed; langArmed = false;
            var v = langCatcherView;
            if (!armed || v == null || e.inputEvent.button != 0) return;
            // A press that started on the language row never reaches the native btnReturn click.
            e.StopPropagation(); Stage.inst.CancelClick(e.inputEvent.touchId);
            if (!LanguageAt(v, e.inputEvent.x, e.inputEvent.y)) { LangTrace("language press released outside the row; nothing opened"); return; }
            LangTrace("click received: language row via btnReturn");
            OpenBooks(v, -1);
        }));
        target.onTouchBegin.Add(langBegin);
        target.onTouchEnd.Add(langEnd);
        langCatcher = target;
    }

    // 0.6.9: 0.6.8 opened the panel but UICharacterView.RefreshSkillBookTips threw a
    // NullReferenceException for language books (22:58 log). Native (R11 GameAssembly):
    // for BookType != 1 and a learnable book it reads content.roleInfo.listLanguage.data
    // (the hero's HeroLanguage list) and takes GetChildAt(data.Count) for its preview.
    // RefreshTipsRoleInfo writes that data and sets numItems = MaxLanguageCount only when
    // model._sheetType == 1 (the skill sheet, retired in 0.5.9); on the Characters sheet it
    // skips that branch, so data stays null. For the one synchronous call, prepare the list
    // exactly as that branch does, then restore the previous data and item count.
    private static GList? langPreviewList;
    private static Il2CppSystem.Object? langPreviewData;
    private static int langPreviewCount = -1;

    static partial void LanguagePreviewBegin(UICharacterView view, Il2CppClient.PlayerStore.PlayerRoleData role)
    {
        LanguagePreviewEnd();
        var list = view._UIContent_k__BackingField?.roleInfo?.listLanguage;
        if (list == null || list.isDisposed) { LangTrace("language preview: roleInfo.listLanguage missing"); return; }
        var languages = role.HeroLanguage;
        int max = UICharacterModel.MaxLanguageCount;
        LangTrace($"language preview: data={(list.data == null ? "null" : "set")} items={list.numItems} known={(languages == null ? -1 : languages.Count)} max={max}");
        langPreviewList = list; langPreviewData = list.data; langPreviewCount = list.numItems;
        list.data = languages;
        if (list.numItems < max) list.numItems = max;
    }

    static partial void LanguagePreviewEnd()
    {
        var list = langPreviewList; langPreviewList = null;
        if (list != null && !list.isDisposed)
        {
            if (langPreviewCount >= 0 && list.numItems != langPreviewCount) list.numItems = langPreviewCount;
            list.data = langPreviewData;
        }
        langPreviewData = null; langPreviewCount = -1;
    }

    static partial void UnbindLanguageCatcher()
    {
        var target = langCatcher; langCatcher = null; langArmed = false; langCatcherView = null;
        if (target != null && !target.isDisposed)
        {
            if (langBegin != null) target.onTouchBegin.Remove(langBegin);
            if (langEnd != null) target.onTouchEnd.Remove(langEnd);
        }
        langBegin = null; langEnd = null;
    }
}

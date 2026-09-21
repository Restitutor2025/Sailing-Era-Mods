using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Restitutor.TabCharacters;

// 0.6.7 diagnostics only (no behaviour change). User report: clicking the language
// row on the Characters sheet opens nothing, and the 0.6.5 log cannot tell which step
// fails because the language path wrote no lines. Candidates:
//  A) another object receives the press (like btnReturn over the equipment slots, 0.6.1/0.6.2)
//  B) the click binding is not attached to the object shown on screen
//  C) the click arrives but OpenBooks returns early
// Lines are prefixed [LangTrace] and capped at 40 per run.
public sealed partial class EntryPoint
{
    private static int langTraceLines, langProbeLines;
    private static UICharacterView? langProbeView;
    private static IntPtr langTracedLanguage, langTracedTitle;

    static partial void LangTrace(string text)
    {
        if (langTraceLines >= 40) return;
        langTraceLines++;
        host?.LoggerInstance.Msg("[LangTrace] " + text);
    }

    private static string LangBounds(GObject? obj)
    {
        if (obj == null || obj.isDisposed) return "n/a";
        try
        {
            var a = obj.LocalToGlobal(new Vector2(0, 0));
            var b = obj.LocalToGlobal(new Vector2(obj.width, obj.height));
            return $"({a.x:0},{a.y:0})-({b.x:0},{b.y:0})";
        }
        catch (Exception ex) { return "error " + ex.GetType().Name; }
    }

    private static string LangAncestors(GObject? obj)
    {
        var chain = new List<string>();
        for (GObject? node = obj?.parent; node != null && chain.Count < 6; node = node.parent) chain.Add(Describe(node));
        return string.Join(" <- ", chain);
    }

    static partial void LangBindTrace(UICharacterView view)
    {
        langProbeView = view;
        var sheet = view.SheetCharacter;
        var language = sheet.texLanguage; var title = sheet.TexTitleCharacterLanguage;
        var lp = language == null ? IntPtr.Zero : language.Pointer;
        var tp = title == null ? IntPtr.Zero : title.Pointer;
        if (lp == langTracedLanguage && tp == langTracedTitle) return;
        langTracedLanguage = lp; langTracedTitle = tp;
        LangTrace($"bind texLanguage={Describe(language)} bound={(lp != IntPtr.Zero && bookBindings.ContainsKey(lp))} bounds={LangBounds(language)} text='{language?.text}'");
        LangTrace($"bind TexTitleCharacterLanguage={Describe(title)} bound={(tp != IntPtr.Zero && bookBindings.ContainsKey(tp))} bounds={LangBounds(title)}");
        LangTrace($"language ancestors=[{LangAncestors(language)}] btnReturn={Describe(sheet.btnReturn)} btnReturnBounds={LangBounds(sheet.btnReturn)}");
    }

    // Left press over the language text or its title: which object FairyGUI hit.
    static partial void TickLangProbe()
    {
        var view = langProbeView;
        if (langProbeLines >= 5 || langTraceLines >= 40 || view == null || Mouse.current?.leftButton.wasPressedThisFrame != true) return;
        try
        {
            if (view._UIContent_k__BackingField == null || view._UIContent_k__BackingField.isDisposed || !IsCharacter(view)) return;
            var sheet = view.SheetCharacter;
            float x = Stage.inst.touchPosition.x, y = Stage.inst.touchPosition.y;
            bool onLanguage = EquipInsideAt(sheet.texLanguage, x, y), onTitle = EquipInsideAt(sheet.TexTitleCharacterLanguage, x, y);
            if (!onLanguage && !onTitle) return;
            var chain = new List<string>();
            for (var node = Stage.inst.touchTarget; node != null && chain.Count < 10; node = node.parent)
            {
                var owner = node.gOwner;
                chain.Add(owner != null ? Describe(owner) : $"DisplayObject('{node.name}')");
            }
            langProbeLines++;
            LangTrace($"left press over {(onLanguage ? "texLanguage" : "TexTitleCharacterLanguage")} at ({x:0},{y:0}): inputActive={view.IsInputActive} books={books != null} equip={equip != null} hit=[{string.Join(" <- ", chain)}]");
        }
        catch (Exception ex) { langProbeLines = 5; host?.LoggerInstance.Warning("[LangTrace] probe stopped: " + ex.Message); }
    }
}

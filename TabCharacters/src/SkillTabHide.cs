using Il2CppClient.Const;
using Il2CppClient.UILogic;
using Il2CppClient.UILogic.UIMenu;
using Il2CppFairyGUI;

namespace Restitutor.TabCharacters;

public sealed partial class EntryPoint
{
    // 0.5.9: the top menu's skill tab is retired. Book/point learning lives in the
    // Characters tab. Verified native entries (GameAssembly 50D53D17...):
    //  - UIMenuCtrl.ShowChild index 3 is the only direct caller of UICharacterCtrl.ShowSheet(Skill).
    //  - Index 3 is reached by TabOnclick (tab button), GetNextSheetIndex (L2/R2, skips
    //    closed tabs via GetSheetOpenByIndex), UIGlobalCtrl.OnFeatureQuickKeyStart
    //    (quick key while the menu is open, gated by GetSheetOpenByIndex) and
    //    UIGlobalCtrl.OnInputActionStart -> UIMenuHelper.Start (quick key while closed).
    //  - UIMenuView.RefreshBtnKnowledge shows the skill tab "!" when any in-team role
    //    has unspent skill points (UICharacterCtrl.CheckBtnNewMark); that mark is copied
    //    to the Characters tab button. The skill sheet object itself stays (the learning
    //    panel borrows its widgets).
    private const int SkillMenuTab = 3, CharacterMenuTab = 2;
    // 0.6.4: the equipment tab is retired the same way. Verified (GameAssembly 50D53D17...):
    // UIMenuCtrl.ShowChild index 4 is the only caller of UICharacterCtrl.ShowSheet (all
    // three callers are ShowChild cases 2/3/4), and EMenuTabType.Equip == 4 (interop
    // constant). Equipment is changed from the Characters sheet panel (EquipPanel*.cs).
    private const int EquipMenuTab = 4;
    private static bool Retired(int tab) => tab == SkillMenuTab || tab == EquipMenuTab;
    private static bool skillTabFaultLogged;

    partial void InstallSkillTab()
    {
        Patch(typeof(UIMenuCtrl), "GetSheetOpenByIndex", new[] { typeof(int) }, null, nameof(SkillSheetOpen));
        Patch(typeof(UIGlobalCtrl), "OnFeatureQuickKeyStart", new[] { typeof(EMenuTabType) }, nameof(BlockSkillQuickKey));
        Patch(typeof(UIGlobalCtrl), "OnInputActionStart", new[] { typeof(EMenuTabType) }, nameof(BlockSkillInput));
        Patch(typeof(UIMenuView), "ItemRender", new[] { typeof(int), typeof(GObject) }, null, nameof(HideSkillTab));
        Patch(typeof(UIMenuView), "RefreshBtnKnowledge", Type.EmptyTypes, null, nameof(MoveSkillBadge));
    }

    // Closed tab: skipped by L2/R2, refused by ChangeTab and the open-menu quick key.
    private static void SkillSheetOpen(int __0, ref bool __result)
    {
        if (Allowed && Retired(__0)) __result = false;
    }
    // K (or any skill-tab quick action) does nothing, whether the menu is open or closed.
    private static bool BlockSkillQuickKey(EMenuTabType __0) => !Allowed || !Retired((int)__0);
    private static bool BlockSkillInput(EMenuTabType __0) => !Allowed || !Retired((int)__0);

    // Hide the button and fold its slot. Child indexes stay 0..8, so native index-based
    // code (RefreshBtnKnowledge GetChildAt(3)/(8), TabOnclick data) is unchanged.
    private static void HideSkillTab(UIMenuView __instance, int __0, GObject __1)
    {
        if (!Allowed || !Retired(__0) || __1 == null) return;
        try
        {
            __1.visible = false;
            var list = __instance.ListTitle;
            if (list != null && !list.foldInvisibleItems)
            {
                list.foldInvisibleItems = true;
                list.SetBoundsChangedFlag();
            }
        }
        catch (Exception ex) { SkillTabFault(ex); }
    }

    private static void MoveSkillBadge(UIMenuView __instance)
    {
        if (!Allowed) return;
        try
        {
            var list = __instance.ListTitle;
            if (list == null || list.numChildren <= SkillMenuTab) return;
            var skill = list.GetChildAt(SkillMenuTab)?.TryCast<Il2CppUIMenu.UIbtnTitle>();
            var character = list.GetChildAt(CharacterMenuTab)?.TryCast<Il2CppUIMenu.UIbtnTitle>();
            if (skill?.ctrlNewState == null || character?.ctrlNewState == null) return;
            character.ctrlNewState.selectedIndex = skill.ctrlNewState.selectedIndex;
        }
        catch (Exception ex) { SkillTabFault(ex); }
    }

    private static void SkillTabFault(Exception ex)
    {
        if (skillTabFaultLogged) return;
        skillTabFaultLogged = true;
        host?.LoggerInstance.Error("Skill tab retirement (menu display only; learning unaffected): " + ex);
    }
}

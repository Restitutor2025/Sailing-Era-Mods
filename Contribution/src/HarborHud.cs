using Il2CppClient.Manager;
using Il2CppClient.UILogic.UIHarbor;
namespace Restitutor.Contribution;
internal static class HarborHud {
    // Harbor main UI is the focused view (its visibility chain may still be settling).
    internal static bool MainFocused() {
        try {
            var harbor = UIManager.Instance?.CurrentFocusViewCtrl?.TryCast<UIHarborCtrl>();
            return harbor != null && !harbor.IsClose() && harbor.Model?.CtrlShowMainUI == 1;
        } catch { return false; }
    }
    internal static bool IsMainScreen() {
        try {
            // Read the existing focus; never create a harbor singleton just to inspect UI.
            var focus = UIManager.Instance?.CurrentFocusViewCtrl;
            var harbor = focus?.TryCast<UIHarborCtrl>();
            if (harbor == null || harbor.IsClose() || harbor.Model?.CtrlShowMainUI != 1) return false;
            var view = harbor.View;
            var content = view?.UIContent;
            if (view?._state == null || content == null || content.isDisposed || content.displayObject?.stage == null) return false;
            for (Il2CppFairyGUI.GObject? node = content; node != null; node = node.parent) {
                if (!node.visible || (node.group != null && !node.group.visible)) return false;
            }
            return true;
        } catch (Exception ex) { EntryPoint.Error("Harbor HUD visibility", ex); return false; }
    }
}


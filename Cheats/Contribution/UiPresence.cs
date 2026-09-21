using Il2CppFairyGUI;
namespace Restitutor.Cheats.Contribution;
internal static class UiPresence {
    internal static bool IsDisplayed(bool open, GObject? content) {
        if (!open || content == null || content.isDisposed || !content.onStage) return false;
        // IsOpen is a view-state flag. A hidden/detached ancestor can leave it true.
        for (int depth=0; content != null; depth++, content=content.parent) {
            if (depth >= 128) return true; // Invalid hierarchy: do not enable cheats.
            if (content.isDisposed || !content.internalVisible || !content.internalVisible2) return false;
        }
        return true;
    }
}

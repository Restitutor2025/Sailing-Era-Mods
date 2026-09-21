using Il2CppClient.UILogic.UIMap;
using Il2CppClient.UILogic.UIMap.UIMapIcon;
using Il2CppClient.Utils;
using Il2CppGyyx.Template;
using Il2CppInterop.Runtime;
using Il2CppMap;

namespace Restitutor.Map;

// Unreachable route ports are drawn red.
// 0.2.3 native path: the original UIMapHarbourIcon.UpdateInfo already has a red-port branch
// (0x5E4DE4..0x5E4F1B): UIMapCtrl._eMapUseType(+0x68)==1 and UIMapCtrl.RedPort(+0x78)
// contains HarbourId(+0x38) -> IconUtils.GetRedHarbourIcon + ctrlSelfPort=2. For an unreachable
// icon only, the type is set to 1 for the duration of that one UpdateInfo call and restored in a
// finalizer; RedPort gets the id once. The original then writes the same url/state every frame,
// which GLoader/Controller ignore when unchanged. 0.2.1 overwrote the original's url and state
// after every call (log 2026-09-22: up to 3331 url resets per second while dragging).
// Legacy path (0.2.1 behavior) is kept for a field-layout mismatch.
internal static class RoutePortVisuals
{
    internal static bool Native { get; private set; }
    private static UIMapCtrl? owner;
    private static readonly HashSet<int> added = new();   // ids this session put into RedPort
    private static readonly HashSet<int> known = new();   // ids already checked against RedPort

    internal static void Verify()
    {
        uint Offset(Type type, string field)
        {
            var info = type.GetField("NativeFieldInfoPtr_" + field, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (info?.GetValue(null) is not IntPtr ptr || ptr == IntPtr.Zero) return 0;
            return IL2CPP.il2cpp_field_get_offset(ptr);
        }
        uint use = Offset(typeof(UIMapCtrl), "_eMapUseType"), red = Offset(typeof(UIMapCtrl), "RedPort"),
             id = Offset(typeof(UIMapHarbourIcon), "_HarbourId_k__BackingField");
        Native = use == 0x68 && red == 0x78 && id == 0x38;
        string text = $"_eMapUseType=0x{use:X} RedPort=0x{red:X} HarbourId=0x{id:X}";
        if (Native) EntryPoint.Log.Msg("[ROUTE] Red ports use the native UpdateInfo branch (" + text + ").");
        else EntryPoint.Log.Warning("[ROUTE] Native red-port layout differs (" + text + "); using the 0.2.1 overwrite path.");
    }

    // --- native path -------------------------------------------------------------------
    // Returns the previous use type, or -1 when this call is left untouched.
    internal static int Begin(UIMapCtrl map, int harbourId)
    {
        if (!ReferenceEquals(owner, map) && owner?.Pointer != map.Pointer) { owner = map; added.Clear(); known.Clear(); }
        if (known.Add(harbourId))
        {
            var list = map.RedPort;
            if (list != null && !list.Contains(harbourId)) { list.Add(harbourId); added.Add(harbourId); }
        }
        int previous = (int)map._eMapUseType;
        if (previous == 1) return -1;
        map._eMapUseType = (EMapUseType)1;
        return previous;
    }
    // Route changed (RefreshRoute): re-check RedPort membership once per id.
    internal static void Invalidate() => known.Clear();
    internal static void End(UIMapCtrl map, int previous)
    { if (previous >= 0) map._eMapUseType = (EMapUseType)previous; }

    // --- legacy path (0.2.1) -----------------------------------------------------------
    private static readonly Dictionary<IntPtr, (UIHarbourIcon ui, string url, int state)> saved = new();
    internal static void Apply(UIMapHarbourIcon icon, bool reachable, bool nativeRefresh)
    {
        var ui = icon.Component?.TryCast<UIHarbourIcon>();
        if (ui == null || ui.isDisposed || ui.loaderIcon == null || ui.ctrlSelfPort == null) return;
        if (nativeRefresh || !saved.ContainsKey(ui.Pointer))
            saved[ui.Pointer] = (ui, ui.loaderIcon.url, ui.ctrlSelfPort.selectedIndex);
        var baseline = saved[ui.Pointer];
        ui.loaderIcon.url = reachable ? baseline.url : IconUtils.GetRedHarbourIcon(TemplateManager.GetPort(icon.HarbourId).mapIcon);
        ui.ctrlSelfPort.selectedIndex = reachable ? baseline.state : 2;
    }

    internal static void Restore()
    {
        // Native path: remove only the ids this session added; the original redraws icons itself.
        var map = owner;
        if (map != null && added.Count > 0)
        {
            try { var list = map.RedPort; if (list != null) foreach (int id in added) list.Remove(id); map.Model?.MarkDirty(); }
            catch (Exception ex) { EntryPoint.Log.Error("[ROUTE] RedPort restore failed: " + ex.Message); }
        }
        owner = null; added.Clear(); known.Clear();
        foreach (var entry in saved.Values)
        {
            if (entry.ui.isDisposed) continue;
            if (entry.ui.loaderIcon != null && !entry.ui.loaderIcon.isDisposed) entry.ui.loaderIcon.url = entry.url;
            if (entry.ui.ctrlSelfPort != null) entry.ui.ctrlSelfPort.selectedIndex = entry.state;
        }
        saved.Clear();
    }
}

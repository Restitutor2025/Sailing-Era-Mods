using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIMap;
using Il2CppClient.UILogic.UIMap.UIMapMask;

namespace Restitutor.Map;

// Protects only a known mask/area pair. Never merges exploration from another save.
internal static class FogLifetime
{
    private static readonly MaskOwnership owner = new();
    internal static void Install()
    {
        EntryPoint.Hook(typeof(UIMapCtrl), "InitUIMask", typeof(FogLifetime), nameof(BeforeInit), nameof(AfterInit));
        EntryPoint.Hook(typeof(PlayerAreaDB), "Deserialize", typeof(FogLifetime), nameof(Reset));
        // InitHook also clears MiniMapMaskData on an existing database object.
        EntryPoint.Hook(typeof(PlayerAreaDB), "InitHook", typeof(FogLifetime), nameof(Reset));
        EntryPoint.Hook(typeof(MapMaskManager), "Dispose", typeof(FogLifetime), nameof(Reset));
        EntryPoint.Hook(typeof(MapMaskManager), "SetMaskData", typeof(FogLifetime), nameof(Reset));
    }
    private static void Reset()
    {
        owner.Reset();
        EntryPoint.Log.Msg("[FOG] Mask ownership reset for native data initialization/replacement/disposal.");
    }
    private static bool BeforeInit()
    {
        try
        {
            var area = PlayerDataManager.Instance.Data?.PlayerAreaDB;
            var mask = MapMaskManager.Instance.MapMask;
            bool usable = mask != null && UIMapMask.MainTexture != null && UIMapMask.MaskBufferLeft != null && UIMapMask.MaskBufferRight != null;
            if (!owner.CanRetain(area?.Pointer ?? IntPtr.Zero, mask?.Pointer ?? IntPtr.Zero, usable))
            {
                EntryPoint.Log.Msg($"[FOG] Native initialization required: area={area?.Pointer}, mask={mask?.Pointer}, usable={usable}.");
                return true;
            }
            // InitUIMask only constructs the shared mask from the serialized snapshot.
            // Retaining its live instance also retains its queue, lock, buffers and texture.
            EntryPoint.Log.Msg("[FOG] Retained live mask during repeated Tab-map initialization for the same area DB.");
            return false;
        }
        catch (Exception ex) { owner.Reset(); EntryPoint.Log.Error("[FOG] Guard unavailable; original initialization runs: " + ex.Message); return true; }
    }
    private static void AfterInit()
    {
        try
        {
            var area = PlayerDataManager.Instance.Data?.PlayerAreaDB;
            var mask = MapMaskManager.Instance.MapMask;
            owner.Bind(area?.Pointer ?? IntPtr.Zero, mask?.Pointer ?? IntPtr.Zero);
            EntryPoint.Log.Msg($"[FOG] Bound live mask: area={area?.Pointer}, mask={mask?.Pointer}.");
        }
        catch (Exception ex) { owner.Reset(); EntryPoint.Log.Error("[FOG] Cannot bind mask owner: " + ex.Message); }
    }
}

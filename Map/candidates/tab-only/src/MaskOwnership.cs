namespace Restitutor.Map;

internal sealed class MaskOwnership
{
    private IntPtr area, mask;
    internal void Bind(IntPtr areaId, IntPtr maskId) { area = areaId; mask = maskId; }
    internal void Reset() { area = mask = IntPtr.Zero; }
    internal bool CanRetain(IntPtr areaId, IntPtr maskId, bool usable) =>
        usable && area != IntPtr.Zero && mask != IntPtr.Zero && area == areaId && mask == maskId;
}

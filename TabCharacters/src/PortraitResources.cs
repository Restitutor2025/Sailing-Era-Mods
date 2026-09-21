using Il2CppCharacter;
using Il2CppCore.NewUISystem;
using Il2CppFairyGUI;

namespace Restitutor.TabCharacters;

internal static class PortraitResources
{
    internal readonly record struct Result(int Rows, int Urls, int Pending)
    {
        public static Result operator +(Result a, Result b) => new(a.Rows + b.Rows, a.Urls + b.Urls, a.Pending + b.Pending);
    }

    internal static Result ClearLoader(GLoader? loader)
    {
        if (loader == null || loader.isDisposed) return default;
        var custom = loader.TryCast<MyGLoader>();
        int urls = string.IsNullOrEmpty(loader.url) ? 0 : 1;
        // Preserve the original ClearContent/virtual FreeExternal path for completed loads.
        if (urls != 0) loader.url = string.Empty;
        int pending = 0;
        // ClearContent can skip FreeExternal before a texture has arrived. Release only a remaining
        // operation; completed-load release has already nulled the handler, preventing double release.
        if (custom != null && custom._handler != null)
        {
            custom.ReleaseLoader();
            pending = 1;
        }
        return new Result(0, urls, pending);
    }

    internal static Result ClearPool(GList list)
    {
        if (list.isDisposed) return default;
        var pool = list.itemPool?._pool;
        if (pool == null) return default;
        Result result = default;
        foreach (var pair in pool)
        {
            var queue = pair.Value;
            if (queue == null) continue;
            foreach (var item in queue)
            {
                // Only detached portrait rows from this exact list's pool. Never traverse general UI.
                if (item == null || item.isDisposed || item.parent != null) continue;
                var row = item.TryCast<UIbtnRole>();
                if (row == null) continue;
                result += ClearLoader(row.loaderRole) + new Result(1, 0, 0);
            }
        }
        return result;
    }
}

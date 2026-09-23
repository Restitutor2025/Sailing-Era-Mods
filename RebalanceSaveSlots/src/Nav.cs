using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using Il2CppFairyGUI;
using Il2CppUISystem;
using Il2CppClient.UILogic.UISystem;
using Il2CppClient.PlayerStore.StorageHistory;

namespace Restitutor.RebalanceSaveSlots;

// Save/load list navigation (0.2.0).
//  - Focus: the game's UIStorageView.ShowHook (0x6635C0) always selects row 0 (GetChildAt(0).selected = true,
//    model.SelectedStorageIndex = 0, ScrollToView(0)). With 101 rows the user wants the slot they last used.
//  - Q / E: the save screen has no L1/R1 action of its own (UISystemCtrl has OnAction_A/B/Y only), so the
//    keys are free; one press moves the selection 5 rows and puts it at the top of the list (page turn).
//    Selecting a row is what the game's own click handler does (OnStorageItemIndexChanged 0x6633F0:
//    model.SelectedStorageIndex = row; no Refresh), so save/load act on the selected row as before.
//  - Hint: the game's bottom tip bar is table-driven (UIOperationTips), so the mod draws its own line
//    under the list instead.
internal static class Nav
{
    private static MelonLogger.Instance log = null!;
    private static UIStorageView? view;
    private static GList? list;
    private static UISystemModel? model;
    private static Hint? hint;
    private static int lastUsed = -1;      // slot saved to / loaded from in this run
    private static bool pendingFocus;
    private static bool geometryLogged;

    internal static void Init(MelonLogger.Instance logger) => log = logger;

    /// <summary>UISystemCtrl.ReadStorage / SaveStorage ran: remember the slot.</summary>
    internal static void Used(UISystemCtrl ctrl, string what)
    {
        int i = ctrl?.Model?.SelectedStorageIndex ?? -1;
        if (i < 0) return;
        if (lastUsed != i) log.Msg($"{what}: slot {i} is now the one the screen opens on.");
        lastUsed = i;
    }

    internal static void Shown(UIStorageView v)
    {
        view = v; pendingFocus = true;
    }

    internal static void Hidden()
    {
        view = null; list = null; model = null;
        hint?.Dispose(); hint = null;
    }

    /// <summary>After UIStorageView.Refresh and the mod's row count fix.</summary>
    internal static void Refreshed(UIStorageView v, UIStoragePanel panel, GList gl)
    {
        view = v; list = gl; model = v._model;
        if (hint == null || !hint.Alive) { hint?.Dispose(); hint = new Hint(panel, gl, log, ref geometryLogged); }
        if (!pendingFocus) return;
        pendingFocus = false;
        int latest = -1;
        try { latest = StorageHistoryManager.Instance?.GetLatestStorageIndex() ?? -1; } catch { }
        int row = Rules.FocusRow(lastUsed, latest, gl.numItems);
        if (row > 0) Select(row);
    }

    private static void Select(int row)
    {
        var gl = list; var m = model;
        if (gl == null || gl.isDisposed || m == null || row < 0 || row >= gl.numItems) return;
        m.SelectedStorageIndex = row;
        if (gl.selectedIndex != row) gl.selectedIndex = row;
        gl.ScrollToView(row, false, true);   // selected row goes to the top of the list
    }

    private static int keyFrame = -1;
    private static string? keyLogged;

    /// <summary>Q / E (gamepad L1 / R1): 5 rows per press. Never swallows the input.</summary>
    internal static bool OnKey(InputAction.CallbackContext c)
    {
        var gl = list; var v = view;
        if (gl == null || gl.isDisposed || v == null || model == null || !v._isShowing) return true;
        string n = c.action?.name ?? "";
        int dir = n is "Action_L1" or "Action_LB" ? -1 : n is "Action_R1" or "Action_RB" ? 1 : 0;
        if (dir == 0 || !c.performed || !c.ReadValueAsButton()) return true;
        int f = Time.frameCount * 2 + (dir > 0 ? 1 : 0);
        if (f == keyFrame) return true;      // one step per press even if two action names fire
        keyFrame = f;
        if (keyLogged != n) { keyLogged = n; log.Msg($"storage list: {n} -> {Rules.Page} rows {(dir < 0 ? "up" : "down")}."); }
        int row = Rules.Paged(model.SelectedStorageIndex, dir, gl.numItems);
        if (row >= 0) Select(row);
        return true;
    }

    // One line under the list: [Q] 이전 5칸 ... 다음 5칸 [E]. Key icons are the game's own
    // (IconUtils.GetInputKeyIcon(5) = L1/Q, (6) = R1/E) and only load through the game's loader class.
    private sealed class Hint : IDisposable
    {
        private readonly GComponent panel;
        private bool disposed;
        public bool Alive => !disposed && !panel.isDisposed && panel.parent != null;

        public Hint(GComponent host, GList gl, MelonLogger.Instance log, ref bool logged)
        {
            float width = Math.Min(gl.width, 560f), height = 34f;
            float x = gl.x + (gl.width - width) / 2f;
            float y = gl.y + gl.height + 6f;
            if (y + height > host.height) y = gl.y + gl.height - height - 4f;
            panel = new GComponent { touchable = false, sortingOrder = int.MaxValue };
            panel.SetSize(width, height);
            panel.SetXY(x, y);
            host.AddChild(panel);
            var bg = new GGraph { touchable = false };
            bg.SetXY(0, 0); bg.SetSize(width, height);
            bg.DrawRect(width, height, 0, Color.clear, new Color(.08f, .09f, .11f, .72f));
            panel.AddChild(bg);
            float icon = height - 10f;
            bool q = Icon(4f, icon, 5), e = Icon(width - icon - 4f, icon, 6);
            Text(icon + 10f, width / 2f - icon - 14f, AlignType.Left, q ? "이전 5칸" : "Q  이전 5칸");
            Text(width / 2f + 4f, width / 2f - icon - 14f, AlignType.Right, e ? "다음 5칸" : "다음 5칸  E");
            if (!logged)
            {
                logged = true;
                log.Msg($"storage list: panel {host.width:0}x{host.height:0}, list ({gl.x:0},{gl.y:0},{gl.width:0}x{gl.height:0}), hint ({x:0},{y:0},{width:0}x{height:0}), key icons {(q ? "Q" : "-")}/{(e ? "E" : "-")}.");
            }
        }

        private bool Icon(float x, float size, int key)
        {
            try
            {
                string url = Il2CppClient.Utils.IconUtils.GetInputKeyIcon(key);
                if (string.IsNullOrEmpty(url)) return false;
                var loader = UIObjectFactory.NewObject(ObjectType.Loader)?.TryCast<GLoader>()
                             ?? new Il2CppCore.NewUISystem.MyGLoader();
                loader.touchable = false; loader.autoSize = false;
                loader.fill = FillType.ScaleMatchHeight; loader.align = AlignType.Center; loader.verticalAlign = VertAlignType.Middle;
                loader.SetSize(size, size); loader.SetXY(x, 5f);
                panel.AddChild(loader);
                loader.url = url;
                return true;
            }
            catch { return false; }
        }

        private void Text(float x, float w, AlignType align, string s)
        {
            var t = new GTextField { text = s, touchable = false, singleLine = true, autoSize = AutoSizeType.None };
            t.textFormat = new TextFormat { size = 17, color = new Color(.92f, .90f, .82f), align = align };
            t.verticalAlign = VertAlignType.Middle;
            t.SetXY(x, 0); t.SetSize(w, 34);
            panel.AddChild(t);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { if (!panel.isDisposed) panel.Dispose(); } catch { }
        }
    }
}

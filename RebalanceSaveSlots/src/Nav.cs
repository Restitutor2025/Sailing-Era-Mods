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
//    under the "저장 수: N/101" text (user 2026-09-23): [Q key] [<] 5칸씩 이동 [>] [E key], with the key icons
//    and the arrows the game's own quantity popup uses (UICompInputNum.btnReduce / btnAdd) and the same
//    text style as that count text.
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
        if (hint == null || !hint.Alive) { hint?.Dispose(); hint = new Hint(panel, log, ref geometryLogged); }
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

    // One line under the count text: [Q] [<] 5칸씩 이동 [>] [E].
    // Key icons: IconUtils.GetInputKeyIcon(5) = L1/Q, (6) = R1/E (only the game's loader class loads them).
    // Arrows: the same package items as the quantity popup's - / + buttons (UICompInputNum.btnReduce/btnAdd).
    private sealed class Hint : IDisposable
    {
        private const float Icon = 26f, Arrow = 22f, Height = 30f, Width = 320f;
        private static string? arrowLeftUrl, arrowRightUrl;
        private readonly GComponent panel;
        private bool disposed;
        public bool Alive => !disposed && !panel.isDisposed && panel.parent != null;

        public Hint(UIStoragePanel storage, MelonLogger.Instance log, ref bool logged)
        {
            var anchor = storage.txtStorageNum;              // "저장 수: N/101"
            var host = anchor?.parent ?? storage;
            float x = anchor != null ? anchor.x + (anchor.width - Width) / 2f : (host.width - Width) / 2f;
            float y = anchor != null ? anchor.y + anchor.height + 10f : host.height / 2f;
            panel = new GComponent { touchable = false, sortingOrder = int.MaxValue };
            panel.SetSize(Width, Height);
            panel.SetXY(x, y);
            host.AddChild(panel);

            var format = new TextFormat();
            if (anchor != null) format.CopyFrom(anchor.textFormat);
            else { format.size = 17; format.color = new Color(.92f, .90f, .82f); }
            format.align = AlignType.Center;

            FindArrows();
            bool q = KeyIcon(0f, 5), e = KeyIcon(Width - Icon, 6);
            bool left = ArrowIcon(Icon + 6f, arrowLeftUrl), right = ArrowIcon(Width - Icon - Arrow - 6f, arrowRightUrl);
            string text = (q ? "" : "Q ") + (left ? "" : "< ") + "5칸씩 이동" + (right ? "" : " >") + (e ? "" : " E");
            Text(Icon + Arrow + 10f, Width - 2f * (Icon + Arrow + 10f), format, text);
            if (!logged)
            {
                logged = true;
                log.Msg($"storage hint: count text {(anchor == null ? "-" : $"({anchor.x:0},{anchor.y:0},{anchor.width:0}x{anchor.height:0})")}, hint ({x:0},{y:0},{Width:0}x{Height:0}), keys {(q ? "Q" : "-")}/{(e ? "E" : "-")}, arrows {(left ? "<" : "-")}/{(right ? ">" : "-")}.");
            }
        }

        // The quantity popup's − / + buttons are package items; create one more of each for the hint.
        private static void FindArrows()
        {
            if (arrowLeftUrl != null && arrowRightUrl != null) return;
            GObject? obj = null;
            try
            {
                string url = Il2CppCommonPrompt.UICompInputNum.URL;
                if (string.IsNullOrEmpty(url)) return;
                obj = UIPackage.CreateObjectFromURL(url);
                var comp = obj?.TryCast<Il2CppCommonPrompt.UICompInputNum>();
                if (comp == null) return;
                arrowLeftUrl = comp.btnReduce?.resourceURL;
                arrowRightUrl = comp.btnAdd?.resourceURL;
            }
            catch { }
            finally { try { obj?.Dispose(); } catch { } }
        }

        private bool ArrowIcon(float x, string? url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            try
            {
                var o = UIPackage.CreateObjectFromURL(url);
                if (o == null) return false;
                o.touchable = false;
                o.SetSize(Arrow, Arrow);
                o.SetXY(x, (Height - Arrow) / 2f);
                panel.AddChild(o);
                return true;
            }
            catch { return false; }
        }

        private bool KeyIcon(float x, int key)
        {
            try
            {
                string url = Il2CppClient.Utils.IconUtils.GetInputKeyIcon(key);
                if (string.IsNullOrEmpty(url)) return false;
                var loader = UIObjectFactory.NewObject(ObjectType.Loader)?.TryCast<GLoader>()
                             ?? new Il2CppCore.NewUISystem.MyGLoader();
                loader.touchable = false; loader.autoSize = false;
                loader.fill = FillType.ScaleMatchHeight; loader.align = AlignType.Center; loader.verticalAlign = VertAlignType.Middle;
                loader.SetSize(Icon, Icon); loader.SetXY(x, (Height - Icon) / 2f);
                panel.AddChild(loader);
                loader.url = url;
                return true;
            }
            catch { return false; }
        }

        private void Text(float x, float w, TextFormat format, string s)
        {
            var t = new GTextField { text = s, touchable = false, singleLine = true, autoSize = AutoSizeType.None };
            t.textFormat = format;
            t.verticalAlign = VertAlignType.Middle;
            t.SetXY(x, 0); t.SetSize(w, Height);
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

using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using Il2CppFairyGUI;
using Il2CppUISystem;
using Il2CppClient.UILogic.UISystem;
using Il2CppClient.PlayerStore.StorageHistory;

namespace Restitutor.RebalanceSaveSlots;

// Save/load list navigation.
//  - Focus (0.2.0): the game's UIStorageView.ShowHook (0x6635C0) always selects row 0 (GetChildAt(0).selected,
//    model.SelectedStorageIndex = 0, ScrollToView(0)). The screen opens on the slot used in this run (save or
//    load), else StorageHistoryManager.GetLatestStorageIndex (0x9CE610: newest timeOfStorage of the used rows).
//    0.2.2: applied right after ShowHook as well as on the next Refresh and logged once per open.
//  - Pages (0.2.3, user; replaces 0.2.0-0.2.2 "5칸씩 이동"): 5 rows per page, 101 rows = 21 pages. Q / E
//    (gamepad L1 / R1) go to the first row of the previous / next page; the selected row is put at the top
//    of the list. A pager under the list shows [Q] < 1 2 … 21 > [E]; the page of the selected row is
//    highlighted; numbers, arrows and key icons are clickable. Selecting a row is what the game's own handlers
//    do (OnStorageItemIndexChanged 0x6633F0 / OnListNavigationItemChanged 0x6647E0:
//    model.SelectedStorageIndex = row; no Refresh); ListNavigation.OnFindNex reads GList.selectedIndex, so
//    keyboard/gamepad navigation continues from the row the mod selects.
//  - Diagnostics (0.2.2, temporary): Q/E never reached the handler in 0.2.0/0.2.1 and S moved two rows; while
//    the screen is open the first 40 input events and navigation changes are logged.
internal static class Nav
{
    private static MelonLogger.Instance log = null!;
    private static UIStorageView? view;
    private static GList? list;
    private static UISystemModel? model;
    private static Pager? pager;
    private static int lastUsed = -1;      // slot saved to / loaded from in this run
    private static bool pendingFocus;
    private static bool geometryLogged;
    private const int DiagCap = 40;
    private static int diagInputs, diagNav, inputsSeen;

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
        view = v; model = v._model;
        var gl = v.UIContent?.listStorage;
        if (gl != null) list = gl;
        diagInputs = 0; diagNav = 0; inputsSeen = 0;
        pendingFocus = true;
        if (gl != null && gl.numItems > 0) Focus("ShowHook");
    }

    internal static void Hidden()
    {
        if (view != null) log.Msg($"[diag] storage screen closed; input events seen while open: {inputsSeen}.");
        view = null; list = null; model = null;
        // The pager stays on the (cached) storage panel for the next open; rebuilt only if that panel is gone.
    }

    /// <summary>After UIStorageView.Refresh and the mod's row count fix.</summary>
    internal static void Refreshed(UIStorageView v, UIStoragePanel panel, GList gl)
    {
        view = v; list = gl; model = v._model;
        if (pager == null || !pager.Alive) { pager?.Dispose(); pager = new Pager(panel, gl, log, ref geometryLogged); }
        pager.Update(model?.SelectedStorageIndex ?? 0, gl.numItems);
        if (!pendingFocus) return;
        pendingFocus = false;
        Focus("Refresh");
    }

    private static void Focus(string where)
    {
        var gl = list;
        if (gl == null || gl.isDisposed || model == null) return;
        int latest = -1;
        try { latest = StorageHistoryManager.Instance?.GetLatestStorageIndex() ?? -1; } catch { }
        int row = Rules.FocusRow(lastUsed, latest, gl.numItems);
        if (row >= 0) Select(row);
        log.Msg($"focus ({where}): used {lastUsed}, latest {latest}, rows {gl.numItems} -> row {row}; list selected {gl.selectedIndex}, model {model.SelectedStorageIndex}.");
    }

    private static void Select(int row)
    {
        var gl = list; var m = model;
        if (gl == null || gl.isDisposed || m == null || row < 0 || row >= gl.numItems) return;
        m.SelectedStorageIndex = row;
        if (gl.selectedIndex != row) gl.selectedIndex = row;
        gl.ScrollToView(row, false, true);   // selected row goes to the top of the list
        pager?.Update(row, gl.numItems);
    }

    /// <summary>Row picked by click (OnStorageItemIndexChanged) or keys (OnListNavigationItemChanged).</summary>
    internal static void SelectionChanged()
    {
        var gl = list;
        if (gl != null && !gl.isDisposed && model != null) pager?.Update(model.SelectedStorageIndex, gl.numItems);
    }

    private static void GoPage(int page)
    {
        var gl = list;
        if (gl == null || gl.isDisposed || model == null) return;
        int row = Rules.PageStart(page, gl.numItems);
        if (row >= 0) Select(row);
    }

    private static void StepPage(int dir)
    {
        var gl = list;
        if (gl == null || gl.isDisposed || model == null) return;
        int row = Rules.PageStep(model.SelectedStorageIndex, dir, gl.numItems);
        if (row >= 0) Select(row);
    }

    /// <summary>UIStorageView.OnListNavigationItemChanged postfix (diagnostic).</summary>
    internal static void NavChanged()
    {
        if (diagNav >= DiagCap) return;
        diagNav++;
        log.Msg($"[diag] nav f={Time.frameCount} list selected {list?.selectedIndex ?? -1}, model {model?.SelectedStorageIndex ?? -1}.");
    }

    private static int keyFrame = -1;
    private static string? keyLogged;

    /// <summary>Q / E (gamepad L1 / R1): previous / next page. Never swallows the input.</summary>
    internal static bool OnKey(InputAction.CallbackContext c)
    {
        var gl = list; var v = view;
        if (gl == null || gl.isDisposed || v == null || model == null) return true;
        string n = c.action?.name ?? "";
        inputsSeen++;
        if (diagInputs < DiagCap)
        {
            diagInputs++;
            bool b = false; try { b = c.ReadValueAsButton(); } catch { }
            log.Msg($"[diag] input f={Time.frameCount} {n} phase {c.phase} button {b} showing {v._isShowing}.");
        }
        if (!v._isShowing) return true;
        int dir = n is "Action_L1" or "Action_LB" ? -1 : n is "Action_R1" or "Action_RB" ? 1 : 0;
        if (dir == 0 || !c.performed || !c.ReadValueAsButton()) return true;
        int f = Time.frameCount * 2 + (dir > 0 ? 1 : 0);
        if (f == keyFrame) return true;      // one step per press even if two action names fire
        keyFrame = f;
        if (keyLogged != n) { keyLogged = n; log.Msg($"storage list: {n} -> {(dir < 0 ? "previous" : "next")} page."); }
        StepPage(dir);
        return true;
    }

    // Under the list, centred: [Q] < 1 2 … 21 > [E].
    // Key icons: IconUtils.GetInputKeyIcon(5) = L1/Q, (6) = R1/E, 65 px (user: 2.5x); only the game's loader
    // class loads them. Arrows: Common package (loaded at launch by GameLaunch.InitCommonRes) image
    // ui_common_arrow_02, 22x30, pointing right; the left one is flipped (user's pick). Numbers: the count text's
    // format ("저장 수: N/101"); the current page in gold. Built once per storage panel (the panel is kept by
    // the game between opens), so its click callbacks are created once.
    private sealed class Pager : IDisposable
    {
        private const float Key = 65f, KeyGap = 10f, ArrowGap = 18f, NumGap = 20f;
        private static readonly List<Il2CppSystem.Object> keep = new();   // kept alive: native listeners hold them (built once per panel)
        private static readonly Color Gold = new(1f, .82f, .38f);
        private readonly GComponent panel;
        private readonly List<GTextField> numbers = new();
        private readonly TextFormat normal, current;
        private int shownPage = -1, shownPages = -1;
        private bool disposed;
        public bool Alive => !disposed && !panel.isDisposed && panel.parent != null;

        public Pager(UIStoragePanel storage, GList gl, MelonLogger.Instance log, ref bool logged)
        {
            var host = gl.parent ?? storage;
            panel = new GComponent { sortingOrder = int.MaxValue };
            host.AddChild(panel);

            normal = new TextFormat();
            var anchor = storage.txtStorageNum;
            if (anchor != null) normal.CopyFrom(anchor.textFormat);
            else { normal.size = 28; normal.color = new Color(.92f, .90f, .82f); }
            normal.align = AlignType.Center;
            current = new TextFormat(); current.CopyFrom(normal); current.color = Gold; current.bold = true;

            int pages = Rules.PageCount(gl.numItems);
            var q = KeyIcon(5, "Q"); var left = ArrowIcon(true);
            for (int i = 0; i < pages; i++) numbers.Add(Number(i));
            var right = ArrowIcon(false); var e = KeyIcon(6, "E");
            Click(q, () => StepPage(-1)); Click(left, () => StepPage(-1));
            Click(right, () => StepPage(1)); Click(e, () => StepPage(1));

            float h = Key;
            foreach (var n in numbers) h = Math.Max(h, n.height);
            float x = 0;
            x = Place(q, x, h) + KeyGap;
            x = Place(left, x, h) + ArrowGap;
            for (int i = 0; i < numbers.Count; i++) x = Place(numbers[i], x, h) + (i < numbers.Count - 1 ? NumGap : ArrowGap);
            x = Place(right, x, h) + KeyGap;
            x = Place(e, x, h);
            panel.SetSize(x, h);
            // Centred under the list, vertically on the list's bottom edge + 20 (the gap above the frame border).
            float px = gl.x + (gl.width - x) / 2f, py = gl.y + gl.height + 20f - h / 2f;
            panel.SetXY(px, py);
            if (!logged)
            {
                logged = true;
                log.Msg($"storage pager: list ({gl.x:0},{gl.y:0},{gl.width:0}x{gl.height:0}) in {host.width:0}x{host.height:0}, pager ({px:0},{py:0},{x:0}x{h:0}), pages {pages}, keys {Kind(q)}/{Kind(e)}, arrows {Kind(left)}/{Kind(right)}.");
            }
        }

        /// <summary>Highlight the page of <paramref name="row"/>; cheap when nothing changed.</summary>
        public void Update(int row, int count)
        {
            if (!Alive) return;
            int page = Rules.PageOf(row), pages = Rules.PageCount(count);
            if (page == shownPage && pages == shownPages) return;
            shownPage = page; shownPages = pages;
            for (int i = 0; i < numbers.Count; i++)
            {
                var t = numbers[i];
                if (t.isDisposed) continue;
                t.visible = i < pages;
                t.textFormat = i == page ? current : normal;
            }
        }

        private static string Kind(GObject o) => o.TryCast<GTextField>() != null ? "text" : "image";

        private float Place(GObject o, float x, float h)
        {
            o.SetXY(x, (h - o.height) / 2f);
            panel.AddChild(o);
            return x + o.width;
        }

        private GTextField Number(int page)
        {
            var t = Label((page + 1).ToString());
            Click(t, () => GoPage(page));
            return t;
        }

        private GTextField Label(string s)
        {
            var t = new GTextField { singleLine = true, autoSize = AutoSizeType.Both };
            t.textFormat = normal;
            t.text = s;
            return t;
        }

        private static void Click(GObject o, Action act)
        {
            o.touchable = true;
            var cb = (EventCallback1)(e => { try { e.StopPropagation(); act(); } catch { } });
            keep.Add(cb);
            o.onClick.Add(cb);
        }

        private GObject ArrowIcon(bool left)
        {
            try
            {
                var img = UIPackage.CreateObject("Common", "ui_common_arrow_02")?.TryCast<GImage>();
                if (img != null)
                {
                    if (left) img.flip = FlipType.Horizontal;
                    return img;
                }
            }
            catch { }
            return Label(left ? "<" : ">");
        }

        private GObject KeyIcon(int key, string letter)
        {
            try
            {
                string url = Il2CppClient.Utils.IconUtils.GetInputKeyIcon(key);
                if (!string.IsNullOrEmpty(url))
                {
                    var loader = UIObjectFactory.NewObject(ObjectType.Loader)?.TryCast<GLoader>()
                                 ?? new Il2CppCore.NewUISystem.MyGLoader();
                    loader.autoSize = false;
                    loader.fill = FillType.ScaleMatchHeight; loader.align = AlignType.Center; loader.verticalAlign = VertAlignType.Middle;
                    loader.SetSize(Key, Key);
                    loader.url = url;
                    return loader;
                }
            }
            catch { }
            return Label(letter);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { if (!panel.isDisposed) panel.Dispose(); } catch { }
        }
    }
}

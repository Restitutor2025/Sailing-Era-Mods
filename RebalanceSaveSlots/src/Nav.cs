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
//    0.2.2: applied right after ShowHook as well as on the next Refresh (0.2.1 only waited for a Refresh,
//    which did not move the focus from the title screen; user 2026-09-23) and logged once per open.
//  - Q / E: the save screen has no L1/R1 action of its own (UISystemCtrl has OnAction_A/B/Y only); one press
//    moves the selection 5 rows and puts it at the top of the list. Selecting a row is what the game's own
//    handlers do (OnStorageItemIndexChanged 0x6633F0 / OnListNavigationItemChanged 0x6647E0:
//    model.SelectedStorageIndex = row; no Refresh). ListNavigation.OnFindNex reads GList.selectedIndex, so
//    keyboard/gamepad navigation continues from the row the mod selects.
//  - Hint (0.2.2, user): under "저장 수: N/101": [Q key] [<] 5칸씩 이동 [>] [E key]; key icons 2.5x (65),
//    arrows = Common package image ui_common_arrow_02 (left one flipped), text style copied from the count text.
//  - Diagnostics (0.2.2, temporary): Q/E never reached the handler in 0.2.0/0.2.1 (no log line) and S moved two
//    rows; while the screen is open the first 40 input events and navigation changes are logged.
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
        hint?.Dispose(); hint = null;
    }

    /// <summary>After UIStorageView.Refresh and the mod's row count fix.</summary>
    internal static void Refreshed(UIStorageView v, UIStoragePanel panel, GList gl)
    {
        view = v; list = gl; model = v._model;
        if (hint == null || !hint.Alive) { hint?.Dispose(); hint = new Hint(panel, log, ref geometryLogged); }
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

    /// <summary>Q / E (gamepad L1 / R1): 5 rows per press. Never swallows the input.</summary>
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
        if (keyLogged != n) { keyLogged = n; log.Msg($"storage list: {n} -> {Rules.Page} rows {(dir < 0 ? "up" : "down")}."); }
        int row = Rules.Paged(model.SelectedStorageIndex, dir, gl.numItems);
        if (row >= 0) Select(row);
        return true;
    }

    // One line under the count text: [Q] [<] 5칸씩 이동 [>] [E], centred on it.
    // Key icons: IconUtils.GetInputKeyIcon(5) = L1/Q, (6) = R1/E; only the game's loader class loads them.
    // Arrows: Common package (loaded at launch by GameLaunch.InitCommonRes) item ui_common_arrow_02, 22x30,
    // pointing right; the left one is the same image flipped.
    private sealed class Hint : IDisposable
    {
        private const float Key = 65f, KeyGap = 10f, Gap = 16f;
        private readonly GComponent panel;
        private bool disposed;
        public bool Alive => !disposed && !panel.isDisposed && panel.parent != null;

        public Hint(UIStoragePanel storage, MelonLogger.Instance log, ref bool logged)
        {
            var anchor = storage.txtStorageNum;              // "저장 수: N/101"
            var host = anchor?.parent ?? storage;
            panel = new GComponent { touchable = false, sortingOrder = int.MaxValue };
            host.AddChild(panel);

            var format = new TextFormat();
            if (anchor != null) format.CopyFrom(anchor.textFormat);
            else { format.size = 28; format.color = new Color(.92f, .90f, .82f); }
            format.align = AlignType.Left;

            var q = KeyIcon(5, "Q", format); var left = ArrowIcon(true, format);
            var text = Label("5칸씩 이동", format);
            var right = ArrowIcon(false, format); var e = KeyIcon(6, "E", format);

            float h = Math.Max(Key, text.height);
            float x = 0;
            x = Place(q, x, h) + KeyGap;
            x = Place(left, x, h) + Gap;
            x = Place(text, x, h) + Gap;
            x = Place(right, x, h) + KeyGap;
            x = Place(e, x, h);
            panel.SetSize(x, h);
            float px = anchor != null ? anchor.x + (anchor.width - x) / 2f : (host.width - x) / 2f;
            float py = anchor != null ? anchor.y + anchor.height + 6f : host.height / 2f;
            panel.SetXY(px, py);
            if (!logged)
            {
                logged = true;
                log.Msg($"storage hint: count text {(anchor == null ? "-" : $"({anchor.x:0},{anchor.y:0},{anchor.width:0}x{anchor.height:0})")}, hint ({px:0},{py:0},{x:0}x{h:0}), keys {Kind(q)}/{Kind(e)}, arrows {Kind(left)}/{Kind(right)} {left.width:0}x{left.height:0}, text {text.width:0}x{text.height:0}.");
            }
        }

        private static string Kind(GObject o) => o.TryCast<GTextField>() != null ? "text" : "image";

        private float Place(GObject o, float x, float h)
        {
            o.SetXY(x, (h - o.height) / 2f);
            panel.AddChild(o);
            return x + o.width;
        }

        private static GTextField Label(string s, TextFormat format)
        {
            var t = new GTextField { touchable = false, singleLine = true, autoSize = AutoSizeType.Both };
            t.textFormat = format;
            t.text = s;
            return t;
        }

        private static GObject ArrowIcon(bool left, TextFormat format)
        {
            try
            {
                var img = UIPackage.CreateObject("Common", "ui_common_arrow_02")?.TryCast<GImage>();
                if (img != null)
                {
                    img.touchable = false;
                    if (left) img.flip = FlipType.Horizontal;
                    return img;
                }
            }
            catch { }
            return Label(left ? "<" : ">", format);
        }

        private static GObject KeyIcon(int key, string letter, TextFormat format)
        {
            try
            {
                string url = Il2CppClient.Utils.IconUtils.GetInputKeyIcon(key);
                if (!string.IsNullOrEmpty(url))
                {
                    var loader = UIObjectFactory.NewObject(ObjectType.Loader)?.TryCast<GLoader>()
                                 ?? new Il2CppCore.NewUISystem.MyGLoader();
                    loader.touchable = false; loader.autoSize = false;
                    loader.fill = FillType.ScaleMatchHeight; loader.align = AlignType.Center; loader.verticalAlign = VertAlignType.Middle;
                    loader.SetSize(Key, Key);
                    loader.url = url;
                    return loader;
                }
            }
            catch { }
            return Label(letter, format);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { if (!panel.isDisposed) panel.Dispose(); } catch { }
        }
    }
}

using Il2CppFairyGUI;
using UnityEngine;
namespace Restitutor.Contribution;

// Owns only its components; never replaces the game's focus controller.
internal static class Overlay
{
    private static GComponent? root, modal, viewport, cards, hudRows;
    private static GImage? hudArt;
    private static GTextField? hudTitle;
    private static GGraph? scrollThumb, scrollTrack;
    private static int revision = -1;
    private static string lastStatus = "";
    private static float screenWidth, screenHeight, dragStart, scrollStart;
    private static bool dragging;
    private static bool hudVisible = true;
    internal static void SetHudVisible(bool visible) { hudVisible = visible; if(root != null && !root.isDisposed) root.visible = visible && lastStatus.Length > 0; }
    private static double swallowUntil;
    private static readonly List<EventCallback1> callbacks = new();
    private static readonly Color Ink = new(.08f, .065f, .035f, 1);
    private static readonly Color Gold = new(.66f, .47f, .21f, 1);
    private static readonly Color Cream = new(1, .94f, .75f, 1);
    internal static bool BlocksInput => modal?.visible == true || EntryPoint.Now < swallowUntil;
    internal static void SetVisible(bool visible)
    {
        if (root != null && !root.isDisposed) root.visible = visible && hudVisible;
        if (modal != null && !modal.isDisposed) modal.visible = visible && EntryPoint.Journal.Notices.Count > 0;
        if (!visible) dragging = false;
    }
    internal static void Clear()
    {
        if (root != null && !root.isDisposed) root.Dispose();
        if (modal != null && !modal.isDisposed) modal.Dispose();
        UiAssets.ReleaseUnusedMaterials();
        root = modal = viewport = cards = hudRows = null; hudArt = null; hudTitle = null;
        scrollThumb = scrollTrack = null; callbacks.Clear(); revision = -1; lastStatus = "";
        dragging = false; swallowUntil = 0;
    }
    private static GGraph Rect(GComponent parent, float x, float y, float w, float h, Color fill, bool touch = false)
    {
        var graph = new GGraph { touchable = touch };
        graph.SetXY(x, y); graph.DrawRect(w, h, 0, fill, fill); parent.AddChild(graph); return graph;
    }
    private static GGraph Round(GComponent parent, float x, float y, float w, float h, Color fill)
    {
        var graph = new GGraph { touchable = false }; graph.SetXY(x, y);
        graph.DrawRoundRect(w, h, fill, new float[] { 12, 12, 12, 12 }); parent.AddChild(graph); return graph;
    }
    private static GImage Art(GComponent parent, NTexture texture, float x, float y, float w, float h)
    {
        var image = new GImage { touchable = false };
        UiAssets.Bind(image, texture, parent == cards);
        image.SetXY(x, y); image.SetSize(w, h); parent.AddChild(image); return image;
    }
    private static GTextField Text(GComponent parent, string value, float x, float y, float w, int size = 26, bool center = false, bool light = false)
    {
        var text = new GTextField { touchable = false, autoSize = AutoSizeType.Height, singleLine = false };
        var format = text.textFormat; format.font = UIConfig.defaultFont; format.size = size;
        format.color = light ? Cream : Ink; format.lineSpacing = 5; format.bold = true;
        format.align = center ? AlignType.Center : AlignType.Left;
        text.textFormat = format; text.SetXY(x, y); text.SetSize(w, 30); text.text = value; parent.AddChild(text); return text;
    }
    private static void Listen(EventListener listener, Action<EventContext> action)
    {
        EventCallback1 callback = (EventCallback1)action; callbacks.Add(callback); listener.Add(callback);
    }
    internal static void Draw(string value)
    {
        var stage = GRoot.inst;
        if (root == null || root.isDisposed || root.parent?.Pointer != stage.Pointer || screenWidth != stage.width || screenHeight != stage.height)
        {
            Clear(); screenWidth = stage.width; screenHeight = stage.height;
            root = new GComponent { name = "RestitutorContributionStatus", touchable = false, sortingOrder = 20000 };
            stage.AddChild(root);
            hudArt = Art(root, UiAssets.Get("city-panel"), 0, 0, 900, 540);
            hudTitle = Text(root, "도시 혜택", 150, 86, 600, 48, true, true);
            hudRows = new GComponent { touchable = false }; root.AddChild(hudRows);
            BuildModal(stage);
        }
        if (lastStatus != value) { BuildHud(value); lastStatus = value; }
        root.visible = hudVisible && value.Length > 0;
        if (revision != EntryPoint.Journal.Revision) RefreshCards();
        modal!.visible = EntryPoint.Journal.Notices.Count > 0;
    }
    private static void BuildHud(string value)
    {
        hudRows!.RemoveChildren(0, -1, true);
        UiAssets.ReleaseUnusedMaterials();
        float y = 0;
        foreach (string line in value.Split('\n').Skip(1))
        {
            var pair = line.Split(new[] { "  " }, 2, StringSplitOptions.None);
            if (pair.Length != 2) continue;
            string label = pair[0], state = pair[1];
            bool neutral = label == "세금" && state == "일반";
            Color fill = state.StartsWith("잠김") || state == "없음" ? new(.35f, .29f, .22f, 1) :
                label == "교역 허가" ? new(.36f, .09f, .055f, 1) :
                label == "투자 가능" ? new(.035f, .23f, .34f, 1) :
                neutral ? new(.88f, .76f, .53f, 1) : new(.025f, .29f, .23f, 1);
            var stateText = Text(hudRows, state, 518, y + 12, 244, 31, true, !neutral);
            float h = Math.Max(76, stateText.height + 26);
            var border = Round(hudRows, 504, y + 4, 272, h - 8, Gold);
            var badge = Round(hudRows, 508, y + 8, 264, h - 16, fill);
            hudRows.SetChildIndex(border, hudRows.GetChildIndex(stateText));
            hudRows.SetChildIndex(badge, hudRows.GetChildIndex(stateText));
            stateText.y = y + (h - stateText.height) / 2;
            int icon = label == "교역 허가" ? 1 : label == "투자 가능" ? 3 : label == "세금" ? 4 : 2;
            Art(hudRows, UiAssets.Icon(icon), 164, y + (h - 64) / 2, 74, 64);
            var title = Text(hudRows, label, 264, y, 232, 34);
            title.y = y + (h - title.height) / 2;
            y += h; Rect(hudRows, 162, y, 620, 1, new Color(.56f, .43f, .25f, .5f));
        }
        float height = Math.Max(540, y / .63f);
        hudRows.y = height * .30f; hudRows.SetSize(900, y);
        hudArt!.SetSize(900, height); hudTitle!.y = height * .16f;
        root!.SetSize(900, height);
        float scale = Math.Min(Math.Min(520, screenWidth * .34f) / 900, screenHeight * .48f / height);
        root.SetScale(scale, scale); root.SetXY((screenWidth - 900 * scale) / 2, 12);
    }
    private static void BuildModal(GRoot stage)
    {
        modal = new GComponent { name = "RestitutorContributionNotice", sortingOrder = 30000 };
        modal.SetSize(screenWidth, screenHeight); stage.AddChild(modal);
        Rect(modal, 0, 0, screenWidth, screenHeight, new Color(0, 0, 0, .55f), true);
        var panel = new GComponent(); panel.SetSize(720, 780); modal.AddChild(panel);
        float scale = Math.Min(1, Math.Min((screenWidth - 32) / 720, (screenHeight - 32) / 780));
        panel.SetScale(scale, scale); panel.SetXY((screenWidth - 720 * scale) / 2, (screenHeight - 780 * scale) / 2);
        Art(panel, UiAssets.Get("notice-panel"), 0, 0, 720, 780);
        Text(panel, "알림", 180, 91, 360, 38, true, true);
        viewport = new GComponent { name = "ContributionScroll" };
        viewport.SetXY(72, 168); viewport.SetSize(560, 438); viewport.SetupOverflow(OverflowType.Hidden); panel.AddChild(viewport);
        Rect(viewport, 0, 0, 560, 438, new Color(0, 0, 0, 0), true);
        cards = new GComponent { touchable = false }; viewport.AddChild(cards);
        scrollTrack = Round(panel, 647, 172, 16, 430, new Color(.30f, .23f, .13f, .65f));
        scrollThumb = Round(panel, 648, 172, 14, 60, new Color(.92f, .63f, .18f, 1));
        Listen(viewport.onTouchBegin, ctx => {
            dragging = true; dragStart = viewport.GlobalToLocal(new Vector2(ctx.inputEvent.x, ctx.inputEvent.y)).y;
            scrollStart = cards.y; ctx.CaptureTouch(); ctx.StopPropagation();
        });
        Listen(viewport.onTouchMove, ctx => {
            if (!dragging) return;
            Scroll(scrollStart + viewport.GlobalToLocal(new Vector2(ctx.inputEvent.x, ctx.inputEvent.y)).y - dragStart); ctx.StopPropagation();
        });
        Listen(viewport.onTouchEnd, ctx => { dragging = false; ctx.StopPropagation(); });
        Text(panel, "위아래로 드래그하여 확인", 110, 611, 500, 20, true);
        var button = new GComponent(); button.SetXY(230, 653); button.SetSize(260, 55); panel.AddChild(button);
        Rect(button, 0, 0, 260, 55, new Color(0, 0, 0, 0), true);
        var feedback = Round(button, 8, 5, 244, 43, new Color(1, 1, 1, 0));
        var label = Text(button, "확인", 4, 4, 252, 32, true, true);
        bool hovered = false, pressed = false;
        void ButtonState()
        {
            feedback.DrawRoundRect(244, 43, pressed ? new Color(0, 0, 0, .28f) : new Color(1, .86f, .48f, hovered ? .16f : 0), new float[] {12,12,12,12});
            label.y = pressed ? 6 : 4;
        }
        Listen(button.onRollOver, _ => { hovered = true; ButtonState(); });
        Listen(button.onRollOut, _ => { hovered = pressed = false; ButtonState(); });
        Listen(button.onTouchBegin, ctx => { pressed = true; ButtonState(); ctx.CaptureTouch(); ctx.StopPropagation(); });
        Listen(button.onTouchEnd, ctx => { pressed = false; ButtonState(); ctx.StopPropagation(); });
        Listen(button.onClick, ctx => {
            // Sound failures must not strand the user in a modal or discard the acknowledgement.
            try { Il2CppAssets.Scripts.Core.SoundSystem.CommonSoundUtils.SpecialClick(); }
            catch (Exception ex) { EntryPoint.Error("Notice confirm sound", ex); }
            hovered = pressed = false; ButtonState();
            EntryPoint.Journal.Acknowledge(); modal.visible = false; swallowUntil = EntryPoint.Now + .2; ctx.StopPropagation();
        });
    }
    private static void Scroll(float value)
    {
        float overflow = Math.Max(0, cards!.height - viewport!.height);
        cards.y = Math.Clamp(value, -overflow, 0);
        scrollTrack!.visible = scrollThumb!.visible = overflow > 0;
        float height = Math.Max(42, 430 * viewport.height / Math.Max(viewport.height, cards.height));
        scrollThumb.SetSize(14, height); scrollThumb.y = 172 + (overflow > 0 ? -cards.y / overflow * (430 - height) : 0);
    }
    private static void RefreshCards()
    {
        cards!.RemoveChildren(0, -1, true);
        UiAssets.ReleaseUnusedMaterials();
        var rows = NoticePresentation.Rows(EntryPoint.Journal.Notices);
        bool cities = rows.Select(r => r.City).Distinct().Count() > 1;
        float y = 0;
        foreach (var row in rows)
        {
            float top = y;
            if (cities) { var city = Text(cards, row.City, 85, y + 5, 460, 18); y += city.height + 5; }
            var text = Text(cards, row.Line, 85, y + 14, 460);
            float bottom = Math.Max(top + 76, text.y + text.height + 16);
            Art(cards, UiAssets.Icon(row.Kind == 0 ? 0 : 1), 5, top + (bottom - top - 60) / 2, 68, 60);
            Rect(cards, 5, bottom, 548, 1, new Color(.56f, .43f, .25f, .55f)); y = bottom + 1;
        }
        cards.SetSize(viewport!.width, y); Scroll(cards.y); revision = EntryPoint.Journal.Revision;
    }
}


using System.Collections;
using MelonLoader;
using UnityEngine;
using Il2CppFairyGUI;
using Il2CppCommon;
using Il2CppClient.UILogic.UIBattleHp;
using Il2CppClient.WorldLogic.Entity.Component.Boat;

namespace Restitutor.RebalanceBoarding;

// 0.3.0  Boarding progress gauge, drawn directly under the sailor gauge of every PLAYER ship
//        (BoatEntityData.IsPlayer == BoatTeam.isPlayer, RVA 0x25D3F30 - flagship and escorts alike).
//        Enemy ships get none.
//
// Every player ship can enter melee; what differs is who resolves it. CheckProgressFull
// (RVA 0x25CD710) branches on
//     (self.Team.isPlayer && self.baseShipData.IsFlagship) || (target.Team.isPlayer && target.baseShipData.IsFlagship)
// -> BoardShootManager.ShootingProgressIsFull  (the melee the player actually plays)
//  else                                         -> AIMeleeBattleSimulateHandler (auto-resolved)
// so on an escort the gauge says when its auto-resolved melee is about to fire.
//
// The gauge is a second Common/proBattleSeaman instance added as a child of the ship's own
// UICompBattleHp, so the game's existing per-frame UpdateHPBar moves it along with the other two
// bars - this mod adds no per-frame work of its own. Values are written only on boarding events.
internal static class ProgressBarUI
{
    internal const float FadeSeconds = Gauge.FadeSeconds;
    private const float GapY = 1f;                          // px between the sailor bar and ours
    private static readonly Color Tint = new(1f, 0.62f, 0.13f, 1f);   // amber: not the HP green/red, not the sailor blue

    private sealed class Entry
    {
        public UIproBattleSeaman Bar = null!;
        public bool Shown;
    }

    // key: BoatEntityData pointer - shared by BoatEntityHealthBar and BoatEntityBoardShoot on the same ship
    private static readonly Dictionary<IntPtr, Entry> bars = new();
    internal static int attached, detached;

    /// <summary>Value that fills the gauge: the same number CheckProgressFull compares against.</summary>
    internal static int Threshold = Patch.OriginalThreshold;

    internal static void Clear()
    {
        foreach (var e in bars.Values) Destroy(e);
        bars.Clear();
    }

    private static void Purge()
    {
        foreach (var k in bars.Where(p => p.Value.Bar == null || p.Value.Bar.isDisposed).Select(p => p.Key).ToList()) bars.Remove(k);
    }

    private static void Destroy(Entry e)
    {
        try { if (e.Bar != null && !e.Bar.isDisposed) { e.Bar.RemoveFromParent(); e.Bar.Dispose(); } }
        catch { /* the comp may already be gone with the scene */ }
    }

    // --- attach / detach -------------------------------------------------------------------

    internal static void Attach(BoatEntityHealthBar hb)
    {
        var data = hb.boatData;
        if (data == null || !data.IsPlayer) return;
        var key = data.Pointer;
        if (bars.ContainsKey(key)) return;
        if (bars.Count >= 32) Purge();   // a ship destroyed without HideHPBar would otherwise linger
        if (!UIBattleHpManager.Instance.GetHpBar(hb.hpBarId, out var comp) || comp == null) return;
        var sailor = comp.progSailor;
        if (sailor == null) return;

        var bar = UIproBattleSeaman.CreateInstance();
        if (bar == null) return;
        bar.name = "restitutorBoardProgress";
        bar.touchable = false;
        bar.width = sailor.width;
        bar.height = sailor.height;
        bar.xy = new Vector2(sailor.x, sailor.y + sailor.height + GapY);
        bar.max = 100d;
        bar.value = 0d;
        bar.alpha = 0f;
        bar.visible = false;
        Recolor(bar);
        comp.AddChild(bar);
        bars[key] = new Entry { Bar = bar };
        attached++;
    }

    private static void Recolor(UIproBattleSeaman bar)
    {
        Paint(bar._barObjectH);
        Paint(bar._barObjectV);
        if (bar.tweenBar != null) bar.tweenBar.color = Tint;
        if (bar.flash != null) bar.flash.color = Tint;
    }

    private static void Paint(GObject? o)
    {
        if (o == null) return;
        var img = o.TryCast<GImage>();
        if (img != null) { img.color = Tint; return; }
        var comp = o.TryCast<GComponent>();
        if (comp == null) return;
        for (int i = 0; i < comp.numChildren; i++)
        {
            var child = comp.GetChildAt(i)?.TryCast<GImage>();
            if (child != null) child.color = Tint;
        }
    }

    internal static void Detach(BoatEntityHealthBar hb)
    {
        var data = hb.boatData;
        if (data == null) return;
        if (!bars.Remove(data.Pointer, out var e)) return;
        Destroy(e);
        detached++;
    }

    // --- value / visibility ----------------------------------------------------------------

    // total = this ship's progress + its target's, the exact sum CheckProgressFull compares
    // against the threshold (100 stock, 40 with NativeThreshold applied).
    internal static void Update(BoatEntityBoardShoot shoot)
    {
        if (bars.Count == 0) return;
        var data = shoot.boatData;
        if (data == null) return;
        var target = shoot.targetEnemy;
        if (!bars.TryGetValue(data.Pointer, out var e))
        {
            // the tick may belong to the enemy that is boarding one of our ships
            if (target == null) return;
            var td = target.boatData;
            if (td == null || !bars.TryGetValue(td.Pointer, out e)) return;
            (shoot, target) = (target, shoot);
        }
        int total = shoot.ShootProgress + (target != null ? target.ShootProgress : 0);
        if (!Gauge.ShouldShow(total, shoot.isMeleeBattling)) { Hide(e); return; }
        Set(e, total);
    }

    internal static void HideFor(BoatEntityBoardShoot shoot)
    {
        var data = shoot.boatData;
        if (data == null || !bars.TryGetValue(data.Pointer, out var e)) return;
        Hide(e);
    }

    private static void Set(Entry e, int total)
    {
        if (e.Bar == null || e.Bar.isDisposed) return;
        if (total <= 0) { Hide(e); return; }
        e.Bar.value = Gauge.Percent(total, Threshold);
        Show(e);
    }

    private static void Show(Entry e)
    {
        if (e.Shown || e.Bar == null || e.Bar.isDisposed) return;
        e.Shown = true;
        e.Bar.visible = true;
        e.Bar.TweenFade(1f, FadeSeconds);
    }

    private static void Hide(Entry e)
    {
        if (!e.Shown || e.Bar == null || e.Bar.isDisposed) return;
        e.Shown = false;
        e.Bar.TweenFade(0f, FadeSeconds);
        MelonCoroutines.Start(HideWhenFaded(e));
    }

    private static IEnumerator HideWhenFaded(Entry e)
    {
        yield return new WaitForSeconds(FadeSeconds);
        if (!e.Shown && e.Bar != null && !e.Bar.isDisposed) { e.Bar.visible = false; e.Bar.value = 0d; }
    }
}

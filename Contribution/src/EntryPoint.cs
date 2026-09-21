using System.Diagnostics;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Il2CppCore.InputSystem;
using MelonLoader;
using Restitutor.Core;
using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.Const;
using Il2CppClient.UILogic.UIGovHouse;
using Il2CppClient.UILogic.UIMarket;
using SceneManager = Il2CppCore.SceneSystem.SceneManager;

[assembly: MelonInfo(typeof(Restitutor.Contribution.EntryPoint), "Restitutor fixes Contribution", "0.5.6", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.Contribution;
public sealed class EntryPoint : MelonMod
{
    internal static bool Enabled;
    internal static readonly Journal Journal = new();
    internal static MelonLogger.Instance Log = null!;
    internal static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    internal static PlayerData? Player;
    private static int mainThread;
    private static double nextHud;
    // 0.5.5: the harbor main-screen check and the benefit text are recomputed on events only
    // (focus change, harbor main UI shown/hidden, city/port change, notices, influence, screen size),
    // not every frame / every 0.1 s.
    private static bool hudDirty = true, statusDirty = true, lastCity, sizeHooked;
    private static int lastPort = int.MinValue, lastRevision = int.MinValue, hudRetryFrames;
    private static void MarkHud() { hudDirty = true; statusDirty = true; hudRetryFrames = 60; }
    [ThreadStatic] private static int menuDepth;
    private static readonly HashSet<string> errors = new();
    public override void OnInitializeMelon()
    {
        Log = LoggerInstance; mainThread = Environment.CurrentManagedThreadId;
        // Everything that touches Restitutor.Core sits in Install(): without Restitutor.Core.dll in
        // UserLibs the failure surfaces here and only this mod stays disabled.
        try { Install(); }
        catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Restitutor.Core", StringComparison.Ordinal) == true)
        { Enabled = false; Log.Error("Restitutor.Core.dll is missing from UserLibs; Contribution stays disabled."); }
    }
    private static HookSet? hooks;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Install()
    {
        if (!CoreInfo.Require(Log, "0.2.0")) { Enabled = false; return; }
        hooks = new HookSet(HarmonyInstance, typeof(EntryPoint));
        if (!hooks.InstallAll(Log, "Initialization", () =>
        {
            foreach (var second in new[] { typeof(int), typeof(int).MakeByRefType() })
                Hook(typeof(WorldPortHoldDB), "UpdateInfluence", nameof(BeforeInfluence), nameof(AfterInfluence), args: new[] { typeof(int), second });
            Hook(typeof(WorldPortHoldDB), "InitHook", nameof(Reset));
            Hook(typeof(WorldPortHoldDB), "AddNaturalResources", after: nameof(AfterNaturalResource));
            Hook(typeof(PlayerPortDB), "AddEnterPort", nameof(BeforeEnterPort), nameof(AfterEnterPort));
            Hook(typeof(PlayerData), "Deserialize", nameof(BeforeLoad), nameof(AfterLoad));
            Hook(typeof(PlayerData), "Serialize", after: nameof(AfterSerialize));
            Hook(typeof(PlayerDataManager), "ProcessArchiveInitialize", after: nameof(PlayerReady));
            Hook(typeof(UIGovHouseEntryView), "RefreshMenuBtn", nameof(MenuBegin), finalizer: nameof(MenuEnd));
            Hook(typeof(FunctionOpenDB), "GetFunctionData", after: nameof(FilterMenuFunction), args: new[] { typeof(int) });
            Hook(typeof(UIGovHouseEntryCtrl), "ShowView", nameof(GuardLicence));
            Hook(typeof(GameEffectManager), "AddGameEffectInstant", nameof(EffectBefore));
            Hook(typeof(UIMarketCtrl), "SetMarketGoods", after: nameof(MarketGoods));
            // 0.5.6: shared Core input gate instead of an own prefix on InputSystemManager.OnEventCaptureInput.
            hooks!.Input("Contribution", _ => CaptureInput());
            Enabled = true;
        })) { Enabled = false; return; }
        Log.Msg("Contribution 0.5.6 loaded (Restitutor.Core " + CoreInfo.Version + ", shared input gate; harbor HUD check and benefit text on events: focus, main UI, city/port, notices). City benefits visible only on focused harbor main screen.");
    }
    // Same lookup as 0.5.5: inherited members included (declaredOnly: false), exact parameter types when given.
    private void Hook(Type type, string method, string? before = null, string? after = null, string? finalizer = null, Type[]? args = null)
        => hooks!.Hook(type, method, prefix: before, postfix: after, finalizer: finalizer, args: args, declaredOnly: false);
    internal static void Error(string context, Exception ex) { if (errors.Add(context)) Log.Error(context + ": " + ex); }
    private static void Reset()
    {
        Player = null; Journal.Clear(); Engine.Reset(); CargoDiscovery.Reset(); LiveViews.Reset(); Overlay.Clear(); UiAssets.Dispose(); nextHud = 0;
        MarkHud(); lastCity = false; lastPort = int.MinValue; lastRevision = int.MinValue;
    }
    private static void BeforeLoad() { Reset(); }
    private static void AfterLoad(PlayerData __instance, PlayerDataSerializer __0)
    {
        Player = __instance; SaveState.Load(__0);
    }
    private static void AfterSerialize(PlayerData __instance, PlayerDataSerializer __0)
    {
        if (Enabled && Player?.Pointer == __instance.Pointer) SaveState.Save(__0);
    }
    private static void PlayerReady(PlayerDataManager __instance) { Player = __instance.Data; }
    private static void BeforeEnterPort(PlayerPortDB __instance,int __0,out int? __state)
    {
        __state=null;
        if(!Enabled || Environment.CurrentManagedThreadId!=mainThread)return;
        try {
            if(PlayerDataManager.Instance?.Data?.PlayerPort?.Pointer!=__instance.Pointer)return;
            if(__instance.StayInPortId==__0 && !__instance.HasStayedPort(__0))__state=__0;
        } catch(Exception ex) {Error("Read first port entry",ex);}
    }
    private static void AfterEnterPort(PlayerPortDB __instance,int? __state)
    {
        if(!Enabled || !__state.HasValue)return;
        try {
            // IsStayInPort is set by the caller after AddEnterPort returns.
            if(__instance.StayInPortId==__state.Value && __instance.HasStayedPort(__state.Value))
                Journal.FirstVisit(__state.Value);
        } catch(Exception ex) {Error("Queue first port discovery",ex);}
    }
    private static void BeforeInfluence(WorldPortHoldDB __instance, int __0, out int? __state)
    {
        __state = null;
        if (!Enabled || Environment.CurrentManagedThreadId != mainThread) return;
        try { __state = __instance.GetPortData(__0)?.Influence; }
        catch (Exception ex) { Error("Read contribution before", ex); }
    }
    private static void AfterNaturalResource(WorldPortHoldDB __instance,int __0,int __1,bool __result)
    {
        if(!Enabled || !__result || Environment.CurrentManagedThreadId!=mainThread)return;
        try {
            var current=PlayerDataManager.Instance?.Data;
            if(current==null || Player==null || current.Pointer!=Player.Pointer)return;
            CargoDiscovery.NaturalResourceAdded(current,__instance,__0,__1,__result);
        } catch(Exception ex) {Error("Queue natural resource discovery city="+__0,ex);}
    }
    private static void AfterInfluence(WorldPortHoldDB __instance, int __0, int? __state)
    {
        if (!Enabled || !__state.HasValue) return;
        statusDirty = true;
        try { Engine.Observe(__instance, __0, __state.Value); }
        catch (Exception ex) { Error("Observe contribution", ex); }
    }
    public override void OnUpdate()
    {
        if (!Enabled || Player == null) { if (lastCity) { lastCity = false; Overlay.SetVisible(false); } return; }
        try
        {
            // Match the exact player/commander chain used by the native calculator.
            // Cached Player can outlive the manager's data during teardown/reload.
            var current = PlayerDataManager.Instance?.Data;
            if (current == null || current.Pointer != Player.Pointer ||
                current.PlayerCommander?.CommanderData == null ||
                current.PlayerPort == null || current.WorldPort == null)
            {
                if (lastCity) { lastCity = false; Overlay.SetVisible(false); }
                return;
            }
            var scene = SceneManager.Instance;
            bool city = scene != null && scene.IsInHarborScene && scene.IsSceneEntered && !scene.IsInLoadingOrStarting && Player.PlayerPort.IsStayInPort;
            if (city != lastCity) { lastCity = city; Overlay.SetVisible(city); MarkHud(); }
            if (!city) return;
            if (!hudHooks) InstallHudHooks();
            if (!eventHud) hudDirty = true; // hooks unavailable: 0.5.4 behavior (check every frame)
            int portId = Player.PlayerPort.StayInPortId;
            if (portId != lastPort) { lastPort = portId; MarkHud(); }
            if (Journal.Revision != lastRevision) { lastRevision = Journal.Revision; statusDirty = true; Overlay.SetVisible(true); }
            if (!sizeHooked) { sizeHooked = true; Il2CppFairyGUI.GRoot.inst.onSizeChanged.Add((Il2CppFairyGUI.EventCallback0)(Action)(() => { statusDirty = true; })); }
            // 0.5.5: no copy/scan when nothing is pending (was a new array every frame in a city).
            if (Journal.Ports.Count > 0) { foreach (var pending in Journal.Ports.Values.ToArray()) Engine.Apply(Player, pending); statusDirty = true; }
            if (hudDirty)
            {
                // Evaluated on the frame after the event, once. If the harbor main UI is focused but its
                // show transition has not made every ancestor visible yet, re-check for at most 60 frames.
                bool main = HarborHud.IsMainScreen();
                Overlay.SetHudVisible(main);
                if (main || hudRetryFrames-- <= 0 || !HarborHud.MainFocused()) hudDirty = false;
                if (main) statusDirty = true;
            }
            if (!statusDirty || Now < nextHud) return;
            nextHud = Now + .1; statusDirty = false;
            var port = Player.WorldPort.GetPortData(portId);
            if (port != null) Overlay.Draw(Engine.Status(port));
        }
        catch (Exception ex) { Overlay.Clear(); MarkHud(); Error("City update", ex); }
    }
    // Postfix only: never skips, changes or delays the original call; sets two flags and returns.
    // The try/catch guarantees nothing can propagate back into the native caller.
    private static void HudEvent() { try { if (Enabled) MarkHud(); } catch { } }
    // Registered on the first in-game frame, never in OnInitializeMelon: resolving a UIManager
    // method runs its native .cctor, which creates FairyGUI.Stage too early (Item Rebuild 0.1.0
    // black screen, docs/mods/item-rebuild/0.1.1.md).
    private static bool hudHooks;
    private void InstallHudHooks()
    {
        hudHooks = true;
        try
        {
            Hook(typeof(UIManager), "SetFocusOnUIView", after: nameof(HudEvent));
            Hook(typeof(UIManager), "SetFocusOutUIView", after: nameof(HudEvent));
            Hook(typeof(Il2CppClient.UILogic.UIHarbor.UIHarborCtrl), "ShowMainUI", after: nameof(HudEvent));
            Hook(typeof(Il2CppClient.UILogic.UIHarbor.UIHarborCtrl), "HideMainUI", after: nameof(HudEvent));
            Hook(typeof(Il2CppClient.UILogic.UIHarbor.UIHarborCtrl), "ShowHook", after: nameof(HudEvent));
            Hook(typeof(Il2CppClient.UILogic.UIHarbor.UIHarborCtrl), "CloseHook", after: nameof(HudEvent));
            eventHud = true;
            Log.Msg("Harbor HUD event hooks registered (focus in/out, main UI show/hide, harbor show/close).");
        }
        catch (Exception ex) { eventHud = false; Error("Harbor HUD event hooks; falling back to the per-frame check", ex); }
    }
    private static bool eventHud;
    private static void MenuBegin(out bool __state) { __state = Enabled; if (__state) menuDepth++; }
    private static Exception? MenuEnd(Exception? __exception, bool __state) { if (__state) menuDepth--; return __exception; }
    private static void FilterMenuFunction(int __0, ref FunctionState? __result)
    { if (Enabled && menuDepth > 0 && __0 == (int)EFunctionOpen.GovHouseLicence) __result = null; }
    private static bool GuardLicence(UIGovHouseTab __0) => !(Enabled && __0 == UIGovHouseTab.Licence);
    private static bool CaptureInput() => !Enabled || !Overlay.BlocksInput;
    private static void EffectBefore(string __1,ref int __3) { if(Engine.GrantPort.HasValue && __1=="LicenceIncreaseCargoProduction")__3=Engine.GrantPort.Value; }
    private static void MarketGoods(UIMarketCtrl __instance) => LiveViews.MarketBuilt(__instance);
    public override void OnDeinitializeMelon() { Enabled = false; if (hooks != null) hooks.RemoveAll(); else HarmonyInstance.UnpatchSelf(); Reset(); }
}










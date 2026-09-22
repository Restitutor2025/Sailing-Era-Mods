using Il2CppClient.Manager;
using Il2CppClient.PlayerStore;
using Il2CppClient.UILogic.UIHarbor;
using Il2CppClient.UILogic.UIWharf;
using Il2CppClient.UILogic.UIMap;
using Il2CppClient.UILogic.UIMap.UIMapIcon;
using Il2CppClient.UILogic.UIMenu;
using Il2CppClient.UILogic.UISailReady;
using Il2CppClient.WorldLogic.Scenes;
using Il2CppCore.NewUISystem;
using Il2CppCore.InputSystem;
using Il2CppFairyGUI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Restitutor.Map;

// Candidate only. Static binding and scoped native reachability checks completed; runtime validation is pending.
internal static class SharedMap
{
    private static UIMapCtrl? map;
    private static UISailLineCtrl? planner;
    private static UISailReadyCtrl? ready;
    private static bool session, routeMode, available, closing, forwarding, fromHarbor;
    private static int lastFrame = -1, lastPort;
    private static long started;
    private static long generation;
    private static string closingAction = "";
    private static GObject? mapInfo;
    private static bool mapInfoVisible;
    private static bool verified;
    private static readonly Dictionary<IntPtr, (GObject flag, bool visible)> flags = new();

    internal static void Install()
    {
        // Native callers are replaced BEFORE they can look up the old singleton.
        H(typeof(HarbourScene), "UpdateHook", nameof(HarbourUpdate));
        H(typeof(UIHarborView), "_Auto_b__48_0", nameof(HarborEntry));
        H(typeof(UIWharfView), "_Auto_b__19_0", nameof(WharfRouteEntry));
        H(typeof(UIWharfCtrl), "_OpenReadySailing_b__9_0", nameof(WharfReadyEntry));
        H(typeof(UISailLineCtrl), "ShowHook", nameof(PlannerShow));
        H(typeof(UISailLineCtrl), "BuildAllLine", nameof(BuildAllLine));
        H(typeof(UISailLineCtrl), "GMSetAllLineOpen", nameof(OpenAllLines));
        H(typeof(UISailLineCtrl), "CloseHook", nameof(PlannerClose));
        H(typeof(UISailLineCtrl), "OnAction_A", nameof(SelectFocused), Type.EmptyTypes);
        H(typeof(UISailLineCtrl), "OnAction_A", nameof(SelectPort), new[] { typeof(int) });
        H(typeof(SailLineManager), "AddNodePort", nameof(AddPort));
        H(typeof(SailLineManager), "ReduceNodePort", nameof(RemovePort));
        H(typeof(UISailLineCtrl), "SetPortFocus", nameof(FocusPort));
        H(typeof(UISailLineCtrl), "OnAction_R3", nameof(FocusPlayer));
        H(typeof(UISailLineCtrl), "OnAction_UpAxis_And_DPadUp", nameof(ListFocus));
        H(typeof(UISailLineCtrl), "OnAction_DownAxis_And_DPadDown", nameof(ListFocus));
        H(typeof(UISailLineCtrl), "CheckNewLineAniComplete", nameof(CheckNewLine));
        H(typeof(UISailReadyCtrl), "ShowCheckLine", nameof(ShowCheckLine));
        H(typeof(UISailReadyCtrl), "CloseCheckLine", nameof(CloseCheckLine));
        H(typeof(UISailReadyCtrl), "_CloseCheckLine_b__26_0", nameof(DelayedClose));
        H(typeof(UISailReadyCtrl), "CloseHook", nameof(ReadyClose));
        H(typeof(UISailReadyView), "UpdateHook", nameof(ReadyUpdate));
        H(typeof(UIMapCtrl), "CloseHook", nameof(MapClose));
        H(typeof(UIMapCtrl), "DisposeHook", nameof(MapClose));
        H(typeof(UIBase), "get_IsInputActive", nameof(MapInput));
        H(typeof(UIMapCtrl), "OnAction_A", nameof(MapAction));
        H(typeof(UIMapCtrl), "OnAction_B", nameof(MapAction));
        H(typeof(UIMapCtrl), "OnAction_X", nameof(MapAction));
        H(typeof(UIMapCtrl), "ClickPortCheckLine", nameof(ClickPort));
        H(typeof(UIMapCtrl), "SetLane", nameof(SetLanes), Type.EmptyTypes);
        H(typeof(UIMapHarbourIcon), "_InitComponent_b__16_0", nameof(PortClick));
        EntryPoint.InputHandler(CaptureClose); // 0.2.4: shared Core input gate (was a prefix on InputSystemManager.OnEventCaptureInput)
        EntryPoint.Hook(typeof(UIMapView), "Refresh", typeof(SharedMap), after: nameof(MapRefresh));
        // 0.2.5: no hook on UIMapHarbourIcon.UpdateInfo (900-1,700 calls/s at sea for every map icon). Route-session
        // visuals are re-applied from the existing UIMapView.Refresh postfix, only for icons in view, only in a session.
        EntryPoint.Hook(typeof(UIMapHandler), "TouchPortOrAreaListUI", typeof(SharedMap), after: nameof(OverList));
    }
    private static void H(Type type, string method, string handler, Type[]? args = null) =>
        EntryPoint.Hook(type, method, typeof(SharedMap), before: handler, args: args);

    private static bool Owns(UIMapCtrl instance) => session && map?.Pointer == instance.Pointer;
    private static bool Focused()
    {
        if (!session || !available || closing) return false;
        var owner = routeMode ? planner?.View : ready?.View as UIBase;
        var focus = UIManager.Instance.CurrentFocusViewCtrl;
        return owner != null && owner.IsInputActive && focus != null &&
               focus.Pointer == (routeMode ? planner?.Pointer : ready?.Pointer);
    }
    private static bool Busy()
    {
        var t = TransitionManager.Instance;
        return t.IsInGatherState || t.IsEnterBlackScreen || t.IsInBlackScreen;
    }
    private static void Begin(bool harbor)
    {
        if (session) throw new InvalidOperationException("A sailing map session already exists.");
        var candidate = UIMapCtrl.Instance;
        if (!candidate.IsClose()) throw new InvalidOperationException("Tab map is owned by another screen.");
        var nextReady = UISailReadyCtrl.Instance;
        // A map can close before the original 500 ms ready-panel cleanup finishes.
        // Finish that panel before reusing its singleton for a fresh session.
        if (!nextReady.IsClose()) nextReady.Close();
        map = candidate; ready = nextReady; fromHarbor = harbor;
        generation++;
        session = true; available = routeMode = closing = false;
        started = System.Diagnostics.Stopwatch.GetTimestamp();
        if (!verified) { verified = true; RoutePortVisuals.Verify(); }
        MapDiag.Begin(harbor, nextReady.PlayTransition);
        map.Show();
        ready.Show();
    }
    private static bool HarbourUpdate()
    {
        if (GameManager.IsPlayerInGame) UIMapCtrl.Instance.UpdatePlayerInHarbour();
        return false;
    }
    private static bool HarborEntry(UIHarborView __instance)
    {
        UIHarborCtrl.Instance.Close();
        Begin(true);
        UISailLineCtrl.Instance.ShowCheckLane(true, __instance.model.AutoSupplyCost);
        __instance._isClick = false;
        return false;
    }
    private static bool WharfRouteEntry(UIWharfView __instance)
    {
        UIWharfCtrl.Instance.Close();
        Begin(false);
        UISailLineCtrl.Instance.ShowCheckLane(false, __instance.AutoSupplyCost);
        __instance._isClick = false;
        return false;
    }
    private static bool WharfReadyEntry(UIWharfCtrl __instance)
    { __instance.Close(); Begin(false); return false; }

    private static bool PlannerShow(UISailLineCtrl __instance)
    {
        if (!session) throw new InvalidOperationException("Route planner opened without its shared map session.");
        planner = __instance; routeMode = true; available = false;
        __instance.Model.SelectAreaIndex = -1;
        UIManager.Instance.ShowOperationTipsPanel("UI_Wharf", 4);
        ready!.ShowCheckLine();
        __instance.BuildData();
        map!.Model.MarkDirty();
        Il2Cpp.Messenger.Broadcast("OnTriggerHelp", (EPlayerHelp)0x147);
        __instance.Model.TargetDays = 0;
        var function = FunctionOpenManager.Instance._function.GetFunctionData(0x15);
        if (function != null && function.IsOpen)
            Il2Cpp.Messenger.Broadcast("OnTriggerHelp", (EPlayerHelp)0x155);
        return false;
    }
    private static bool BuildAllLine(UISailLineCtrl __instance)
    {
        var m = __instance.SailLineManager;
        m.ComplexLineDatas.Clear(); m.SinglePortLineDatas.Clear();
        m.InitSelectPortList(); m.InitAllLine();
        var target = map ?? UIMapCtrl.Instance;
        target.Model.ClearAllLaneIcon(); target.SetLane(m.GetAllSailLine());
        __instance.AddPortData();
        return false;
    }
    private static bool OpenAllLines(UISailLineCtrl __instance)
    {
        // Existing GM command only: do not invoke this from normal route entry.
        foreach (int lane in Il2CppGyyx.Template.TemplateManager.PrefabLaneKeys)
            __instance._playerData.PlayerLaneData.UnlockPrefabLane(lane);
        __instance.BuildData(); __instance.Model.MarkDirty();
        (map ?? UIMapCtrl.Instance).Model.MarkDirty();
        return false;
    }
    private static bool ShowCheckLine(UISailReadyCtrl __instance)
    {
        __instance.Model.ShowInterActive = 1;
        UIMenuHelper._menuUIShow = -1;
        return false;
    }
    private static bool ReadyUpdate(UISailReadyView __instance)
    {
        var ctrl = UISailReadyCtrl.Instance;
        if (!__instance.model.MapIsDown && session && map?.View != null &&
            map.View.IsOpen() && map.MapBgLoadDown())
        {
            __instance.model.MapIsDown = true;
            bool dissolve = ctrl.PlayTransition;
            if (dissolve) TransitionManager.Instance.LineDissolve(0.5f);
            MapDiag.MapDown(ctrl.PlayTransition, dissolve);
        }
        if (session) MapDiag.ReadyFrame();
        // This is exactly the native UpdateHook tail, also exposed as Updata.
        ctrl.Updata();
        return false;
    }
    internal static void Tick()
    {
        if (!session || closing || available) return;
        try
        {
            if (map?.View?.Content == null || !map.View.IsOpen() || !map.MapBgLoadDown() || Busy() ||
                ready?.View == null || !ready.View.IsOpen() || (routeMode && (planner?.View == null || !planner.View.IsOpen())))
            {
                if ((System.Diagnostics.Stopwatch.GetTimestamp() - started) / (double)System.Diagnostics.Stopwatch.Frequency > 15)
                {
                    MapDiag.Event($"timeout: mapOpen={map?.View?.IsOpen()} bgLoaded={map?.MapBgLoadDown()} busy={Busy()} readyOpen={ready?.View?.IsOpen()}");
                    throw new TimeoutException("Shared map did not finish loading.");
                }
                return;
            }
            var owner = routeMode ? planner!.Cast<IUIBaseCtrl>() : ready.Cast<IUIBaseCtrl>();
            if (map.View.IsOnFocused) map.TransferFocusTo(owner);
            mapInfo = map.View.Content.groupTip;
            mapInfoVisible = mapInfo.visible; mapInfo.visible = false;
            available = true;
            MapDiag.Event($"shared map available routeMode={routeMode}");
            if (routeMode) { UIManager.Instance.ShowOperationTipsPanel("UI_Wharf", 4); RefreshRoute(); }
            EntryPoint.Log.Msg($"[ROUTE] Shared Tab map ready; routeMode={routeMode}.");
        }
        catch (Exception ex)
        {
            EntryPoint.Log.Error("[ROUTE] Shared map failed: " + ex);
            if (routeMode && planner != null) planner.Close();
            else ready?.CloseCheckLine(fromHarbor);
        }
    }
    private static bool SelectFocused(UISailLineCtrl __instance)
    {
        if (!routeMode || !Focused()) return false;
        if (map!.Model.ViewMode == MapViewMode.World)
        {
            forwarding = true;
            try { map.OnAction_A(); } finally { forwarding = false; }
        }
        else
        {
            var port = map.GetCurrentSelectPort(true);
            if (port != null) __instance.OnAction_A(port.HarbourId);
        }
        return false;
    }
    private static bool SelectPort(UISailLineCtrl __instance, int __0)
    {
        if (!routeMode || !Focused() || planner?.Pointer != __instance.Pointer) return false;
        if (lastFrame == Time.frameCount && lastPort == __0) return false;
        lastFrame = Time.frameCount; lastPort = __0;
        NativeRouteData.Select(__instance, __0, true, RefreshRoute);
        return false;
    }
    private static bool AddPort(SailLineManager __instance, int __0)
    {
        if (routeMode && planner?.SailLineManager.Pointer == __instance.Pointer)
            NativeRouteData.Add(planner, __0, RefreshRoute);
        return false;
    }
    private static bool RemovePort(SailLineManager __instance, int __0)
    {
        if (routeMode && planner?.SailLineManager.Pointer == __instance.Pointer)
            NativeRouteData.Reduce(planner, __0, RefreshRoute);
        return false;
    }
    private static bool FocusPort(UISailLineCtrl __instance, EventContext __0)
    {
        var button = __0.sender?.TryCast<GButton>();
        if (button == null) return false;
        __instance.NeedClearPortList = false;
        map!.SetFocusPortIcon(button.data.Unbox<int>());
        var list = button.parent.Cast<GList>(); list.selectedIndex = list.GetChildIndex(button);
        return false;
    }
    private static bool FocusPlayer()
    { if (session) map!.SetMapFocusOnPlayer(); return false; }
    private static bool ListFocus(UISailLineCtrl __instance)
    {
        var view = __instance.View;
        if (view == null || !view.IsOpen() || !view.IsInputActive || view.UIContent.ctrlOpenStatus.selectedIndex == 0) return false;
        int area = view.ListArea.selectedIndex;
        if (area > -1)
            __instance.Model.SelectAreaIndex = view.ListArea.GetChildAt(area).Cast<GButton>().data.Unbox<int>();
        else
        {
            int index = view.ListPort.selectedIndex;
            if (index < 0) view.SetFocusPortList();
            else
            {
                __instance.NeedClearPortList = false;
                map!.SetFocusPortIcon(view.ListPort.GetChildAt(index).Cast<GButton>().data.Unbox<int>());
            }
        }
        return false;
    }
    private static bool CheckNewLine(UISailLineCtrl __instance, int __0)
    {
        if (__instance._playerData.PlayerPort.StayInPortId != __0 &&
            __instance._playerData.PlayerLaneData.CheckNewLane(__0, out var lines))
        { __instance.SailLineManager.StopLineNewAni(lines); map!.Model.MarkDirty(); }
        return false;
    }
    private static bool PlannerClose(UISailLineCtrl __instance)
    {
        UIManager.Instance.HideOperationTipsPanel();
        __instance.SailLineManager.SelectPortList.Clear();
        routeMode = false; available = false;
        UISailReadyCtrl.Instance.CloseCheckLine(__instance._isHarbor);
        return false;
    }
    private static bool CloseCheckLine(UISailReadyCtrl __instance, bool __0)
    {
        if (__0)
        {
            if (!__instance.Model.IsAutoSail) PortManager.Instance.CommonBackToPortFromFacility((Il2CppClient.Const.EPortFacilityType)0);
            long expectedGeneration = generation;
            __instance.AddTimer(new Il2CppCommon.Trigger.Trigger(500, 1, (Il2CppSystem.Action)(() =>
            {
                // A callback from a closed route must not close a newly opened route.
                if (generation != expectedGeneration) return;
                __instance.Model.ShowInterActive = 0;
                __instance.Close();
            })));
        }
        else
        {
            __instance.Model.ShowInterActive = 0;
            __instance.Close(); UIWharfCtrl.Instance.Show();
        }
        UIMenuHelper._menuUIShow = 0;
        return false;
    }
    private static bool DelayedClose(UISailReadyCtrl __instance)
    { __instance.Model.ShowInterActive = 0; __instance.Close(); return false; }
    private static bool ReadyClose(UISailReadyCtrl __instance)
    {
        closing = true;
        try { Restore(); if (session) map?.Close(); }
        finally { Clear(); closing = false; }
        __instance.PlayTransition = true;
        return false;
    }
    private static bool MapClose(UIMapCtrl __instance)
    {
        if (!Owns(__instance) || closing) return true;
        closing = true;
        try { Restore(); if (routeMode) planner?.Close(); else ready?.Close(); }
        finally { Clear(); closing = false; }
        return true;
    }
    private static bool MapInput(UIBase __instance, ref bool __result)
    {
        MapDiag.Input(session);
        if (!session || map?.View?.Pointer != __instance.Pointer) return true;
        __result = Focused(); return false;
    }
    private static bool MapAction(UIMapCtrl __instance) => forwarding || !Owns(__instance);
    private static bool ClickPort(UIMapCtrl __instance, int __0)
    {
        if (!Owns(__instance) || !routeMode) return true;
        if (Focused()) planner!.OnAction_A(__0);
        return false;
    }
    private static bool PortClick(UIMapHarbourIcon __instance)
    {
        if (!routeMode || !session || map == null || __instance.Model?.Pointer != map.Model.Pointer || map.Model.ViewMode == MapViewMode.World) return true;
        if (Focused() && __instance.IsInit && __instance.Component != null && !__instance.Component.isDisposed)
        { map.SetFocusPortIcon(__instance.HarbourId); planner!.OnAction_A(__instance.HarbourId); }
        return false;
    }
    private static bool CaptureClose(InputAction.CallbackContext __0)
    {
        string name = __0.action?.name ?? "";
        if (closingAction.Length > 0 && closingAction == name)
        {
            if (__0.canceled || (__0.performed && !__0.ReadValueAsButton())) closingAction = "";
            return false;
        }
        if (!routeMode || !Focused() || (name != "Action_Start" && name != "MenuMap")) return true;
        if (__0.performed && __0.ReadValueAsButton()) { closingAction = name; planner!.Close(); }
        return false;
    }
    private static bool SetLanes(UIMapCtrl __instance)
    { if (!Owns(__instance) || !routeMode) return true; RefreshRoute(); return false; }
    private static void RefreshRoute()
    {
        if (!session || !routeMode || planner == null || map == null) return;
        MapDiag.Route();
        RoutePortVisuals.Invalidate();
        var manager = planner.SailLineManager;
        var lines = manager.GetAllSailLine(); var selected = new RouteHighlights();
        foreach (var path in manager.BestLineList)
            for (int i = 1; i < path.Count; i++) selected.AddEdge(path[i - 1], path[i]);
        foreach (var line in lines)
        {
            line.isSelect = selected.TryDirection(line.StartNodeId, line.EndNodeId, out bool forward);
            if (line.isSelect) line.Order = forward;
        }
        map.SetLane(lines); map.Model.MarkDirty(); planner.Model.MarkDirty();
        foreach (var icon in map.Model.HarbourIcons.Values) RefreshPort(icon, false);
    }
    // Same reachability rule as 0.2.1; one dictionary lookup instead of three.
    private static bool Reachable(int harbourId)
    {
        if (harbourId == planner!._playerData.PlayerPort.StayInPortId) return true;
        return planner.Model.TotalPortDic.TryGetValue(harbourId, out var data) && data != null && data.MeasureCanReach && data.NoLineReach;
    }
    private static bool RouteIcon(UIMapHarbourIcon icon)
        => routeMode && session && map != null && icon.IsInit && icon.Model?.Pointer == map.Model.Pointer;
    // Runs inside UIMapView.Refresh after the native UpdateInViewIcons has redrawn the visible icons
    // (UpdateInfo rewrites url and ctrlSelfPort every call, so the red state is re-applied here).
    private static void RefreshVisible()
    {
        var model = map!.Model; var icons = model.HarbourIcons; var inView = model.InViewIcons;
        if (icons == null || inView == null) return;
        foreach (int id in inView)
            if (icons.TryGetValue(id, out var icon) && icon != null) { MapDiag.PortInfo(); RefreshPort(icon, true); }
    }
    private static void RefreshPort(UIMapHarbourIcon __instance, bool nativeRefresh)
    {
        if (!RouteIcon(__instance)) return;
        RoutePortVisuals.Apply(__instance, Reachable(__instance.HarbourId), nativeRefresh);
        var flag = __instance.Component?.TryCast<Il2CppMap.UIHarbourIcon>()?.imgRedFlag;
        if (flag == null || flag.isDisposed) return;
        if (!flags.ContainsKey(flag.Pointer)) flags.Add(flag.Pointer, (flag, flag.visible));
        flag.visible = planner!.SailLineManager.SelectPortList.Contains(__instance.HarbourId);
    }
    private static void MapRefresh(UIMapView __instance)
    {
        if (session && map?.View?.Pointer == __instance.Pointer) MapDiag.View();
        if (session && routeMode && map?.View?.Pointer == __instance.Pointer) RefreshVisible();
        if (session && map?.View?.Pointer == __instance.Pointer && mapInfo != null && !mapInfo.isDisposed) mapInfo.visible = false;
    }
    private static void OverList(UIMapHandler __instance, ref bool __result)
    { if (routeMode && session && map?.UIMapHandler?.Pointer == __instance.Pointer) __result |= planner!.TouchPortOrAreaList(); }
    private static void Restore()
    {
        RoutePortVisuals.Restore();
        foreach (var saved in flags.Values) if (!saved.flag.isDisposed) saved.flag.visible = saved.visible;
        flags.Clear();
        if (mapInfo != null && !mapInfo.isDisposed) mapInfo.visible = mapInfoVisible;
    }
    private static void Clear()
    {
        session = routeMode = available = false;
        map = null; planner = null; ready = null; mapInfo = null;
        flags.Clear(); lastFrame = -1; lastPort = 0; forwarding = false;
        RoutePortVisuals.Restore();
    }
}




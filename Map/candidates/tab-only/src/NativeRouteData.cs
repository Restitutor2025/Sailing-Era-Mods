using Il2CppClient.UILogic.UISailReady;
using Il2CppClient.UILogic.UITips;
using Il2CppClient.Utils;
using UnityEngine;
using IntList = Il2CppSystem.Collections.Generic.List<int>;
using VectorList = Il2CppSystem.Collections.Generic.List<UnityEngine.Vector2>;

namespace Restitutor.Map;

// Native calculation is retained. Only the caller's old-map presentation dependencies
// are removed here. Registered through the shared-map lifecycle adapter.
internal static class NativeRouteData
{
    // UISailLineCtrl.OnAction_A(int), RVA 0xD31EE0.
    internal static void Select(UISailLineCtrl planner, int port, bool acceptInput, Action refresh)
    {
        if (!acceptInput || port <= 0) return;
        var model = planner.Model;
        if (model.Measurer == null) { Tip("Wharf_Preparing_Cabin_Need_Hero"); return; }
        var manager = planner.SailLineManager;
        if (manager.SelectPortIndex == 0 && port == planner._playerData.PlayerPort.StayInPortId) return;
        if (!model.TotalPortDic.ContainsKey(port)) return;
        ESelectPortStatus status = default;
        int requiredLevel = 0;
        planner.CheckPortSelectStatus(port, out status, out requiredLevel);
        if ((int)status == 5) return;
        if ((int)status == 3) { Tip("Tip_MeasureLvNoReached"); return; }
        // Despite its name, the native branch requires this field to be TRUE.
        if (!model.TotalPortDic[port].NoLineReach)
        { Tip("UIStatic_SailLine_NoOpenSailLineOrContainUnexploredPorts"); return; }
        if (manager.SelectPortList.Contains(port)) Reduce(planner, port, refresh);
        else Add(planner, port, refresh);
        model.MarkDirty();
    }

    // SailLineManager.AddNodePort, RVA 0xC65550. FindPath remains native.
    internal static void Add(UISailLineCtrl planner, int port, Action refresh)
    {
        var manager = planner.SailLineManager;
        int index = manager.SelectPortIndex;
        if (index >= 3) return;
        if (index == 2 && manager.SelectPortList[2] > 0)
        {
            Reduce(planner, manager.SelectPortList[2], refresh);
            index = manager.SelectPortIndex;
        }
        int previousPort = 0;
        int startPort = planner.Model.HarbourID;
        Vector2 previousEnd = Vector2.zero;
        if (index > 0)
        {
            previousPort = startPort = manager.SelectPortList[index - 1];
            var previousPath = manager.BestLineVector2List[index - 1];
            // Native Enumerable.Last also fails on an empty preceding path.
            previousEnd = previousPath[previousPath.Count - 1];
        }
        IntList nodes = null!;
        VectorList points = null!;
        if (!manager.FindPath(planner.GetPortNode(startPort), planner.GetPortNode(port),
                              out nodes, out points, previousPort, previousEnd))
        { Tip("UIStatic_SailLine_NoOpenSailLineOrContainUnexploredPorts"); return; }
        manager.SelectPortList[index] = port;
        manager.BestLineList[index].AddRange(nodes.Cast<Il2CppSystem.Collections.Generic.IEnumerable<int>>());
        manager.BestLineVector2List[index].AddRange(points.Cast<Il2CppSystem.Collections.Generic.IEnumerable<Vector2>>());
        refresh();
        planner.SetTargetDays2();
    }

    // SailLineManager.ReduceNodePort, RVA 0xC65B80.
    // The original clears from the final slot backwards through the requested port;
    // it writes zero (0xD7330), it does not remove list elements or reconnect paths.
    internal static void Reduce(UISailLineCtrl planner, int port, Action refresh)
    {
        var manager = planner.SailLineManager;
        for (int i = manager.SelectPortList.Count - 1; i >= 0; --i)
        {
            manager.BestLineList[i].Clear();
            manager.BestLineVector2List[i].Clear();
            int removed = manager.SelectPortList[i];
            manager.SelectPortList[i] = 0;
            if (removed == port) break;
        }
        // The display adapter rebuilds selected edges from the surviving paths,
        // including edges shared by removed and surviving segments.
        refresh();
        planner.SetTargetDays2();
    }

    private static void Tip(string key) => UITipsCtrl.Instance.ShowBaseTips(TextLibUtils.Text(key, ""), null);
}



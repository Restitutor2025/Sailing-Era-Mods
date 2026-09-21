namespace UnityEngine
{
    public record struct Vector2(float x, float y) { public static Vector2 zero => default; }
}
namespace Il2CppSystem.Collections.Generic
{
    public interface IEnumerable<T> : System.Collections.Generic.IEnumerable<T> { }
    public class List<T> : System.Collections.Generic.List<T>, IEnumerable<T>
    { public TCast Cast<TCast>() => (TCast)(object)this; }
}
namespace Il2CppClient.Utils
{ public static class TextLibUtils { public static string Text(string key, string _) => key; } }
namespace Il2CppClient.UILogic.UITips
{
    public class UITipsCtrl
    {
        public static UITipsCtrl Instance = new();
        public string Last = "";
        public void ShowBaseTips(string message, string? _) => Last = message;
    }
}
namespace Il2CppClient.UILogic.UISailReady
{
    using Il2CppSystem.Collections.Generic;
    using UnityEngine;
    public enum ESelectPortStatus { None=0, NeedMeasurer=3, Unreached=5 }
    public class PortDB { public int StayInPortId = 10; }
    public class PlayerData { public PortDB PlayerPort = new(); }
    public class Port { public bool NoLineReach = true; }
    public class Model
    {
        public object? Measurer = new();
        public int HarbourID = 10, Dirty;
        public Dictionary<int, Port> TotalPortDic = new();
        public void MarkDirty() => Dirty++;
    }
    public class UISailLineCtrl
    {
        public Model Model = new();
        public SailLineManager SailLineManager = new();
        public PlayerData _playerData = new();
        public ESelectPortStatus Status;
        public int DaysUpdates;
        public void CheckPortSelectStatus(int p, out ESelectPortStatus status, out int level) { status=Status;level=0; }
        public int GetPortNode(int port) => port * 10;
        public void SetTargetDays2() => DaysUpdates++;
    }
    public class SailLineManager
    {
        public List<int> SelectPortList = new() {0,0,0};
        public List<List<int>> BestLineList = new() {new(),new(),new()};
        public List<List<Vector2>> BestLineVector2List = new() {new(),new(),new()};
        public int SelectPortIndex => SelectPortList[0] == 0 ? 0 : SelectPortList[1] == 0 ? 1 : 2;
        public bool PathSucceeds = true;
        public int PathCalls;
        public (int start,int end,int previous,Vector2 position) LastPath;
        public bool FindPath(int start,int end,out List<int> nodes,out List<Vector2> points,int previous,Vector2 position)
        {
            PathCalls++; LastPath=(start,end,previous,position);
            nodes=new() {start,end};points=new() {new(start,0),new(end,0)};
            return PathSucceeds;
        }
    }
}

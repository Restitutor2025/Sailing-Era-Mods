using System.Reflection;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using Il2CppClient.WorldLogic.Scenes;
using SceneManager = Il2CppCore.SceneSystem.SceneManager;

[assembly: MelonInfo(typeof(Restitutor.SailTrace.EntryPoint), "Restitutor Analytics SailTrace", "0.1.2", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]

namespace Restitutor.SailTrace;

// Diagnostic-only recorder for sailing stutter / black terrain+sea.
// Contract: never skips originals, never changes arguments/results/state, never calls GC, never forces loads.
// Records only while SceneManager reports the ocean scene.
// 0.1.1: hooks installed on the first frame after a scene is entered (not at load); ship-entity creation
// timing (BoatEntityCreateManager.CreateShipEntity = 1 flagship per frame, OceanScene.AddNpcBoat);
// 0.1.2: calendar timing (CalendarMgr.AddGameInsideMonth, PortScheduleManager.OnTheMonthRefresh/OnTheDayRefresh)
// recorded in every scene; gap rows carry month/day work done inside the long frame.
// gap rows carry per-frame ship creations, instantiate requests by category, IL2CPP GC count and used-heap delta.
public sealed class EntryPoint : MelonMod
{
    private const string Version = "0.1.2";
    private const double GapThresholdMs = 40;     // 0.1.1: same as HookCensus [HITCH] (above one missed vsync frame)
    private const double SlowFrameMs = 25;
    private const double ChurnWindowMs = 5000;
    private const int MaxTileEventsPerSecond = 400;

    private static TraceWriter? writer;
    private static bool enabled, warned;
    private static int mainThread;
    private static readonly List<string> hooked = new();
    private static readonly List<string> failedHooks = new();

    // --- split-tile tracking (key = IL2CPP object pointer; Boehm GC is non-moving) ---
    private sealed class TileState
    {
        public string Key = "";
        public UnityEngine.Vector3 Pos;
        public double Requested;
        public double Arrived = -1;
    }
    private static readonly Dictionary<IntPtr, TileState> tiles = new();
    private static readonly Dictionary<string, double> lastUnloadByKey = new();
    [ThreadStatic] private static IntPtr loadingSplit;
    [ThreadStatic] private static UnityEngine.Vector3 loadingPos;

    // --- per-frame / per-second counters ---
    private sealed class Counters
    {
        public int Frames, Slow, Gaps;
        public double MaxGap;
        public int TileReq, TileArrive, TileUnloadLoaded, TileUnloadPending, TileChurn, TileDisposeLoaded;
        public int OtherReq;
        public int NpcBoats, AreaEvents, ShipCreate;
        public double ShipCreateMs, ShipCreateMaxMs, NpcBoatMs;
        public int Month, Day; public double MonthMs, DayMs;
        public double MaxArrivalLatency, SumArrivalLatency, MaxCompleteMs;
        public readonly Dictionary<string, int> ReqByCategory = new();
        public void Clear()
        {
            Frames = Slow = Gaps = 0; MaxGap = 0;
            TileReq = TileArrive = TileUnloadLoaded = TileUnloadPending = TileChurn = TileDisposeLoaded = 0;
            OtherReq = NpcBoats = AreaEvents = ShipCreate = 0;
            ShipCreateMs = ShipCreateMaxMs = NpcBoatMs = 0;
            Month = Day = 0; MonthMs = DayMs = 0;
            MaxArrivalLatency = SumArrivalLatency = MaxCompleteMs = 0;
            ReqByCategory.Clear();
        }
    }
    private static readonly Counters second = new();
    private static readonly Counters frame = new();
    private static readonly Queue<(double t, int req, int arrive, int unload)> recentFrames = new();
    private static int tileEventsThisSecond, suppressedTileEvents;

    private static double lastFrame, secondStart;
    private static long frameIndex;
    private static int lastGcCount = -1;
    private static long lastUsed;
    private static bool inOcean;
    private static UnityEngine.Vector3 lastCamPos;
    private static bool haveCam;
    private static int lastQuality = -1;

    public override void OnInitializeMelon()
    {
        try
        {
            mainThread = Environment.CurrentManagedThreadId;
            writer = new TraceWriter(Path.Combine(MelonEnvironment.UserDataDirectory, "Restitutor", "SailTrace"));
            enabled = true; // hooks are installed later in Install(); OnUpdate needs enabled to reach it
            LoggerInstance.Msg($"SailTrace {Version}: writer ready ({writer.PathName}); hooks install on the first entered scene.");
        }
        catch (Exception ex)
        {
            enabled = false;
            writer?.Dispose();
            LoggerInstance.Error("SailTrace initialization failed: " + ex);
        }
    }

    private bool installed;
    private void Install()
    {
        installed = true;
        try
        {
            var asm = typeof(OceanScene).Assembly;

            // Terrain split tiles (static basis: MapTile.Update -> NeedLoad ? MapSplitTile.Load : MapSplitTile.UnLoad)
            var split = asm.GetType("Il2CppClient.WorldLogic.Map.MapSplitTile", true)!;
            TryPatch(split, "Load", 2, nameof(SplitLoadPrefix), nameof(SplitLoadPostfix));
            TryPatch(split, "OnLoadComplete", 1, nameof(SplitCompletePrefix), nameof(SplitCompletePostfix));
            TryPatch(split, "UnLoad", 0, nameof(SplitUnloadPrefix), null);
            TryPatch(split, "Dispose", 0, nameof(SplitDisposePrefix), null);

            // All instantiate requests (tiles + map objects + other); both overloads.
            var res = asm.GetType("Il2CppCore.ResourcesSystem.ResourcesManager", true)!;
            TryPatch(res, "InstantiateAsync", 2, nameof(InstantiatePrefix), null);
            TryPatch(res, "InstantiateAsync", 4, nameof(InstantiatePrefix), null);

            // Area / NPC / quality events.
            var ocean = typeof(OceanScene);
            TryPatch(ocean, "OnEnterSeaArea", 1, nameof(AreaIntPrefix), null);
            TryPatch(ocean, "OnEnterSeaSection", 1, nameof(AreaLongPrefix), null);
            TryPatch(ocean, "OnEnterArea", 1, nameof(AreaLongPrefix), null);
            TryPatch(ocean, "OnExitArea", 1, nameof(AreaLongPrefix), null);
            TryPatch(ocean, "OnAfterEnterSpawnNpcArea", 1, nameof(AreaLongPrefix), null);
            TryPatch(ocean, "AddNpcBoat", 5, nameof(NpcBoatPrefix), nameof(NpcBoatPostfix));
            var creator = asm.GetType("Il2CppClient.WorldLogic.Scenes.Ocean.BoatCreating.BoatEntityCreateManager", true)!;
            TryPatch(creator, "CreateShipEntity", 2, nameof(ShipCreatePrefix), nameof(ShipCreatePostfix));
            var calendar = FindType(asm, "CalendarMgr"); var schedule = FindType(asm, "PortScheduleManager");
            if (calendar != null) TryPatch(calendar, "AddGameInsideMonth", 1, nameof(CalPrefix), nameof(MonthAddPostfix)); else failedHooks.Add("CalendarMgr: type not found");
            if (schedule != null) { TryPatch(schedule, "OnTheMonthRefresh", 0, nameof(CalPrefix), nameof(MonthRefreshPostfix)); TryPatch(schedule, "OnTheDayRefresh", 0, nameof(CalPrefix), nameof(DayRefreshPostfix)); }
            else failedHooks.Add("PortScheduleManager: type not found");
            var spawner = asm.GetType("Il2CppClient.WorldLogic.Map.MapSpawner", true)!;
            TryPatch(spawner, "OnTerrainQualityChange", 0, nameof(QualityChangePrefix), null);

            enabled = true;
            writer!.Add(new
            {
                k = "session", version = Version, schema = 1, utc = DateTime.UtcNow,
                interopMvid = asm.ManifestModule.ModuleVersionId.ToString(),
                quality = SafeQuality(), qualityName = SafeQualityName(),
                vSync = UnityEngine.QualitySettings.vSyncCount, targetFps = UnityEngine.Application.targetFrameRate,
                screen = UnityEngine.Screen.width + "x" + UnityEngine.Screen.height,
                gapThresholdMs = GapThresholdMs, hooks = hooked.ToArray(), failedHooks = failedHooks.ToArray(),
                note = "Diagnostic only. Tile key = instance pointer, name = request key. gap = wall time between OnUpdate calls (CPU+GPU wait+loading), not GPU time."
            });
            LoggerInstance.Msg($"SailTrace {Version} recording: {writer!.PathName} (hooks {hooked.Count}, failed {failedHooks.Count})");
            foreach (var f in failedHooks) LoggerInstance.Warning("SailTrace hook unavailable: " + f);
        }
        catch (Exception ex)
        {
            enabled = false;
            try { HarmonyInstance.UnpatchSelf(); } catch { }
            writer?.Dispose();
            LoggerInstance.Error("SailTrace hook install failed; hooks removed: " + ex);
        }
    }

    private void TryPatch(Type type, string name, int paramCount, string? prefix, string? postfix)
    {
        string label = $"{type.Name}.{name}/{paramCount}";
        try
        {
            var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == name && m.GetParameters().Length == paramCount && !m.IsGenericMethodDefinition).ToArray();
            if (candidates.Length != 1) throw new MissingMethodException(type.FullName, $"{name} ({candidates.Length} candidates with {paramCount} params)");
            HarmonyInstance.Patch(candidates[0],
                prefix == null ? null : H(prefix),
                postfix == null ? null : H(postfix),
                finalizer: H(nameof(PassThroughFinalizer)));
            hooked.Add(label);
        }
        catch (Exception ex) { failedHooks.Add(label + ": " + ex.GetType().Name + " " + ex.Message); }
    }
    private static HarmonyMethod H(string name) => new(typeof(EntryPoint).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!);

    private static bool Active => enabled && inOcean && Environment.CurrentManagedThreadId == mainThread;
    private static double Round(double v) => Math.Round(v, 2);

    // ---------------- hooks ----------------
    private static Exception? PassThroughFinalizer(Exception? __exception)
    {
        loadingSplit = IntPtr.Zero;
        return __exception; // never suppress or replace game exceptions
    }

    private static void SplitLoadPrefix(Il2CppSystem.Object __instance, UnityEngine.Vector3 __1)
    {
        try
        {
            if (!Active) return;
            loadingSplit = __instance.Pointer;
            loadingPos = __1;
        }
        catch (Exception ex) { DiagnosticError("SplitLoadPrefix", ex); }
    }
    private static void SplitLoadPostfix() { loadingSplit = IntPtr.Zero; }

    private static void InstantiatePrefix(string __0)
    {
        try
        {
            if (!Active) return;
            string key = __0 ?? "";
            string cat = Category(key);
            second.ReqByCategory[cat] = second.ReqByCategory.TryGetValue(cat, out var n) ? n + 1 : 1;
            frame.ReqByCategory[cat] = frame.ReqByCategory.TryGetValue(cat, out var fn) ? fn + 1 : 1;
            var ptr = loadingSplit;
            if (ptr == IntPtr.Zero) { second.OtherReq++; frame.OtherReq++; return; }
            double now = TraceWriter.Ms;
            tiles[ptr] = new TileState { Key = key, Pos = loadingPos, Requested = now };
            second.TileReq++; frame.TileReq++;
            double? reloadAfter = null;
            if (lastUnloadByKey.TryGetValue(key, out var unloadedAt))
            {
                if (now - unloadedAt <= ChurnWindowMs) { second.TileChurn++; reloadAfter = Round(now - unloadedAt); }
                lastUnloadByKey.Remove(key);
            }
            TileEvent(new { k = "tile_req", ms = Round(now), f = frameIndex, key = ShortKey(key), cat, pos = P(loadingPos), vp = Viewport(loadingPos), reloadAfterMs = reloadAfter, inFlight = InFlight() });
        }
        catch (Exception ex) { DiagnosticError("InstantiatePrefix", ex); }
    }

    private static void SplitCompletePrefix(Il2CppSystem.Object __instance, out double __state)
    {
        __state = TraceWriter.Now;
    }
    private static void SplitCompletePostfix(Il2CppSystem.Object __instance, UnityEngine.GameObject __0, double __state)
    {
        try
        {
            if (!Active) return;
            double now = TraceWriter.Ms, completeMs = TraceWriter.Now - __state;
            if (!tiles.TryGetValue(__instance.Pointer, out var t))
            {
                TileEvent(new { k = "tile_arrive_untracked", ms = Round(now), f = frameIndex, name = SafeName(__0), completeMs = Round(completeMs) });
                return;
            }
            t.Arrived = now;
            double latency = now - t.Requested;
            second.TileArrive++; frame.TileArrive++;
            second.SumArrivalLatency += latency;
            second.MaxArrivalLatency = Math.Max(second.MaxArrivalLatency, latency);
            second.MaxCompleteMs = Math.Max(second.MaxCompleteMs, completeMs);
            TileEvent(new { k = "tile_arrive", ms = Round(now), f = frameIndex, key = ShortKey(t.Key), latencyMs = Round(latency), completeMs = Round(completeMs), vp = Viewport(t.Pos), inFlight = InFlight() });
        }
        catch (Exception ex) { DiagnosticError("SplitCompletePostfix", ex); }
    }

    private static void SplitUnloadPrefix(Il2CppSystem.Object __instance) => EndTile(__instance, "tile_unload");
    private static void SplitDisposePrefix(Il2CppSystem.Object __instance) => EndTile(__instance, "tile_dispose");
    private static void EndTile(Il2CppSystem.Object instance, string kind)
    {
        try
        {
            if (!enabled || Environment.CurrentManagedThreadId != mainThread) return;
            // UnLoad is called every frame for tiles outside view; only tracked (requested) tiles produce events.
            if (!tiles.Remove(instance.Pointer, out var t)) return;
            if (!inOcean) return;
            double now = TraceWriter.Ms;
            bool loaded = t.Arrived >= 0;
            if (kind == "tile_unload") { if (loaded) { second.TileUnloadLoaded++; frame.TileUnloadLoaded++; } else second.TileUnloadPending++; }
            else if (loaded) second.TileDisposeLoaded++;
            lastUnloadByKey[t.Key] = now;
            TileEvent(new { k = kind, ms = Round(now), f = frameIndex, key = ShortKey(t.Key), loaded, aliveMs = Round(now - (loaded ? t.Arrived : t.Requested)), vp = Viewport(t.Pos), inFlight = InFlight() });
        }
        catch (Exception ex) { DiagnosticError("EndTile", ex); }
    }

    private static void AreaIntPrefix(int __0, MethodBase __originalMethod) => AreaEvent(__originalMethod.Name, __0);
    private static void AreaLongPrefix(long __0, MethodBase __originalMethod) => AreaEvent(__originalMethod.Name, __0);
    private static void AreaEvent(string name, long id)
    {
        try
        {
            if (!Active) return;
            second.AreaEvents++;
            writer?.Add(new { k = "area", ms = Round(TraceWriter.Ms), f = frameIndex, name, id });
        }
        catch (Exception ex) { DiagnosticError("AreaEvent", ex); }
    }
    private static void NpcBoatPrefix(out double __state) { __state = TraceWriter.Now; }
    private static void NpcBoatPostfix(double __state)
    {
        try
        {
            if (!Active) return;
            double ms = TraceWriter.Now - __state;
            second.NpcBoats++; frame.NpcBoats++; second.NpcBoatMs += ms; frame.NpcBoatMs += ms;
            writer?.Add(new { k = "npc_boat", ms = Round(TraceWriter.Ms), f = frameIndex, costMs = Round(ms) });
        }
        catch (Exception ex) { DiagnosticError("NpcBoatPostfix", ex); }
    }
    private static Type? FindType(Assembly asm, string name)
    {
        Type[] all; try { all = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { all = e.Types.Where(t => t != null).Select(t => t!).ToArray(); }
        var m = all.Where(t => t.Name == name).ToArray();
        return m.Length == 1 ? m[0] : null;
    }
    private static void CalPrefix(out double __state) { __state = TraceWriter.Now; }
    private static void MonthAddPostfix(double __state) => Calendar("month_add", __state);
    private static void MonthRefreshPostfix(double __state) => Calendar("month_refresh", __state);
    private static void DayRefreshPostfix(double __state) => Calendar("day_refresh", __state);
    private static void Calendar(string kind, double start)
    {
        try
        {
            if (!enabled || Environment.CurrentManagedThreadId != mainThread) return;
            double ms = TraceWriter.Now - start;
            if (kind == "day_refresh") { frame.Day++; frame.DayMs += ms; } else if (kind == "month_add") { frame.Month++; frame.MonthMs += ms; }
            string scene = "?"; try { var sm = SceneManager.Instance; scene = sm == null ? "null" : sm.IsInOceanScene ? "ocean" : sm.IsInHarborScene ? "harbor" : "other"; } catch { }
            writer?.Add(new { k = kind, ms = Round(TraceWriter.Ms), f = frameIndex, costMs = Round(ms), scene });
        }
        catch (Exception ex) { DiagnosticError("Calendar", ex); }
    }
    private static void ShipCreatePrefix(out double __state) { __state = TraceWriter.Now; }
    private static void ShipCreatePostfix(double __state)
    {
        try
        {
            if (!Active) return;
            double ms = TraceWriter.Now - __state;
            second.ShipCreate++; frame.ShipCreate++;
            second.ShipCreateMs += ms; frame.ShipCreateMs += ms;
            second.ShipCreateMaxMs = Math.Max(second.ShipCreateMaxMs, ms);
            writer?.Add(new { k = "ship_create", ms = Round(TraceWriter.Ms), f = frameIndex, costMs = Round(ms) });
        }
        catch (Exception ex) { DiagnosticError("ShipCreatePostfix", ex); }
    }
    private static void QualityChangePrefix()
    {
        try
        {
            if (!enabled) return;
            writer?.Add(new { k = "terrain_quality_change", ms = Round(TraceWriter.Ms), f = frameIndex, quality = SafeQuality(), qualityName = SafeQualityName() });
        }
        catch (Exception ex) { DiagnosticError("QualityChangePrefix", ex); }
    }

    // ---------------- frame loop ----------------
    public override void OnUpdate()
    {
        if (!enabled) { WarnOnce(); return; }
        if (!installed)
        {
            try { var sm = SceneManager.Instance; if (sm == null || !sm.IsSceneEntered) return; } catch { return; }
            Install();
            if (!enabled) return;
        }
        try
        {
            if (writer?.Failure != null) { enabled = false; WarnOnce(); return; }
            double now = TraceWriter.Ms;
            frameIndex++;
            bool nowOcean = IsOcean();
            if (nowOcean != inOcean)
            {
                if (inOcean) FlushSecond(now);
                inOcean = nowOcean;
                writer?.Add(new { k = nowOcean ? "ocean_enter" : "ocean_exit", ms = Round(now), f = frameIndex, trackedTiles = tiles.Count });
                if (!nowOcean) { tiles.Clear(); lastUnloadByKey.Clear(); recentFrames.Clear(); }
                lastFrame = now; secondStart = now; haveCam = false; lastGcCount = -1; lastUsed = 0;
                second.Clear(); frame.Clear();
                return;
            }
            if (!inOcean) { lastFrame = now; frame.Clear(); return; }

            double gap = now - lastFrame;
            lastFrame = now;
            int gc = SafeGc();
            long used = SafeUsed();
            long usedDelta = lastUsed > 0 && used > 0 ? used - lastUsed : 0;
            if (used > 0) lastUsed = used;
            int gcDelta = lastGcCount >= 0 && gc >= 0 ? gc - lastGcCount : 0;
            if (gc >= 0) lastGcCount = gc;

            second.Frames++;
            if (gap >= SlowFrameMs) second.Slow++;
            second.MaxGap = Math.Max(second.MaxGap, gap);

            // 1-second sliding window of per-frame tile activity (frame counters describe the frame that just ended).
            recentFrames.Enqueue((now, frame.TileReq, frame.TileArrive, frame.TileUnloadLoaded));
            while (recentFrames.Count > 0 && now - recentFrames.Peek().t > 1000) recentFrames.Dequeue();

            if (gap >= GapThresholdMs)
            {
                second.Gaps++;
                int req1 = 0, arr1 = 0, unl1 = 0;
                foreach (var r in recentFrames) { req1 += r.req; arr1 += r.arrive; unl1 += r.unload; }
                writer?.Add(new
                {
                    k = "gap", ms = Round(now), f = frameIndex, gapMs = Round(gap),
                    // activity recorded between previous OnUpdate and this one (i.e. inside the long frame)
                    inFrame = new { req = frame.TileReq, arrive = frame.TileArrive, unload = frame.TileUnloadLoaded, otherReq = frame.OtherReq,
                        month = frame.Month, monthMs = Round(frame.MonthMs), day = frame.Day, dayMs = Round(frame.DayMs),
                        shipCreate = frame.ShipCreate, shipCreateMs = Round(frame.ShipCreateMs), npcBoat = frame.NpcBoats, npcBoatMs = Round(frame.NpcBoatMs),
                        reqByCat = frame.ReqByCategory.Count > 0 ? new Dictionary<string, int>(frame.ReqByCategory) : null },
                    il2cppUsedDeltaMB = Round(usedDelta / 1048576.0),
                    last1s = new { req = req1, arrive = arr1, unload = unl1 },
                    inFlight = InFlight(), trackedTiles = tiles.Count, gcDelta, heapMB = SafeHeapMB(),
                    battle = SafeBattle(), focused = UnityEngine.Application.isFocused
                });
            }
            frame.Clear();

            if (now - secondStart >= 1000) FlushSecond(now);
        }
        catch (Exception ex) { DiagnosticError("OnUpdate", ex); }
    }

    private static void FlushSecond(double now)
    {
        double span = now - secondStart;
        if (span <= 0 || second.Frames == 0) { secondStart = now; second.Clear(); tileEventsThisSecond = 0; return; }
        object? cam = null;
        double camSpeed = -1;
        try
        {
            var c = UnityEngine.Camera.main;
            if (c != null)
            {
                var p = c.transform.position;
                if (haveCam) camSpeed = Round(UnityEngine.Vector3.Distance(new UnityEngine.Vector3(p.x, 0, p.z), new UnityEngine.Vector3(lastCamPos.x, 0, lastCamPos.z)) / (span / 1000.0));
                lastCamPos = p; haveCam = true;
                cam = new { x = Round(p.x), y = Round(p.y), z = Round(p.z), fov = Round(c.fieldOfView), far = Round(c.farClipPlane) };
            }
        }
        catch { }
        int q = SafeQuality();
        if (q != lastQuality) { writer?.Add(new { k = "quality", ms = Round(now), quality = q, qualityName = SafeQualityName() }); lastQuality = q; }
        writer?.Add(new
        {
            k = "sec", ms = Round(now), spanMs = Round(span), frames = second.Frames,
            fps = Round(second.Frames * 1000.0 / span), maxGapMs = Round(second.MaxGap), slow25 = second.Slow, gaps50 = second.Gaps,
            tileReq = second.TileReq, tileArrive = second.TileArrive, tileUnload = second.TileUnloadLoaded,
            tileCancel = second.TileUnloadPending, tileDispose = second.TileDisposeLoaded, churn5s = second.TileChurn,
            avgLatencyMs = second.TileArrive > 0 ? Round(second.SumArrivalLatency / second.TileArrive) : 0,
            maxLatencyMs = Round(second.MaxArrivalLatency), maxCompleteMs = Round(second.MaxCompleteMs),
            inFlight = InFlight(), trackedTiles = tiles.Count, otherReq = second.OtherReq,
            reqByCat = new Dictionary<string, int>(second.ReqByCategory),
            npcBoats = second.NpcBoats, npcBoatMs = Round(second.NpcBoatMs), areaEvents = second.AreaEvents,
            shipCreate = second.ShipCreate, shipCreateMs = Round(second.ShipCreateMs), shipCreateMaxMs = Round(second.ShipCreateMaxMs),
            il2cppUsedMB = Round(SafeUsed() / 1048576.0),
            gcCount = lastGcCount, heapMB = SafeHeapMB(), cam, camSpeed, battle = SafeBattle(),
            suppressedTileEvents
        });
        secondStart = now; second.Clear(); tileEventsThisSecond = 0; suppressedTileEvents = 0;
    }

    // ---------------- helpers ----------------
    private static void TileEvent(object row)
    {
        if (++tileEventsThisSecond > MaxTileEventsPerSecond) { suppressedTileEvents++; return; }
        writer?.Add(row);
    }
    private static int InFlight()
    {
        int n = 0;
        foreach (var t in tiles.Values) if (t.Arrived < 0) n++;
        return n;
    }
    private static string Category(string key)
    {
        const string root = "assets/assetspackage/";
        string k = key.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? key.Substring(root.Length) : key;
        var parts = k.Split('/');
        if (parts.Length >= 3 && (parts[0] == "fbx" || parts[0] == "prefab" || parts[0] == "effect")) return parts[0] + "/" + parts[1];
        return parts.Length >= 2 ? parts[0] : "(other)";
    }
    private static string ShortKey(string key)
    {
        int i = key.LastIndexOf('/');
        string name = i >= 0 ? key[(i + 1)..] : key;
        return key.Contains("terraintilemodel", StringComparison.OrdinalIgnoreCase) ? "M:" + name
             : key.Contains("terraintile", StringComparison.OrdinalIgnoreCase) ? "T:" + name : name;
    }
    private static object P(UnityEngine.Vector3 v) => new[] { Math.Round(v.x, 1), Math.Round(v.y, 1), Math.Round(v.z, 1) };
    private static object? Viewport(UnityEngine.Vector3 world)
    {
        try
        {
            var c = UnityEngine.Camera.main;
            if (c == null) return null;
            var v = c.WorldToViewportPoint(world);
            return new[] { Math.Round(v.x, 2), Math.Round(v.y, 2), Math.Round(v.z, 1) };
        }
        catch { return null; }
    }
    private static string? SafeName(UnityEngine.GameObject? go) { try { return go?.name; } catch { return null; } }
    private static bool IsOcean()
    {
        try { var s = SceneManager.Instance; return s != null && s.IsInOceanScene; } catch { return false; }
    }
    private static bool? SafeBattle()
    {
        try { return SceneManager.Instance?.CurScene()?.TryCast<OceanScene>()?.IsInBattle; } catch { return null; }
    }
    private static int SafeGc() { try { return Il2CppSystem.GC.CollectionCount(0); } catch { return -1; } }
    private static long SafeUsed() { try { return Il2CppInterop.Runtime.IL2CPP.il2cpp_gc_get_used_size(); } catch { return 0; } }
    private static long SafeHeapMB() { try { return Il2CppSystem.GC.GetTotalMemory(false) / 1048576; } catch { return -1; } }
    private static int SafeQuality() { try { return UnityEngine.QualitySettings.GetQualityLevel(); } catch { return -1; } }
    private static string? SafeQualityName()
    {
        try { var names = UnityEngine.QualitySettings.names; int q = UnityEngine.QualitySettings.GetQualityLevel(); return q >= 0 && q < names.Length ? names[q] : null; }
        catch { return null; }
    }
    private static void DiagnosticError(string where, Exception ex)
    {
        enabled = false;
        writer?.Add(new { k = "diagnostic_error", ms = Round(TraceWriter.Ms), where, error = ex.GetType().Name + ": " + ex.Message });
    }
    private void WarnOnce()
    {
        if (warned || writer == null) return;
        warned = true;
        LoggerInstance.Warning("SailTrace recording stopped. " + (writer.Failure ?? "See diagnostic_error in the JSONL."));
    }

    public override void OnDeinitializeMelon()
    {
        try { if (inOcean) FlushSecond(TraceWriter.Ms); } catch { }
        enabled = false;
        try { HarmonyInstance.UnpatchSelf(); } catch { }
        writer?.Add(new { k = "session_end", ms = Round(TraceWriter.Ms), dropped = writer.Dropped });
        writer?.Dispose();
    }
}

using System.Reflection;
using Restitutor.TabCharacters;
using Il2CppClient.UILogic.UICharacter;
using Il2CppFairyGUI;

int checks = 0;
void Check(bool value, string message) { ++checks; if (!value) throw new Exception(message); }
void Set(string field, object? value) => typeof(EntryPoint).GetField(field, BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, value);
var host = new EntryPoint();
void Enable() { Set("host", host); Set("mainThread", Environment.CurrentManagedThreadId); Set("enabled", true); Set("recoveryPending", false); }
Enable();

// Independent chunking oracle, including both boundaries and every partial final page.
for (int count = 0; count <= 211; ++count)
{
    var expected = Enumerable.Range(0, count).Chunk(10).ToArray();
    if (expected.Length == 0) expected = new[] { Array.Empty<int>() };
    var p = new PageWindow(); p.Reconcile(count);
    Check(!p.Move(-1), "left boundary");
    for (int page = 0; page < expected.Length; ++page)
    {
        Check(Enumerable.Range(0, p.VisibleCount).Select(p.GlobalIndex).SequenceEqual(expected[page]), "exact page members");
        Check(p.GlobalIndex(-1) == -1 && p.GlobalIndex(p.VisibleCount) == -1, "invalid slot guard");
        if (page + 1 < expected.Length) Check(p.Move(1), "advance to next chunk");
    }
    Check(!p.Move(1), "right boundary does not wrap");
    for (int page = expected.Length - 1; page > 0; --page)
    {
        Check(p.Move(-1), "backward");
        Check(Enumerable.Range(0, p.VisibleCount).Select(p.GlobalIndex).SequenceEqual(expected[page - 1]), "backward members");
    }
}
var shrink = new PageWindow(); shrink.Reconcile(25); shrink.Move(1); shrink.Move(1); shrink.Reconcile(12);
Check(shrink.Start == 10 && shrink.VisibleCount == 2, "roster shrink clamps page");
shrink.Reconcile(0); Check(shrink.Start == 0 && shrink.VisibleCount == 0, "empty roster");
shrink.Reconcile(21); Check(shrink.Start == 0 && shrink.VisibleCount == 10, "empty then grow");

var view = new UICharacterView();
var model = view._model;
model.ListRole = Enumerable.Range(100, 25).ToList();
model.RoleIndex = 3;
var ctrl = new UICharacterCtrl(view);
var list = view._UIContent_k__BackingField!.listRole;
var roster = model.ListRole;
var nativeRenderer = (ListItemRenderer)(Action<int, GObject>)((index, row) => {
    row.CharacterIndex = index;
    row.Click = () => { model.RoleIndex = index; view.Refresh(); };
});
list.itemRenderer = nativeRenderer;
view.Refresh();
Check(list.numItems == 10 && list.Rows[0].CharacterIndex == 0, "initial ten");
Check(model.RoleIndex == 3 && ReferenceEquals(roster, model.ListRole) && model.ListRole.Count == 25, "selection and roster unchanged");
Check(!list.scrollPane.touchEffect && !list.scrollPane.mouseWheelEnabled && !list.scrollItemToViewOnClick, "no wheel drag click scroll");
Check(list.ScrollCalls == 0 && list.scrollPane.X == 0 && !list.scrollPane.Tweening, "no automatic scroll or animation");
int details = view.DetailsRefreshes, calls = list.RenderCalls;
ctrl.OnClickBtnLeft(); Check(list.RenderCalls == calls, "left edge does no rerender");
ctrl.OnClickBtnRight();
Check(list.Rows.Select(x => x.CharacterIndex).SequenceEqual(Enumerable.Range(10, 10)), "next ten global bindings");
Check(model.RoleIndex == 3 && view.DetailsRefreshes == details, "paging does not change selection or refresh details");
list.Rows[4].Click!();
Check(model.RoleIndex == 14 && list.Rows[0].CharacterIndex == 10, "click actual character 14 and preserve page");
Check(view.DetailsRefreshes == details + 1 && list.ScrollCalls == 0, "details refresh without scrolling");
ctrl.OnClickBtnRight();
Check(list.numItems == 5 && list.Rows[0].CharacterIndex == 20 && list.Rows[4].CharacterIndex == 24, "last partial page");
calls = list.RenderCalls; ctrl.OnClickBtnRight(); Check(list.RenderCalls == calls, "right edge does no rerender");
Check(model.RoleIndex == 14, "off-page selection retained");
Check(list.HandleArrowKey(1) == -1 && model.RoleIndex == 14, "arrow cannot select portrait");

var unrelated = new GList(); unrelated.numItems = 31; unrelated.ScrollToView(20, false, false);
Check(unrelated.numItems == 31 && unrelated.ScrollCalls == 1 && unrelated.HandleArrowKey(1) == 99, "unrelated lists untouched");
model.IsSelectSkillFilter = true; ctrl.OnClickBtnLeft(); Check(list.Rows[0].CharacterIndex == 20, "filter input guard");
model.IsSelectSkillFilter = false; view.IsInputActive = false; ctrl.OnClickBtnLeft(); Check(list.Rows[0].CharacterIndex == 20, "inactive view guard");
view.IsInputActive = true;
model.ListRole.RemoveRange(12, 13); model.RoleIndex = 3; view.Refresh();
Check(list.numItems == 2 && list.Rows[0].CharacterIndex == 10, "live roster shrink");
model.ListRole.Reverse(); view.Refresh(); list.Rows[1].Click!();
Check(model.RoleIndex == 11, "rebinding after roster reorder");
ctrl.ShowSheet(ESheetType.Skill);
Check(ctrl._Model_k__BackingField.SheetType == ESheetType.Character && list.numItems == 10, "retired skill sheet request opens the paginated Characters sheet");
ctrl.ShowSheet(ESheetType.Equip);
Check(ctrl._Model_k__BackingField.SheetType == ESheetType.Character && list.numItems == 10, "retired equipment sheet request opens the paginated Characters sheet");
// 0.6.4: both other sheets are retired; a test-only sheet value keeps the restore path covered.
ctrl.ShowSheet(ESheetType.Other);
Check(list.numItems == 12 && list.itemRenderer == nativeRenderer && list.scrollPane.touchEffect && list.scrollPane.mouseWheelEnabled, "restore on a non-Characters sheet");
Check(list.selectionMode == ListSelectionMode.Single && list.scrollItemToViewOnClick, "restore list selection flags");
ctrl.ShowSheet(ESheetType.Character);
Check(list.numItems == 10 && list.Rows[0].CharacterIndex == 0, "fresh page on reenter");
ctrl.OnClickBtnRight(); view.HideHook();
Check(list.numItems == 0 && list.itemRenderer == nativeRenderer, "hide clears binding and restores renderer");
view.Refresh(); Check(list.numItems == 10 && list.Rows[0].CharacterIndex == 0, "reopen page reset");
list.Dispose(); Check(list.isDisposed && list.itemRenderer == nativeRenderer, "dispose restores delegate without repopulation");
Check(typeof(EntryPoint).GetField("state", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) == null, "dispose releases view references");

var broken = new UICharacterView(); broken._model.ListRole = new() { 1, 2 };
int invokes = 0;
broken._UIContent_k__BackingField!.listRole.itemRenderer = (ListItemRenderer)(Action<int, GObject>)((_, _) => { if (++invokes == 1) throw new Exception("test renderer failure"); });
broken.Refresh(); host.OnUpdate();
Check(broken._UIContent_k__BackingField.listRole.numItems == 2 && broken._UIContent_k__BackingField.listRole.scrollPane.touchEffect, "deferred recovery restores original list");
Check(!(bool)typeof(EntryPoint).GetField("enabled", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!, "failure disables session");
var originalError = new Exception("native error");
Check(ReferenceEquals(Hook.Call("RefreshFault", originalError), originalError), "original exception preserved");
host.OnDeinitializeMelon();


// Resource cleanup: loaded/pending paths and idempotence, using real cleanup code with loader doubles.
Enable(); Set("cleanupEnabled", true);
var pendingLoader = new Il2CppCore.NewUISystem.MyGLoader { url = "pending", _handler = new object() };
var cleaned = PortraitResources.ClearLoader(pendingLoader);
Check(cleaned.Urls == 1 && cleaned.Pending == 1 && pendingLoader._handler == null && pendingLoader.Releases == 1, "pending operation explicitly released");
PortraitResources.ClearLoader(pendingLoader);
Check(pendingLoader.Releases == 1, "pending cleanup idempotent");
var readyLoader = new Il2CppCore.NewUISystem.MyGLoader { url = "ready", Loaded = true, _handler = new object() };
cleaned = PortraitResources.ClearLoader(readyLoader);
Check(cleaned.Urls == 1 && cleaned.Pending == 0 && readyLoader.Releases == 1, "loaded URL clears through original release only once");
var emptyPending = new Il2CppCore.NewUISystem.MyGLoader { _handler = new object() };
Check(PortraitResources.ClearLoader(emptyPending).Pending == 1 && emptyPending.Releases == 1, "empty URL pending operation cleared");
var disposedLoader = new Il2CppCore.NewUISystem.MyGLoader { url = "disposed", isDisposed = true, _handler = new object() };
PortraitResources.ClearLoader(disposedLoader);
Check(disposedLoader.url == "disposed" && disposedLoader.Releases == 0, "disposed loader untouched");
var ordinaryLoader = new GLoader { url = "ui://package/icon" };
PortraitResources.ClearLoader(ordinaryLoader);
Check(ordinaryLoader.url == "", "base loader only uses original URL clearing");

var resourceView = new UICharacterView(); resourceView._model.ListRole = Enumerable.Range(0, 25).ToList();
var resourceList = resourceView._UIContent_k__BackingField!.listRole;
var resourceCtrl = new UICharacterCtrl(resourceView);
resourceList.itemRenderer = (ListItemRenderer)(Action<int, GObject>)((index, obj) => {
    var loader = obj.TryCast<Il2CppCharacter.UIbtnRole>()!.loaderRole.TryCast<Il2CppCore.NewUISystem.MyGLoader>()!;
    loader.url = "portrait/" + index; loader._handler = new object(); loader.Loaded = true;
    obj.CharacterIndex = index;
});
var stale = new Il2CppCharacter.UIbtnRole();
stale.loaderRole.url = "old-pool";
resourceList.itemPool._pool["portraits"].Enqueue(stale);
var unrelatedPooled = new GLoader { url = "not-a-role-row" };
resourceList.itemPool._pool["other"] = new(); resourceList.itemPool._pool["other"].Enqueue(unrelatedPooled);
resourceView.Refresh();
Check(resourceList.Rows.All(r => r.TryCast<Il2CppCharacter.UIbtnRole>()!.loaderRole.url != ""), "visible portrait URLs retained");
Check(unrelatedPooled.url == "not-a-role-row", "other pooled item type untouched");
var large = resourceView._UIContent_k__BackingField.loaderRole.TryCast<Il2CppCore.NewUISystem.MyGLoader>()!;
var tooltip = resourceView._UIContent_k__BackingField.roleInfo.loaderRole.TryCast<Il2CppCore.NewUISystem.MyGLoader>()!;
large.url = "large-selected"; large._handler = new(); large.Loaded = true;
tooltip.url = "tip-selected"; tooltip._handler = new(); // pending tooltip
resourceCtrl.OnClickBtnRight(); resourceCtrl.OnClickBtnRight();
var idleRows = resourceList.itemPool._pool["portraits"].ToArray();
Check(resourceList.Rows.Count == 5 && idleRows.Length == 5, "short page returns excess rows to pool");
Check(idleRows.All(r => r.TryCast<Il2CppCharacter.UIbtnRole>()!.loaderRole.url == ""), "pooled portrait URLs cleared");
Check(idleRows.All(r => r.TryCast<Il2CppCharacter.UIbtnRole>()!.loaderRole.TryCast<Il2CppCore.NewUISystem.MyGLoader>()!._handler == null), "pooled handlers released");
Check(large.url == "large-selected" && tooltip.url == "tip-selected" && large.Releases == 0 && tooltip.Releases == 0, "page turns preserve selected detail and pending tooltip");
resourceCtrl.OnClickBtnLeft();
Check(resourceList.Rows.Count == 10 && resourceList.Rows.All(r => r.TryCast<Il2CppCharacter.UIbtnRole>()!.loaderRole.url == "portrait/" + r.CharacterIndex), "reuse blank pool rows correctly reloads portraits");
resourceCtrl.ShowSheet(ESheetType.Other);
Check(large.url == "large-selected" && tooltip.url == "tip-selected", "tab switch preserves details");
Check(resourceList.Rows.Count == 25, "non-Characters sheet original full list");
resourceCtrl.ShowSheet(ESheetType.Character);
Check(resourceList.Rows.Count == 10 && resourceList.itemPool._pool["portraits"].Count == 15, "return from full list caps live rows");
Check(resourceList.itemPool._pool["portraits"].All(r => r.TryCast<Il2CppCharacter.UIbtnRole>()!.loaderRole.url == ""), "excess rows from other tab cleared");
// Defensive skip if an externally corrupted pool also lists an attached row.
var attached = resourceList.Rows[0]; resourceList.itemPool._pool["attached"] = new(); resourceList.itemPool._pool["attached"].Enqueue(attached);
PortraitResources.ClearPool(resourceList);
Check(attached.TryCast<Il2CppCharacter.UIbtnRole>()!.loaderRole.url != "", "attached row never cleaned even if found in pool");
resourceList.itemPool._pool.Remove("attached");
resourceView.HideHook();
Check(resourceList.Rows.Count == 0 && resourceList.itemPool._pool["portraits"].All(r => r.TryCast<Il2CppCharacter.UIbtnRole>()!.loaderRole.url == ""), "close releases all pooled portraits without destroying row objects");
Check(large.url == "" && tooltip.url == "" && large.Releases == 1 && tooltip.Releases == 1, "close clears large and pending tooltip once");
resourceView.Refresh();
Check(resourceList.Rows.Count == 10 && resourceList.Rows[0].TryCast<Il2CppCharacter.UIbtnRole>()!.loaderRole.url == "portrait/0", "reopen portraits restore from original renderer");
resourceView.HideHook(); Check(large.Releases == 1 && tooltip.Releases == 1, "repeated close does not double release");
// A cleanup failure is isolated from pagination and selection behavior.
var failedRow = new Il2CppCharacter.UIbtnRole(); failedRow.loaderRole.url = "failing"; failedRow.loaderRole.ThrowOnClear = true;
resourceList.itemPool._pool["fault"] = new(); resourceList.itemPool._pool["fault"].Enqueue(failedRow);
resourceView.Refresh();
Check(!(bool)typeof(EntryPoint).GetField("cleanupEnabled", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!, "cleanup error disables cleanup only");
Check((bool)typeof(EntryPoint).GetField("enabled", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!, "pagination remains enabled after cleanup failure");
resourceCtrl.OnClickBtnRight(); Check(resourceList.Rows[0].CharacterIndex == 10, "paging still works after cleanup failure");
host.OnDeinitializeMelon();
Console.WriteLine($"PASS {checks} managed checks including native API doubles for pending/completed load release, pool reuse, tab/close boundaries, isolation and error recovery. No game code executed.");

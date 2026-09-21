# 0.2.1 현재 계약 보완

등록은 아래 0.2.0의 40개와 동일하다. SharedMap.PortRefresh 후처리 및 자체 경유지 갱신에서 소유 항로 세션의 도달 불가 표시를 적용한다. 추가 공유 상태는 UIHarbourIcon.loaderIcon.url / ctrlSelfPort.selectedIndex이며 원래 UpdateInfo 후 기준을 보관해 도달 가능 전환과 종료/정리에서 복원한다. 원본 UIMapLineCtrl을 호출하지 않는다. [0.2.1 근거](0.2.1.md).

# 0.2.0 후보 패치 (기존 설치본과 별개)
총 40개 등록. 공유 상태: UIManager 포커스, UIMenuHelper 안내, 경유지 목록/항로 표시, 지도 팁/깃발, 전환과 지연 종료. routeMode/소유 세션/포커스로 입력을 제한한다. [변경 계약과 근거](0.2.0-candidate.md).

```csharp
        EntryPoint.Hook(typeof(UIMapCtrl), "InitUIMask", typeof(FogLifetime), nameof(BeforeInit), nameof(AfterInit));
        EntryPoint.Hook(typeof(PlayerAreaDB), "Deserialize", typeof(FogLifetime), nameof(Reset));
        EntryPoint.Hook(typeof(PlayerAreaDB), "InitHook", typeof(FogLifetime), nameof(Reset));
        EntryPoint.Hook(typeof(MapMaskManager), "Dispose", typeof(FogLifetime), nameof(Reset));
        EntryPoint.Hook(typeof(MapMaskManager), "SetMaskData", typeof(FogLifetime), nameof(Reset));
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
        H(typeof(InputSystemManager), "OnEventCaptureInput", nameof(CaptureClose));
        EntryPoint.Hook(typeof(UIMapView), "Refresh", typeof(SharedMap), after: nameof(MapRefresh));
        EntryPoint.Hook(typeof(UIMapHarbourIcon), "UpdateInfo", typeof(SharedMap), after: nameof(PortRefresh));
        EntryPoint.Hook(typeof(UIMapHandler), "TouchPortOrAreaListUI", typeof(SharedMap), after: nameof(OverList));
```

## 아래는 기존 0.1.5 목록 (보존)
# 현재 패치 등록

현재 소스의 등록식과 기존 native-map 메타데이터 서명을 결합한 정적 목록. 실제 설치 성공이나 네이티브 본문 의미를 자동 검증한 것이 아니다. 서명이 여러 개 또는 없으면 변경 전에 해결해야 한다. Prefix/Postfix는 등록식과 CURRENT 지침을 함께 읽는다.

## UIMapCtrl.InitUIMask
[소스](../../../Map/src/FogLifetime.cs) 줄 14
`EntryPoint.Hook(typeof(UIMapCtrl), "InitUIMask", typeof(FogLifetime), nameof(BeforeInit), nameof(AfterInit));`
- `System.Void Client.UILogic.UIMap.UIMapCtrl::InitUIMask()` — RVA `0x5D4410`
## PlayerAreaDB.Deserialize
[소스](../../../Map/src/FogLifetime.cs) 줄 15
`EntryPoint.Hook(typeof(PlayerAreaDB), "Deserialize", typeof(FogLifetime), nameof(Reset));`
- `System.Void Client.PlayerStore.PlayerAreaDB::Deserialize(Client.Manager.PlayerDataSerializer)` — RVA `0x785FF0`
## PlayerAreaDB.InitHook
[소스](../../../Map/src/FogLifetime.cs) 줄 17
`EntryPoint.Hook(typeof(PlayerAreaDB), "InitHook", typeof(FogLifetime), nameof(Reset));`
- `System.Void Client.PlayerStore.PlayerAreaDB::InitHook()` — RVA `0x786270`
## MapMaskManager.Dispose
[소스](../../../Map/src/FogLifetime.cs) 줄 18
`EntryPoint.Hook(typeof(MapMaskManager), "Dispose", typeof(FogLifetime), nameof(Reset));`
- `System.Void Client.Manager.MapMaskManager::Dispose()` — RVA `0x6B6AF0`
## MapMaskManager.SetMaskData
[소스](../../../Map/src/FogLifetime.cs) 줄 19
`EntryPoint.Hook(typeof(MapMaskManager), "SetMaskData", typeof(FogLifetime), nameof(Reset));`
- `System.Void Client.Manager.MapMaskManager::SetMaskData(System.Byte[])` — RVA `0x6B6CD0`
## UISailLineCtrl.ShowHook
[소스](../../../Map/src/UnifiedRoute.cs) 줄 34
`EntryPoint.Hook(typeof(UISailLineCtrl), "ShowHook", typeof(UnifiedRoute), after: nameof(Request));`
- `System.Void Client.UILogic.UISailReady.UISailLineCtrl::ShowHook()` — RVA `0xD2FE40`
## UISailLineCtrl.CloseHook
[소스](../../../Map/src/UnifiedRoute.cs) 줄 35
`EntryPoint.Hook(typeof(UISailLineCtrl), "CloseHook", typeof(UnifiedRoute), before: nameof(PlannerClosing));`
- `System.Void Client.UILogic.UISailReady.UISailLineCtrl::CloseHook()` — RVA `0xD35060`
## UISailLineCtrl.OnAction_A
[소스](../../../Map/src/UnifiedRoute.cs) 줄 36
`EntryPoint.Hook(typeof(UISailLineCtrl), "OnAction_A", typeof(UnifiedRoute), before: nameof(PlannerSelect), args: Type.EmptyTypes);`
- `System.Void Client.UILogic.UISailReady.UISailLineCtrl::OnAction_A()` — RVA `0xD31E40`
## UISailLineCtrl.OnAction_A
[소스](../../../Map/src/UnifiedRoute.cs) 줄 37
`EntryPoint.Hook(typeof(UISailLineCtrl), "OnAction_A", typeof(UnifiedRoute), before: nameof(BeforeNativeSelect), after: nameof(AfterNativeSelect), args: new[] { typeof(int) });`
- `System.Void Client.UILogic.UISailReady.UISailLineCtrl::OnAction_A(System.Int32)` — RVA `0xD31EE0`
## UISailLineCtrl.OnAction_B
[소스](../../../Map/src/UnifiedRoute.cs) 줄 38
`EntryPoint.Hook(typeof(UISailLineCtrl), "OnAction_B", typeof(UnifiedRoute), after: nameof(PlannerChanged));`
- `System.Void Client.UILogic.UISailReady.UISailLineCtrl::OnAction_B()` — RVA `0xD34570`
## UISailLineCtrl.ClearSelectPort
[소스](../../../Map/src/UnifiedRoute.cs) 줄 39
`EntryPoint.Hook(typeof(UISailLineCtrl), "ClearSelectPort", typeof(UnifiedRoute), after: nameof(PlannerChanged));`
- `System.Void Client.UILogic.UISailReady.UISailLineCtrl::ClearSelectPort()` — RVA `0xD333B0`
## UISailLineCtrl.AutoSail
[소스](../../../Map/src/UnifiedRoute.cs) 줄 40
`EntryPoint.Hook(typeof(UISailLineCtrl), "AutoSail", typeof(UnifiedRoute), before: nameof(BeforeDeparture));`
- `System.Void Client.UILogic.UISailReady.UISailLineCtrl::AutoSail()` — RVA `0xD33F40`
## UIMapCtrl.CloseHook
[소스](../../../Map/src/UnifiedRoute.cs) 줄 41
`EntryPoint.Hook(typeof(UIMapCtrl), "CloseHook", typeof(UnifiedRoute), before: nameof(MapClosing));`
- `System.Void Client.UILogic.UIMap.UIMapCtrl::CloseHook()` — RVA `0x5DB470`
## UIMapCtrl.DisposeHook
[소스](../../../Map/src/UnifiedRoute.cs) 줄 42
`EntryPoint.Hook(typeof(UIMapCtrl), "DisposeHook", typeof(UnifiedRoute), before: nameof(MapClosing));`
- `System.Void Client.UILogic.UIMap.UIMapCtrl::DisposeHook()` — RVA `0x5D3C30`
## UIBase.get_IsInputActive
[소스](../../../Map/src/UnifiedRoute.cs) 줄 43
`EntryPoint.Hook(typeof(UIBase), "get_IsInputActive", typeof(UnifiedRoute), before: nameof(FilterInput));`
- `System.Boolean Core.NewUISystem.UIBase::get_IsInputActive()` — RVA `0xB106A0`
## UIMapLineCtrl.UpdateUI
[소스](../../../Map/src/UnifiedRoute.cs) 줄 44
`EntryPoint.Hook(typeof(UIMapLineCtrl), "UpdateUI", typeof(UnifiedRoute), before: nameof(FilterOldMapUpdate));`
- `System.Void Client.UILogic.UIMap.UIMapLineCtrl::UpdateUI()` — RVA `0x5E9990`
## UIMapLineCtrl.SetFocusPortIcon
[소스](../../../Map/src/UnifiedRoute.cs) 줄 45
`EntryPoint.Hook(typeof(UIMapLineCtrl), "SetFocusPortIcon", typeof(UnifiedRoute), before: nameof(FocusPort));`
- `System.Void Client.UILogic.UIMap.UIMapLineCtrl::SetFocusPortIcon(System.Int32)` — RVA `0x5EAB10`
## UIMapLineCtrl.SetMapFocusOnPlayer
[소스](../../../Map/src/UnifiedRoute.cs) 줄 46
`EntryPoint.Hook(typeof(UIMapLineCtrl), "SetMapFocusOnPlayer", typeof(UnifiedRoute), before: nameof(FocusPlayer));`
- `System.Void Client.UILogic.UIMap.UIMapLineCtrl::SetMapFocusOnPlayer()` — RVA `0x5E9480`
## UIMapLineCtrl.EnterArea
[소스](../../../Map/src/UnifiedRoute.cs) 줄 47
`EntryPoint.Hook(typeof(UIMapLineCtrl), "EnterArea", typeof(UnifiedRoute), before: nameof(EnterArea));`
- `System.Void Client.UILogic.UIMap.UIMapLineCtrl::EnterArea(System.Int32)` — RVA `0x5E9350`
## UIMapLineCtrl.GetCurrentSelectPort
[소스](../../../Map/src/UnifiedRoute.cs) 줄 48
`EntryPoint.Hook(typeof(UIMapLineCtrl), "GetCurrentSelectPort", typeof(UnifiedRoute), before: nameof(CurrentRoutePort));`
- `Client.UILogic.UIMap.UIMapIcon.UIMapLineHarbourIcon Client.UILogic.UIMap.UIMapLineCtrl::GetCurrentSelectPort()` — RVA `0x5EAF10`
## UIMapLineCtrl.GetSelectHarbourId
[소스](../../../Map/src/UnifiedRoute.cs) 줄 49
`EntryPoint.Hook(typeof(UIMapLineCtrl), "GetSelectHarbourId", typeof(UnifiedRoute), before: nameof(CurrentRoutePortId));`
- `System.Int32 Client.UILogic.UIMap.UIMapLineCtrl::GetSelectHarbourId()` — RVA `0x5EAC80`
## UIMapCtrl.OnAction_A
[소스](../../../Map/src/UnifiedRoute.cs) 줄 50
`EntryPoint.Hook(typeof(UIMapCtrl), "OnAction_A", typeof(UnifiedRoute), before: nameof(SuppressMapAction));`
- `System.Void Client.UILogic.UIMap.UIMapCtrl::OnAction_A()` — RVA `0x5DD290`
## UIMapCtrl.OnAction_B
[소스](../../../Map/src/UnifiedRoute.cs) 줄 53
`EntryPoint.Hook(typeof(UIMapCtrl), "OnAction_B", typeof(UnifiedRoute), before: nameof(SuppressMapAction));`
- `System.Void Client.UILogic.UIMap.UIMapCtrl::OnAction_B()` — RVA `0x5DD430`
## UIMapCtrl.OnAction_X
[소스](../../../Map/src/UnifiedRoute.cs) 줄 54
`EntryPoint.Hook(typeof(UIMapCtrl), "OnAction_X", typeof(UnifiedRoute), before: nameof(SuppressMapAction));`
- `System.Void Client.UILogic.UIMap.UIMapCtrl::OnAction_X()` — RVA `0x5DD130`
## UIMapCtrl.ClickPortCheckLine
[소스](../../../Map/src/UnifiedRoute.cs) 줄 55
`EntryPoint.Hook(typeof(UIMapCtrl), "ClickPortCheckLine", typeof(UnifiedRoute), before: nameof(ClickPort));`
- `System.Void Client.UILogic.UIMap.UIMapCtrl::ClickPortCheckLine(System.Int32)` — RVA `0x5E0820`
## UIMapCtrl.SetLane
[소스](../../../Map/src/UnifiedRoute.cs) 줄 56
`EntryPoint.Hook(typeof(UIMapCtrl), "SetLane", typeof(UnifiedRoute), before: nameof(SetRouteLanes), args: Type.EmptyTypes);`
- `System.Void Client.UILogic.UIMap.UIMapCtrl::SetLane()` — RVA `0x5D9E30`
## UIMapCtrl.UpdateUI
[소스](../../../Map/src/UnifiedRoute.cs) 줄 57
`EntryPoint.Hook(typeof(UIMapCtrl), "UpdateUI", typeof(UnifiedRoute), after: nameof(MapUpdated));`
- `System.Void Client.UILogic.UIMap.UIMapCtrl::UpdateUI()` — RVA `0x5DEE50`
## UIMapHandler.TouchPortOrAreaListUI
[소스](../../../Map/src/UnifiedRoute.cs) 줄 58
`EntryPoint.Hook(typeof(UIMapHandler), "TouchPortOrAreaListUI", typeof(UnifiedRoute), after: nameof(OverPlannerList));`
- `System.Boolean Client.UILogic.UIMap.UIMapHandler::TouchPortOrAreaListUI()` — RVA `0x5E3750`
## UIMapView.Refresh
[소스](../../../Map/src/UnifiedRoute.cs) 줄 59
`EntryPoint.Hook(typeof(UIMapView), "Refresh", typeof(UnifiedRoute), after: nameof(MapRefreshed));`
- `System.Void Client.UILogic.UIMap.UIMapView::Refresh()` — RVA `0x7BC1A0`
## UIMapHarbourIcon.UpdateInfo
[소스](../../../Map/src/UnifiedRoute.cs) 줄 60
`EntryPoint.Hook(typeof(UIMapHarbourIcon), "UpdateInfo", typeof(UnifiedRoute), after: nameof(PortRefreshed));`
- `System.Void Client.UILogic.UIMap.UIMapIcon.UIMapHarbourIcon::UpdateInfo()` — RVA `0x5E47C0`
## UIMapHarbourIcon._InitComponent_b__16_0
[소스](../../../Map/src/UnifiedRoute.cs) 줄 61
`EntryPoint.Hook(typeof(UIMapHarbourIcon), "_InitComponent_b__16_0", typeof(UnifiedRoute), before: nameof(PortClicked), args: Type.EmptyTypes);`
- `System.Void Client.UILogic.UIMap.UIMapIcon.UIMapHarbourIcon::<InitComponent>b__16_0()` — RVA `0x5E6670`
## InputSystemManager.OnEventCaptureInput
[소스](../../../Map/src/UnifiedRoute.cs) 줄 62
`EntryPoint.Hook(typeof(InputSystemManager), "OnEventCaptureInput", typeof(UnifiedRoute), before: nameof(CaptureClose));`
- `System.Void Core.InputSystem.InputSystemManager::OnEventCaptureInput(UnityEngine.InputSystem.InputAction/CallbackContext)` — RVA `0x8C50C0`


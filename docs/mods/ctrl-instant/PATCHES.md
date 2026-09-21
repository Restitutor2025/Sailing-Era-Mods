# CTRL Instant 0.1.6 계약

진단 훅을 모두 제거했다. 남은 것은 기능 훅 7개로, `UISailInputAgent.OnFeatureInputActionStart`(Postfix)·`Perform`(Prefix)·`End`(Finalizer), `UISailingCtrl.CloseHook`·`DisposeHook`(Prefix), `OceanSceneSailingState.Exit`(Prefix), `GameManager.ForceReset`(Postfix)이다. 정지·선택·선단 창 인계 계약은 0.1.5와 같다. 파일 쓰기는 없다. [근거](0.1.6.md)

## 이전 계약

# CTRL Instant 0.1.5 계약

훅 30개와 등록 시점은 0.1.4와 같다. 정지 유지 규칙만 바뀌었다. 현재 포커스가 `Client.UILogic.UINpcInteractive.UINpcInteractiveCtrl`이면 포커스 변경으로 해제하지 않는다. 그 동안에는 자체 선택 토큰의 소유권을 창에 넘겨 모드가 닫지 않게 하고(원본 `CloseHook`이 닫는다), 재선택도 하지 않는다. 공유 상태 쓰기는 0.1.2와 같다(자체 PauseGame/ContinueGame, 원본 OnAction_CheckTarget_Long/CloseCheckNpcStatus 호출). [근거](0.1.5.md)

## 이전 계약

# CTRL Instant 0.1.4 계약

0.1.3 계약과 같다. 다른 점은 등록 시점뿐이다. 시작 시점에는 기능 훅 8개와 `OceanSceneSailingState.Enter`(0x76FCC0) Postfix 트리거 1개만 등록한다. 진단 훅 21개는 첫 항해 진입 뒤 `OnUpdate`에서 한 번만 등록한다. 총 30개. 이유: 0.1.3처럼 시작 시점에 등록하면 검은 화면이 난다(사용자 확인). [근거](0.1.4.md)

## 이전 계약

# CTRL Instant 0.1.3 진단 계약

기능 훅 8개와 계약은 0.1.2와 같다. 진단 훅 21개를 추가해 총 29개다. 모두 void Prefix/Postfix/Finalizer이며 원본 인수·반환·예외를 바꾸지 않는다. 등록 실패는 `DIAG_HOOK_FAIL`로만 기록하고 기능 훅을 해제하지 않는다.

| 원본 타입 / 함수 | RVA | 형태 / 용도 |
|---|---|---|
| UISailInputAgent.OnFeatureInputActionEnd | 0xD2EAA0 | Prefix 추가(깊이 +1). 기존 Finalizer `Ended`는 첫 줄에서 깊이 −1 |
| GameManager.PauseGame | 0x4D8CA0 | Postfix(`__result`). 관측 창 안에서 발급 ID·문맥 기록 |
| GameManager.ContinueGame(int) | 0x4D8E20 | Prefix. 관측 창 안에서 반환 ID·목록 포함·문맥 기록 |
| UISailingCtrl.OpenCheckNpcStatus | 0x90F5A0 | Prefix·Postfix. 선택 정지 열림 전후 |
| UISailingCtrl.CloseCheckNpcStatus | 0x90F620 | Prefix·Postfix(`__result`). 선택 정지 닫힘 전후 |
| UISailingCtrl.OnAction_CheckTarget_Long | 0x90FA40 | Prefix·Finalizer. 호출 깊이만 추적 |
| UISailingView.CheckRayShip | 0x985C80 | Prefix·Finalizer. 호출 깊이만 추적(매 프레임 호출 가능) |
| UINpcInteractiveCtrl.ShowNpcTeamInfo | 0xBE0AF0 | Prefix. 관측 창 시작(CTRL 없는 원본 경로 포함) |
| UINpcInteractiveCtrl.CloseHook | 0xBE2BC0 | Prefix·Finalizer. 창 닫힘과 호출 깊이 |
| UINpcInteractiveCtrl.CloseNpcInteractiveStatus / Trade / Battle / CheckInfo / CheckAndTriggerNpcEvent(int) | 0xBE0A40 / 0xBE1CB0 / 0xBE2100 / 0xBE16F0 / 0xBE0F80 | Prefix. 창 안의 단계 기록 |
| UIManager.SetFocusOnUIView / SetFocusOutUIView(IUIBaseCtrl, bool) | 0xA34A90 / 0xA34C30 | Prefix. 대상 창 타입 기록 |

29개 등록 대상 모두 현 native-map에서 유일한 선언이고 RVA를 공유하지 않는다(이번 세션에서 확인). 본문 SHA256과 interop 서명은 build.ps1의 extract.py·verify_interop.ps1로 확인해야 한다(미실행). 공유 상태에 쓰는 것은 없다. `GameManager.pauseList`, `UIManager.CurrentFocusViewCtrl`, `OceanScene._curDayTime/_secondsPerDay`, 선택 모델을 읽기만 한다. [근거](0.1.3.md)

## 이전 계약

# CTRL Instant 0.1.2

8개 대상과 Prefix/Postfix/Finalizer 형태는 동일. 시작은 정상 입력을 받은 유효한 UISailInputAgent와 현재 해양 장면 SceneInputAgent.IsOnFocused를 확인한다. HUD의 agent.IsOnFocused/View.IsOnFocused를 요구하지 않는다. 정지 유지 중에는 UIManager.CurrentFocusViewCtrl의 포인터가 자체 PauseGame/선단 선택 직후와 동일한지 확인한다. 새 UI 포커스 쓰기·전역 입력 훅·시간 배율 쓰기 없음. 정상 항해에서 HUD가 focused=False라는 사용자 로그에 근거한 수정이다.

원본 선택 호출에서 발생하는 동기 포커스 전환만 자체 경계로 갱신한다. 외부 창으로 이동한 후에는 재선택하지 않고 기존 토큰 정리 경로로 들어간다. 장면/포커스 진단을 추가했고 다른 UI 에이전트의 진단용 IsOnFocused 조회 예외는 unavailable로 격리한다. [근거와 한계](0.1.2.md).

## 이전 계약

# CTRL Instant 0.1.1 진단 계약

기존 7개 훅의 정지 정책 유지. `CnControls.XBaseInputAgent.OnEvtCaptureFeatureInput(int, InputAction.CallbackContext)` RVA `0x91B2A0`에 void Prefix 1개 추가, 총 8개. CheckTarget(117)만 관측하며 에이전트 실제 타입·phase·유효성·포커스를 기록한다. 원본 실행과 인자를 변경하지 않는다. 공통 입력 함수이므로 타입별 유효성 검사 전 호출 여부까지 볼 수 있다.

기존 시작/성립/끝과 토큰·수명주기에 로그 추가. OnUpdate의 CTRL 키 읽기는 진단 전용이며 새 정지를 시작하지 않는다. 누름/해제 전환 및 누름 중 초당 1회 기록, 실행당 최대 6,000줄. 로그 작성·스냅샷 예외를 격리하고 파일 실패 시 MelonLoader 로그 사용. Unity 시간/저장/전역 입력 설정 변경 없음.

[8개 현재 대상·본문 해시](../../../CTRLInstant/evidence/patch-targets.json). [0.1.0 대상과 소스 보존](../../../CTRLInstant/archive/0.1.0). 실제 진단 로그와 해결 여부는 사용자 실행 전.

## 이전 계약

# CTRL Instant 0.1.0 패치 계약

새 모드로 기존 패치 이력 없음. 변경 전 UI_INPUT, cheat CURRENT/PATCHES, COMPATIBILITY와 CTRL 정적 리뷰를 대조했다.

| 원본 타입 / 함수 | RVA | 패치 / 계약 |
|---|---|---|
| UISailInputAgent.OnFeatureInputActionStart | 0xD2E4B0 | Postfix. 정상 전달된 CheckTarget 시작에만, 항해·현재 UI·입력·앱 포커스와 다른 정지 여부 확인 후 자체 PauseGame 토큰 획득 |
| UISailInputAgent.OnFeatureInputActionPerform | 0xD2E8A0 | Prefix. 자체 처리한 액션의 늦은 Hold 성립만 차단. 다른 액션/미처리 원본 입력 유지 |
| UISailInputAgent.OnFeatureInputActionEnd | 0xD2EAA0 | void Finalizer. 원본 해제 후 자체 토큰 정리. 원본 예외 억제 없음 |
| UISailingCtrl.CloseHook | 0x912100 | Prefix. 같은 소유 컨트롤러 정리, 원본 유지 |
| UISailingCtrl.DisposeHook | 0x912E60 | Prefix. 같은 소유 컨트롤러 정리, 원본 유지 |
| OceanSceneSailingState.Exit | 0x770660 | Prefix. 항해 정지 소유권 정리, 원본 유지 |
| GameManager.ForceReset | 0x4D9000 | Postfix. 원본이 토큰 목록을 비운 후 자체 참조 폐기. 오래된 ID 재사용 없음 |

정확한 서명·RVA·본문 SHA256: [patch-targets.json](../../../CTRLInstant/evidence/patch-targets.json). 7개 모두 현 native-map에 동일 RVA의 다른 선언 없음. Harmony 임의 우선순위 없음.

OnUpdate는 이미 정상 시작된 액션의 유지·해제만 감시한다. 키 폴링으로 새 정지를 시작하지 않는다. 정지 토큰의 발급 목록 포인터와 ID를 함께 보관하여 다른 세션이나 다른 창의 토큰을 해제하지 않는다. 선택 UI도 자체 OnAction_CheckTarget_Long 호출로 열린 것의 토큰만 추적한다. 선단이 없으면 checkNPCShipStatus를 강제로 켜지 않는다.

공유 상태: GameManager.pauseList, 항해 UI 포커스/입력, 원본 선단 선택 상태와 물리/시간의 일시정지 읽기. 배속의 이동 힘, Unity timeScale, 저장 데이터, 전역 Hold 설정은 변경하지 않는다. 원본 CheckTarget 재지정 및 같은 액션에 묶인 패드 입력에도 적용한다.

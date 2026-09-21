# NavtoCity 0.1.0 패치 목록

소스: [EntryPoint.cs](../../../NavtoCity/src/EntryPoint.cs). 전체 서명: [interop 검증](../../../NavtoCity/evidence/interop-verification.txt).

모든 대상에 void Prefix / Postfix / Finalizer. 원본 실행을 차단하거나 인수/결과를 바꾸지 않는다. Finalizer는 원래 예외를 그대로 반환한다. 기록은 입항 관측 범위에서만 활성화한다.

| 타입.메서드 | RVA | 목적 |
|---|---|---|
| PortManager.EnterHarbour(int) | 0xD11240 | 바다에서 시작, 요청 결과 확인 |
| SceneManager.SwitchScene(BaseScene, ISceneLoading) | 0xC75670 | 실제 항구/관리자 관측, 다른 장면 전환 시 범위 종료 |
| SceneManager/InnerSwitchScene.MoveNext() | 0x53BDB0 | 비동기 전환 단계의 실행 시간과 관측 구간 |
| ResourcesManager.LoadSceneAsync(string, LoadSceneMode) | 0x147D480 | 요청 함수 시간/장면 인수; 완료 아님 |
| ResourcesManager.UnloadSceneWithout(string) | 0x147D6D0 | 이전 장면 정리 호출 |
| HarbourScene.ShowLoading() | 0x7D51A0 | 도시 전환 연출 준비 |
| HarbourScene.DisposeLoading() | 0x7D53A0 | 전환 연출 해제 호출; 완료 판정과 별도 |
| HarbourScene.OnEnter() | 0x7D5430 | 도시 초기화 |
| HarbourScene/LoadPreloadResHook.MoveNext() | 0x260B990 | 도시 사전 로딩 단계 실행 시간/관측 구간 |
| OceanScene.OnExit() | 0x51F3D0 | 바다 정리 |
| PlayerTeamSailingStatusManager.SailingLiquidation() | 0x11895A0 | 항해 정산 |
| BaseScene.ClearPreloadResources() | 0x7676D0 | 사전 로딩 자원 정리 |
| HarbourController.Start() | 0x7D2550 | 도시 오브젝트 초기화 |
| HarbourController.ActiveSelf(bool) | 0x7D2F60 | 도시 표시 관련 동기 실행 |
| PlayerDataManager.AutoSavePlayerData(Action) | 0x45A690 | 자동 저장 호출의 동기 실행 부분 |

중첩 iterator의 interop 이름은 `_InnerSwitchScene_d__28`, `_LoadPreloadResHook_d__13`이다. 메타데이터 인덱스에서 위 RVA의 다른 메서드 별칭은 확인되지 않았다. getter 2개는 본문 확인만 했고 패치하지 않는다.

공유 상태: 게임 SceneManager/현재 HarbourScene 읽기, 자체 정적 관측 창/큐만 쓰기. 입력·포커스·장면 수명주기·저장 콜백은 유지한다. 기존 모드의 같은 메서드 직접 등록은 검토한 목록에서 발견되지 않았으며 런타임 충돌 없음의 증명은 아니다.

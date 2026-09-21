# Cheats Character 1.0.0 — 훅 없음

Harmony 훅 0개. 게임 상태 쓰기(적용 버튼 1회당 최대 1회): HP `ModifyProperty.SetCurrentData` 0xD3E660 / `MaxData` 대입 + `LimitCurData` 0xD3E970, 공격력·능력치 `PointProperty.UpdateBaseData` 0x118BC30, 스킬 올림 `PlayerRoleData.InnerUpdateSkillLevel` 0x67ACC0(원본 `Messenger.Broadcast` 포함), 스킬 내림 `LevelProperty.UpdateBaseData`, 이후 `PlayerModuleDB.MarkDBDirty` 0x674A90. UI 갱신 호출: `UICharacterCtrl.InitSkillData`, `UICharacterView.RefreshSheetCharacter`·`RefreshTipsRoleInfo`, `UIModel.MarkDirty`(Tab Characters와 같은 순서). 읽기: `UIManager._alreadyOpenedUICtrls`의 `UICharacterCtrl` 뷰·모델, `PlayerHoldRoleDB.FindHoldRole`. Interface의 입력·세션 수명 공유. [근거](character-1.0.0.md).

# Cheats Exp 1.0.0 — 훅 없음

Harmony 훅 0개. 게임 상태 쓰기: `PlayerCurrencyDB.ModifyAmount(2,target)` RVA 0x452AC0, 적용 버튼 1회당 최대 1회. 읽기: `PlayerData.PlayerCurrency.FindCurrency(2)`, `UIManager._alreadyOpenedUICtrls`의 `UIDrunkeryCtrl`·`UIHeroLevelUpCtrl` 뷰 상태, SceneManager 장면 상태. Interface의 입력·세션 수명 공유. [근거](exp-1.0.0.md).

# Cheats Interface 1.3.0 — H 토글

IsOpen 기본 false, H로 펼침/접힘 전환. 도시/항해/세션 및 뷰 재생성에도 유지. 텍스트 입력/앱 비활성 중 토글 차단, X 뒤 H로 재개. 기존 4개 Interface 훅 유지, H 폴링 및 기존 입력 억제 경로 공유. 빌드·Host 92개 검사 통과, 설치·실전 미검증. [계약·근거](1.3.0.md).

# Cheats Interface 1.2.3 — 기본 접힘

최초/세션 초기화 시 제목줄만 표시. 수동 펼침과 장소 이동 시 상태 유지. 기존 4개 훅/입력/수명 계약 유지. 빌드 및 Host 80개 검사 통과, 설치·실전 미검증. [근거·배포](1.2.3.md).

# Cheats Interface 1.2.2 — 기본 펼침 복원

기본/세션 초기화 시 펼침, 장소 전환 자동 접기 제거. 수동 접기·프로세스 닫힘 및 기존 입력/4개 훅 계약 유지. 사용자 로그와 설치 목록에서 Battle DLL 누락 확인: 배포 ZIP에 Battle 1.0.0 동봉. Battle 로직/배율 유지 정책은 변경 없음. 빌드 및 관리 검사 129개 통과, 설치·실전 검증 전. [근거·배포](1.2.2.md). 아래는 과거 기록.

# Cheats Battle 1.0.0 — 신규 7개

OceanScene.ChangeInputState/OnExit, MeleeBattleController.StartEnterBattle/OnUpdate/ExitBattle/OnBattleEndedTipsClose, BoatEntityGun.DelayFire. 인라인된 ClientBattle.Tick/AttackInfo.Init에는 패치하지 않는다. 원본 실행 유지. 백병전 유닛의 네 능력치 및 자기 공격 건의 gunDamageFactor만 변경·소유 복구. 해상 전투 전체 종료/세션 리셋 시 X1, 백병전 종료나 임시 조회 실패로 선택 변경 금지. Interface의 입력/뷰/세션 수명 공유, 새 전역 입력 훅 없음. [전체 RVA·수명주기](battle-1.0.0.md), [대상](../../../Cheats/Battle/evidence/targets.json). 정적/관리 확인이며 실전 미검증.

# Cheats 1.2.1 — 공헌도 숨김·진입 시 접기

공헌도는 도시 밖 숨김. 최초/도시/항해 진입 시 제목줄만 남기는 최소화. 동일 장소에서는 사용자 펼침 유지. Interface 입력·장면 읽기·세션 수명 공유, 새 훅/게임 상태 쓰기 없음(4+1+2 유지). 빌드 및 248개 관리 검사 통과, 설치·실전 미검증. [근거·배포](1.2.1.md).

# Cheats 1.2.0 — 장소별 패널 숨김

Interface 1.2.0 / Speed 1.1.1 / Bargirls 1.1.1. 항해 조건 밖 속도 패널 숨김, 도시 밖 여급 패널 숨김. 도시 안 여급 기존 제한 유지. 숨긴 공간 제거·포커스 해제, Refresh/세션/기존 4+1+2 훅 유지. 빌드·375개 관리 검사·원본 기준 대조 통과, 설치 및 실전 검증 전. [변경·근거](1.2.0.md).

# Cheats Interface 1.1.3 — 비율 기반 초기 위치

WindowState.Place 최초 좌표를 화면 크기 비율로 계산. 기존 4개 Interface 훅 및 입력·공유 상태·수명주기 계약 유지. [근거](1.1.3.md).

# Cheats Interface 1.1.2 — 기본 위치 여백

WindowState.Place 초기 좌표만 변경. Interface 4개 훅 및 입력·공유 상태·수명주기 계약 유지. 새 네이티브 대상 없음. [근거](1.1.2.md).

# Cheats Bargirls 1.1.0 — 호감도 상승과 여급별 퀘스트 보호

`여급 호감도 [상승]` 버튼 하나로 다음 하트 단계까지 상승. 3단계·현재 여급 퀘스트 진행/보고 대기·원본 과제 상한에서는 차단, 다른 여급 퀘스트는 영향 없음. 여급 없음/술집 밖 취소선 유지. 과제 완료 수치 및 직접 호감도 setter 제거, 원본 UpdateFavorability 사용. 빌드·정책 135개·Host 61개·IL 감사 통과, 실전 미검증. [근거·배포](bargirls-1.1.0.md).

# Cheats Bargirls 1.0.0 — 여급 애정 단계

여급이 있는 술집/여급 화면에서만 1·2·3 버튼 활성화. 미지원 상태는 취소선, 하위 단계는 흐림 및 적용 차단. 애정 50/250/650과 필요한 여급 과제 제한을 상승시키며 보상은 지급하지 않는다. 새 훅 0개. 빌드·정책 88개·Host 61개·IL 감사 통과, 실전 미검증. [범위·근거·배포](bargirls-1.0.0.md).

# Cheats Money 1.0.0 — 소지금 패널 추가

Restitutor_Cheats_Money.dll을 공헌도 아래/배속 위에 추가했다. 목표값 0~2,147,483,647, 플레이어 준비/비로딩 동안 장소 제한 없음. 원본 ModifyAmount로 적용, 새 훅 없음. 빌드/관리 검사 24개/Host 검사 61개/IL 검사 및 설치 해시 대조 완료. 실전 검증 전. [동작·근거·배포](money-1.0.0.md).
# Cheats Interface 1.1.1 — 스크롤바 래퍼 수정

GetChildAt 반환값의 GGraph 직접 형변환 제거, 생성 시 보관한 참조 사용. 기존 4/1/2 훅·입력·수명주기 계약 유지. 사용자 1.1.0 오류 확인, 수정본 실전 검증 전. [원인·검증](1.1.1.md).

# Cheats 1.1.0 현재 패치

아래 4/1/2개 훅 유지. Host만 공통 창의 드래그/스크롤/접힘/프로세스 닫힘/포커스 해제를 담당한다. 숨김 상태와 Player Reset을 분리했고 기능 Refresh는 계속된다. 공통 제목줄은 모든 기능이 비활성이어도 입력 영역이다. 새 전역 입력 훅/우선순위 없음. 각 기능 DLL의 변경은 패널 표시 및 공통 스타일 API 사용에 한정한다. [상세](1.1.0.md).

# Cheats 1.0.0 당시 패치

| 소유 DLL | 대상 | 계약 |
|---|---|---|
| Interface | PlayerData.Deserialize | Prefix 세션/패널 초기화, Postfix 현재 Player 확보 |
| Interface | WorldPortHoldDB.InitHook | Prefix 세션 초기화 |
| Interface | PlayerDataManager.ProcessArchiveInitialize | Postfix 현재 Player 확보 |
| Interface | InputSystemManager.OnEventCaptureInput | 자체 활성 창 포커스/포인터/입력 직후만 차단 |
| Speed | BoatEntityOceanDriver.FixedUpdate | Prefix 호출 범위 이동 힘 배율, Finalizer 조건부 복원 |
| Contribution | BaseObjectData.GetPointProperty | 자체 적용 범위 동일 지휘관 133 조회 1회 중립화 |
| Contribution | BaseObjectData.GetProperty | 같은 범위 21 조회 1회 중립화 |

총 7개 고유 대상(4/1/2), 신규 대상·우선순위 없음. [서명/RVA/별칭 검사](../../../Cheats/evidence/patch-targets.json). 각 Melon의 Harmony 인스턴스로 등록/해제한다. 기능 해제 시 자기 패널/상태만 제거하고 공통 창을 재배치한다. 세션 초기화는 모든 등록 패널의 Reset을 호출한다. 뷰 재생성만으로 선택 배율을 초기화하지 않는다. 각 비활성 패널은 자기 포커스만 해제한다.

지도/정지 중 항해 배율 선택 허용, 실제 이동의 일시정지 차단 및 종료 X1 유지. 공헌도는 도시에서만, 지도/항로에서는 차단. [근거](1.0.0.md).

## 이전 계약

# Cheat 0.3.2 현재 패치

기존 7개 그대로. 배율 선택은 항해 상태만 검사하며 지도/일시정지와 분리. FixedUpdate Prefix는 일시정지 중 배율 쓰기 차단, 종료 X1 및 Finalizer 복원 유지. 도시 공헌도 경계 변경 없음. [근거](0.3.2.md).

## 이전 계약

# Cheat 0.3.1 당시 패치

등록 7개/원본 호출 계약 유지. 지도 차단은 IsOpen뿐 아니라 stage 부착 및 표시 계층을 읽는다. 신규 UI 훅/게임 상태 쓰기 없음. 항해 차단 사유를 자체 UI와 변화 로그로 표시. [근거](0.3.1.md).

## 이전 계약

# Cheat 0.3.0 당시 패치

기존 6개 + `BoatEntityOceanDriver.FixedUpdate()` Prefix/Finalizer = 총 7개 고유 대상. 신규 RVA `0x104AEA0` 전체 메타데이터 별칭 없음. Prefix는 항해 조건/현재 플레이어 함대/배 상태를 확인하고 이동용 `forwardPowerFactorByEscape`를 호출 범위에서만 X2~X5로 곱한다. Finalizer는 자체 적용값이 남은 경우 원래 값으로 복원하며 원본 예외를 억제하지 않는다. X1은 쓰기 없음. 항해 종료 시 선택값 X1, 저장 데이터에는 기록하지 않는다. 입력은 기존 치트창 차단기를 두 개 독립 활성 구역에 적용한다. [본문과 검증 한계](0.3.0.md), [현재 7개 목록](../../../Cheat/evidence/patch-targets.json).

## 과거 계약

# Cheat 0.2.0 당시 패치

6개: PlayerData.Deserialize, WorldPortHoldDB.InitHook, PlayerDataManager.ProcessArchiveInitialize, BaseObjectData.GetPointProperty, BaseObjectData.GetProperty, InputSystemManager.OnEventCaptureInput. 각 기존 계약 유지. UIMap/UIMapLine의 Show/Close/Dispose 등록은 모두 제거. UIManager 열린 목록은 읽기만 한다. [상세](0.2.0.md).

## 과거 계약 (현재 등록 목록이 아님)
# Cheat 0.1.1 패치 계약

0.1.1은 입력 제한만 `[0-9]`로 수정했다. 아래 12개 후킹 대상과 범위는 0.1.0과 동일하다. [근거·회귀 검사](0.1.1.md).

12개 고유 대상. [정확한 서명/RVA](../../../Cheat/evidence/patch-targets.json), [등록 및 범위](../../../Cheat/src/EntryPoint.cs).

| 대상 | 계약 |
|---|---|
| PlayerData.Deserialize | 이전 도시/입력/참조 정리, 완료 후 현재 플레이어 확보 |
| WorldPortHoldDB.InitHook | 자체 상태 정리; 원본 유지 |
| PlayerDataManager.ProcessArchiveInitialize | 완료 후 현재 PlayerData 확보 |
| UIMapCtrl.ShowHook / CloseHook / DisposeHook | 실제 표시된 컨트롤러 추적 및 종료 시 제거 |
| UIMapLineCtrl.ShowHook / CloseHook / DisposeHook | 항로 지도 컨트롤러 추적 및 종료 시 제거 |
| BaseObjectData.GetPointProperty(int) | 치트 적용 스레드·동일 CommanderData·133번 조회 1회에만 null 반환. 문화권 증가 보정 분기 생략 |
| BaseObjectData.GetProperty(int) | 같은 범위의 21번 조회 1회에만 자체 PointProperty(21,0) 반환. 공헌도 증감 배율 1 |
| InputSystemManager.OnEventCaptureInput | 치트창에 FairyGUI 포커스/포인터가 있거나 입력 직후 0.25초 동안 원본 게임 입력 차단 |

UpdateInfluence에는 훅을 추가하지 않는다. 적용 버튼 처리에서 UpdateInfluence(int,int)를 1회 호출한다. 공헌도 setter, InnerUpdateInfluence, 메모리 쓰기 없음. 두 보정 조회 토큰은 원본 수치 변경 및 이벤트 발행 전에 소모된다. 재진입 거부, using/finally로 예외 시 범위 해제. 원본 CommanderData/속성/저장 형식은 변경하지 않는다.

FairyGUI GRoot에 자체 창을 추가하고 일반 UI 위 sortingOrder=25000을 사용한다. Contribution의 확인창(30000)보다 아래다. 자체 입력에만 FairyGUI 전파를 중단하며 UIManager.CurrentFocusViewCtrl을 교체하지 않는다. 지도 선택과 체류 상태는 읽기만 한다. 원본 게임의 정상 UpdateInfluence 호출은 차단하지 않는다.

Contribution과 PlayerData/WorldPortHoldDB 초기화, 입력 캡처 및 공헌도 관측 흐름을 공유한다. Map 0.1.5/0.2.0 후보와 지도 수명주기 및 입력을 공유한다. 임의 Harmony 우선순위를 추가하지 않았다. 같은 함수 등록뿐 아니라 공유 UI/상태가 있으므로 실제 무충돌을 보장하지 않는다.


## 0.1.2 선택 판정
12개 등록과 수명주기는 동일. Resolve에서 HarbourIcons 전체 선택 플래그 합산을 제거하고 각 표시 지도의 GetCurrentSelectPort 결과만 수집한다. 다른 도시 둘 이상이면 기존 오류/적용 거부, 없으면 체류 도시 사용. [근거](0.1.2.md).


## 0.2.1 입력 보정
등록 6개 그대로. 자체 GTextInput.onChanged 콜백만 추가하며 입력 텍스트만 수정한다. Apply/UpdateInfluence 호출 없음. [상세](0.2.1.md).



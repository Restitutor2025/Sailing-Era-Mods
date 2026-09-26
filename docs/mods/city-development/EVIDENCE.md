# 도시 개발 Core0.14.0 원본 대조

기준 GameAssembly SHA256 `50d53d17829e3e77b9786ea42d998d1ad258f0653846524069f22e5e442effca`. 게임/Steam 실행 없음.

## 후크 메타데이터와 본문

| 타입 | 함수 | RVA | 동일 RVA 메타데이터 수 | 로컬 본문 대조 |
|---|---|---|---|---|
| PlayerDataManager | CreateNewArchive | 0x45A370 | 1 | 로컬 대조 자료(미게시) |
| PlayerData | GetInstance | 0x453A50 | 1 | 로컬 대조 자료(미게시) |
| PlayerData | Serialize | 0x456120 | 1 | 로컬 대조 자료(미게시) |
| PlayerData | Deserialize | 0x456A80 | 1 | 로컬 대조 자료(미게시) |
| WorldPortData | GetFacilityList | 0xB74270 | 1 | 로컬 대조 자료(미게시) |
| WorldPortHoldDB | AddPortOpenFacility | 0xB79DF0 | 1 | 로컬 대조 자료(미게시) |
| WorldPortHoldDB | AddSelfPortOpenFacility | 0xB79E50 | 1 | 로컬 대조 자료(미게시) |
| WorldPortHoldDB | AddAllOpenFacility | 0xB79F30 | 1 | 로컬 대조 자료(미게시) |
| PlayerTalkDB | SetEndTalkPart | 0x682BA0 | 1 | 로컬 대조 자료(미게시) |
| PlayerTalkDB | SetDoneTalkPart | 0x682AF0 | 1 | 로컬 대조 자료(미게시) |
| PlayerGameEventDB | SetGameEventDone | 0x4624F0 | 1 | 로컬 대조 자료(미게시) |
| PlayerGameEventDB | SetDoneGameEvent | 0x4625A0 | 1 | 로컬 대조 자료(미게시) |
| PlayerDataManager | ProcessArchiveInitialize | 0x45A490 | 1 | 로컬 대조 자료(미게시) |
| NpcManager | CreateNewNpcData | 0xE326D0 | 1 | 로컬 대조 자료(미게시) |
| NpcManager | UpdateStayPort | 0xE32920 | 1 | 로컬 대조 자료(미게시) |
| GotoSpecialPortAct | OnUpdate | 0xA17360 | 1 | 로컬 대조 자료(미게시) |
| SetPortByBirthplaceAct | OnUpdate | 0x8937E0 | 1 | 로컬 대조 자료(미게시) |
| SetPortByNearestPortAct | OnUpdate | 0x893A00 | 1 | 로컬 대조 자료(미게시) |
| SetPortBySpecialCargoAct | OnUpdate | 0x893EA0 | 1 | 로컬 대조 자료(미게시) |
| SetPortFromPortCollection | OnUpdate | 0x8940E0 | 1 | 로컬 대조 자료(미게시) |
| SetPortFromPortCollectionSequenceLoop | OnUpdate | 0x894460 | 1 | 로컬 대조 자료(미게시) |
| SetPortListByParameterAct | OnUpdate | 0x8945E0 | 1 | 로컬 대조 자료(미게시) |
| SetPortListFromAreaAct | OnUpdate | 0x8947D0 | 1 | 로컬 대조 자료(미게시) |
| SetPortsByDangerousAreaAct | OnUpdate | 0x8949C0 | 1 | 로컬 대조 자료(미게시) |

메타데이터의 단일 주소는 모든 호출이 후크를 지난다는 증명이 아니다. 인라인 경로를 고려해 시설 조회에서 다시 정책을 적용하고, 이벤트 완료·직렬화 직전에도 진행을 평가한다.

## 확인된 본문과 설계 근거

- CreateNewArchive는 PlayerData.GetInstance를 실제 호출한 뒤 Data에 대입하고 초기화 구독자를 호출한다. 생성 구간 플래그와 GetInstance Postfix로 새 세이브 계획을 분리한다.
- PlayerData.Serialize는 VersionSet 문자열을 FBPlayerDataT.VersionSet 벡터에 복사한다. 확장 문구는 이 직렬화 벡터에만 추가하고 Deserialize Prefix에서 제거한다. Finalizer에서 벡터 참조를 복원한다. 게임의 살아 있는 VersionSet에는 확장을 넣지 않는다.
- ProcessVersion은 등록된 버전 처리기 목록과 세트 포함 여부를 이용한다. 확장을 원본 Deserialize 이전에 추출하므로 이 마이그레이션의 입력으로 전달하지 않는다. 원본 마이그레이션 키는 그대로 보존한다.
- GetFacilityList의 특정 도시 전용 분기를 설정된 도시에서 공통 정책으로 대체한다. 일반/전용 시설 목록을 같은 누적 개방 목록에 맞추며 시장 재고·인구·기술·허가 상태는 초기화하지 않는다.
- NpcManager.CreateNewNpcData 및 UpdateStayPort는 BornPorts에서 무작위 선택한다. 후보 배열을 호출 기간에만 좁히고 Finalizer로 원본 참조를 복원한다. 후보가 전혀 없으면 새 NPC 생성은 보류한다.
- CreateNpcById는 CreateNewNpcData의 false 반환 시 후속 NPC 조회/생성을 건너뛴다. 해금 시 보류된 NPC 중 원본 IsNeedCreateNpc 조건을 만족하는 것만 다시 생성한다.
- GotoSpecialPortAct.OnUpdate는 MoveToPort false를 이동 중/실패 상태로 해석하는 분기가 있다. 정책상 닫힌 목적지는 행동 작업에서 Failure로 반환해 진행 중인 함대를 도착 직전에 취소하지 않는다.
- 후보 목록 생성 작업은 과거 원본 배열 사본을 보관하여 해금 시 후보를 복구한다. 다른 코드가 배열 참조를 교체했다면 덮어쓰지 않는다. 해금만으로 특정 함대의 정기 방문은 보장하지 않는다.

## 검증 범위

관리 코드 저장 어댑터는 네이티브 객체 대역으로 직렬화 경계/원본 버전키 보존/알 수 없는 확장 보존/구세이브 제외/새게임 분리를 검사한다. IL2CPP 원본 직렬화 왕복과 실제 게임 UI·NPC 이동은 사용자 실전 확인 전이다. 외부 모드가 직접 목적지·체류 필드를 바꾸는 경로까지 통제하지 않는다.

추가 본문 대조: SetEndTalkPart는 SetDoneTalkPart 호출 없이 DoneTalkParts에 직접 추가하므로 별도 Postfix를 연결한다. UIHarborCtrl.GetAreaFacilityList는 GetFacilityList를 실제 두 번 호출한다(958820). RefreshHarborData(956240)→GetCurrentFacilityList(958660), UIHarborView.Refresh(9659A0)의 목록 갱신을 확인하고 현재 항구 단계 완료 시 대화 종료/입력 금지 해제 이후 1회 갱신한다. 직렬화 직전 평가는 NPC 생성·UI 통지를 보류한다.

NpcManager.Instance_OnInitPlayerData(E31000)는 현재 플레이어의 WorldNpc/WorldShip을 연결하고 InitGameWorldNpc(E31950)를 직접 호출한다. 저장을 다시 읽은 초기화에서도 생성 누락 NPC를 원본 필요 조건으로 재평가하는 경로이며, 개발 전 보류 큐는 여기서 다시 구성된다.

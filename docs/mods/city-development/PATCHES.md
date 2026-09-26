# 도시 개발 Core0.14.0 패치 계약

- 기준: 설치 GameAssembly SHA25650d53d17829e3e77b9786ea42d998d1ad258f0653846524069f22e5e442effca. [함수별 본문과 RVA](EVIDENCE.md).
- 생성/저장: CreateNewArchive Prefix/Finalizer, PlayerData.GetInstance Postfix, Serialize Prefix/Postfix, Deserialize Prefix/Finalizer. 새 게임에서 계획 캡처; 직렬화된 FBPlayerDataT.VersionSet에만 전용 확장 추가, Deserialize 동안 분리/복구. live VersionSet·원본 버전키 보존. 구세이브 미적용, 알 수 없는 확장 원문 보존.
- 시설: WorldPortData.GetFacilityList Prefix, WorldPortHoldDB.AddPortOpenFacility/AddSelfPortOpenFacility Prefix, AddAllOpenFacility Postfix. 설정 도시의 일반/전용 목록을 같은 누적 개방 목록으로 관리, 변경 시 dirty. 인구/기술/무역/재고/허가 재설정 없음.
- 조건: PlayerTalkDB.SetDoneTalkPart/SetEndTalkPart, PlayerGameEventDB.SetGameEventDone/SetDoneGameEvent Postfix. ProcessArchiveInitialize Prefix. getter/직렬화 전 보완 평가. 완료 단계는 감소하지 않는다. 최대32단계, 앞 단계부터 확인.
- NPC: NpcManager.CreateNewNpcData/UpdateStayPort Prefix/Finalizer(출생 후보 호출 중 필터/복구), GotoSpecialPortAct.OnUpdate Prefix, 목적지 선택5종/후보 목록3종 OnUpdate Postfix. 생성 보류→해금 후 원본 생성 조건 재확인. 후보 배열 복구는 동일 참조일 때만. 도착 함대 취소/순간이동 없음.
- 입력/수명: 직접 입력 소비 없음. 현재 항구 단계 완료에 한해 UI 갱신 예약, 기존 EntryPoint.OnUpdate에서 대화 종료·화면 준비 후1회 native RefreshHarborData/Refresh. 유휴 시 bool 검사만. 세이브 상태는 현재 PlayerData 포인터 소유권과 연결; 다른 슬롯/새 게임 생성은 교체. NPC 임시 후보/보류 목록도 소유권 변경 때 초기화.
- 접점: 네이티브 시설 개방 목록/dirty 통지, FB 문자열 벡터, Npc.BornPorts 임시 배열, 행동 정의 배열, 항구 UI. 기존 NavDiag 읽기 후크는 변경하지 않는다. 다른 저장 확장·시설·AI 수정 모드와 실전 병용 미확인.

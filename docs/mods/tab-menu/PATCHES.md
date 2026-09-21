# TabMenu 0.1.0 패치 목록

소스: [EntryPoint.cs](../../../TabMenu/src/EntryPoint.cs). [전체 서명](../../../TabMenu/evidence/targets.json).

일반 대상은 void Prefix/Postfix 및 원래 예외를 그대로 반환하는 Finalizer. renderer 5개는 void Prefix에서 횟수만 합산한다. 원본 차단/인수/반환값 변경 없음. 설치 interop 선언은 모두 유일하며 native-map 안에서 같은 RVA의 다른 선언 및 중복 등록 없음. 실행상 병용 무결함을 의미하지 않는다.

| 대상 | RVA | 기록 |
|---|---|---|
| UIMenuHelper.Start | 0xBD95A0 | 관측 시작/요청 탭/반환 bool/시간; static 전용 핸들러 |
| UIMenuCtrl.ShowChild | 0xBD6F00 | 관측 시작 또는 중첩 포함/실제 선택 탭/시간 |
| UIMenuCtrl.PrepareShowChild | 0xBD5DD0 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UIMenuCtrl.CloseHook | 0xBD7800 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UIBagCtrl.ShowHook | 0x6A30D0 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UIBagCtrl.SyncPlayerDataToModel | 0x6A33E0 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UIBagCtrl.RefreshDataModel | 0x6A35B0 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UIBagView.OnInit | 0x6A5AA0 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UIBagView.Refresh | 0x6A6120 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UIBagView.RefreshBagData | 0x6A66C0 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterCtrl.ShowSheet | 0x947500 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterCtrl.ShowHook | 0x947910 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterCtrl.SyncPlayerDataToModel | 0x9479B0 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterCtrl.InitRoleData | 0x948780 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterCtrl.InitBookData | 0x948010 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterCtrl.InitEquipData | 0x948C60 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterView.OnInit | 0x1020D40 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterView.Refresh | 0x10236A0 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterView.RefreshSheetCharacter | 0x10255E0 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterView.RefreshSheetSkill | 0x1027200 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UICharacterView.RefreshSheetEquip | 0x1028EE0 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| PlayerEquipDB.SyncEquipItemData | 0x461080 | 동기 호출 시간/전후 스냅샷(지원 인스턴스) |
| UIBagView.BagSlotRenderer | 0x6A6EE0 | 횟수만 합산 |
| UICharacterView.RenderListRole | 0x1023040 | 횟수만 합산 |
| UICharacterView.RenderListBook | 0x1028AB0 | 횟수만 합산 |
| UICharacterView.RenderListEquip | 0x102A8C0 | 횟수만 합산 |
| UICharacterView.RenderListEquipWear | 0x102B000 | 횟수만 합산 |

## 읽기 전용 조회와 공유 경계

기존 컨트롤러 Model backing field, View UIContent backing field, 모델 컬렉션 Count/선택 인덱스, 장비 DB ListEquip backing field를 읽는다. GList._virtual/numItems/numChildren은 일부 가방/캐릭터 목록만 조회한다. 공통 GList나 EffectCalculator에는 패치하지 않는다. IL2CPP GC.CollectionCount(0)/GetTotalMemory(false)는 시작/최대 1Hz/종료 때 조회만 수행한다.

메뉴 입력/포커스/UI 수명주기는 원본과 공유하지만 수정하지 않는다. 원래 닫기/저장/캐시/GC/완료 콜백 유지. NavtoCity와 GC 통계를 함께 조회할 수 있어 조회 부하는 합산된다. 소스 검색한 intro/textspeed/map/instant-entrance/nav-to-city에서 같은 대상 직접 등록은 발견되지 않았다. 외부 모드 전체 및 실행 패치 순서는 미확인.

상세 호출은 창당 2,000개까지이며 이후 총계 유지, renderer는 창별 합계. 게임 스레드 외 호출은 원본 유지하되 기록에서 제외. 초기화 실패 시 자기 훅 해제, 실행 중 진단 오류 시 기록 비활성화.

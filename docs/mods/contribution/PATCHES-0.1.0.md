# Contribution 0.1.0 패치 계약

23개 고유 대상. 정확한 서명·RVA는 [기계 판독 목록](../../../Contribution/evidence/patch-targets.json), 등록은 [EntryPoint.cs](../../../Contribution/src/EntryPoint.cs). 현재 대상 RVA 별칭 중복 없음은 정적 확인이며 런타임 후킹 성공을 뜻하지 않는다.

| 대상 | 변경 계약 |
|---|---|
| WorldPortHoldDB.UpdateInfluence 두 overload | 원본 전후 실제 수치 관측. 기준 통과 및 실제 허가 상태 변화만 큐에 기록. 원본 수치·반환값 유지 |
| WorldPortHoldDB.InitHook | 자체 상태 초기화 |
| PlayerData.Deserialize | 자체 상태 초기화 후 부가 저장 복원 |
| PlayerData.Serialize | 원본 직렬화 후 부가 저장 기록 |
| PlayerDataManager.ProcessArchiveInitialize | 현재 PlayerData 참조 획득 |
| UIGovHouseEntryView.RefreshMenuBtn | 메뉴 구성 호출 범위 기록·예외 시 복원 |
| FunctionOpenDB.GetFunctionData(int) | 위 범위에서 허가증 항목 33만 null 처리 |
| UIGovHouseEntryCtrl.ShowView | 허가증 탭 진입 차단 |
| GameEffectManager.InitCommerceEffect, PortScheduleManager.CommerceMonthCheck | 건물 효과 생성 범위 기록·예외 시 복원 |
| GameEffectManager.AddGameEffectInstant | 위 범위 효과 GUID 추적. 자체 교역 허가 부여 범위에서 대상 도시 보정 |
| CommerceDB.SetFleetTime | 폐쇄 대기 상회 월말 정산 제외. 구매 허가가 잠긴 도착 선단 취소. 임시 컬렉션은 finalizer 복원 |
| UIMarketCtrl.SetMarketGoods, Exchange | 취소된 별도 교역 허가 상품의 향후 직접 구매 차단·열린 화면 갱신 |
| UICommerceCtrl.ShowHook, UICommerceEntraceCtrl.ShowHook | 살아 있는 화면 추적; 상회 삭제 전에 원본 닫기 수행 |
| UIPopupsView.RefreshCommerceTrade | 원본 월간 보고서에 중단 안내 영역 추가. 재갱신 시 목록 위치·높이 복원 |
| GoodsData.CheckCondition | 상회 폐쇄 전 판매 조건이 충족된 상품의 검사 범위 기록 |
| CommerceBuildingConditionTask.OnCheck, OpenCommerceConditionTask.OnCheck | 위 상품 검사에 한정한 상회 조건 범위 기록 |
| CommerceDB.FindCommerce | 두 범위가 모두 일치할 때만 과거 건물 정보를 가진 임시 상회 반환. 실제 상회 DB에는 재등록하지 않음 |
| InputSystemManager.OnEventCaptureInput | 자체 확인창 및 닫기 직후 입력 전파 차단 |

## 공유 상태·수명주기

WorldPort 허가 목록, CommerceDB 및 선단/투자 잔액, GameEffectManager 효과, 시장 목록, FairyGUI GRoot·월간 보고서 목록, 입력 캡처를 사용한다. 공헌도 setter/UpdateInfluence 호출은 없다. 부가 저장은 원본 PlayerDataT를 다시 pack한 SHA256 키로 대응시키며 원본 저장 형식은 변경하지 않는다.

도시 판정은 IsInHarborScene·IsSceneEntered·!IsInLoadingOrStarting·IsStayInPort의 결합이다. 해상 큐는 도시 진입 후 적용한다. 자체 OnUpdate는 처리 후 HUD를 0.1초 간격 갱신한다. 초기화·로드·모드 종료 시 자체 창과 참조를 정리한다.

지도 모드와 InputSystemManager.OnEventCaptureInput을 공유한다. 임의 Harmony 우선순위는 추가하지 않았다. 네이티브 SetFleetTime에는 기존 비용/보고서 trampoline이 있어 해당 설치 본문을 기준으로 분석했다. 실제 병용 및 UI 포커스는 미검증이다.

# Contribution 0.5.4 — 도시 메인 혜택 HUD

도시 메인 UI 상태·포커스·실제 표시를 읽어 혜택 HUD만 제한. 시설/다른 메뉴에서 숨김, 복귀 시 표시. 14개 훅 유지, 상품 인식·해금·알림·저장 변경 없음. 빌드/관리 검사 통과, 실전 미검증. [근거](0.5.4.md).

# 0.5.3 현재 계약

14개 훅 유지. _ClipBox HasProperty 오판 제거, 자체 이미지 material 키워드를 설치 셰이더 변형에 맞추며 자체 NGraphics custom clipping flag2 설정. 전역/게임 UI는 수정하지 않는다. [근거](0.5.3.md).

# 0.5.2 당시 계약

14개 훅 유지. 자체 GImage에 전용 Material 지정, 현재 imageShader 지원/clipping 확인. texture/material 자동 정리 방지 및 Reset 시 UI→material→texture 순 정리. 전역 shader/material 설정 변경 없음. 실패한 UI 생성은 Clear로 정리한다. 입력/게임 데이터 정책 유지. [근거](0.5.2.md).

# 0.5.1 당시 계약

14개 훅 유지. 자체 확인 버튼의 진입/이탈/터치 시 표시 반응, 실제 click 때 CommonSoundUtils.SpecialClick 호출 추가. 음향 예외와 확인 처리는 격리한다. 전역 음량/포커스 변경 없음, 기존 모달 입력 해제 정책 유지. [근거](0.5.1.md).

# 0.5.0 당시 계약

기존 14개 네이티브 훅 및 저장 v3 유지. 변경은 자체 Overlay·이미지 캐시·알림 표시 순서다. 도시 HUD는 비입력, 알림은 기존 sortingOrder 30000·입력 차단·확인 후 0.2초 차단 유지. GRoot 포커스를 변경하지 않는다. 화면 재구성 때 컴포넌트/콜백을 정리하며 캐시 texture는 모드 해제 때 정리한다. 게임 데이터·해금/인식 순서를 수정하지 않는다. [0.5.0 근거/한계](0.5.0.md).

# 0.3.1 당시 계약

WorldPortHoldDB.AddNaturalResources(int,int), RVA 0xB7A210의 Postfix 추가. 총 14개. 원본 성공 반환/현재 플레이어 DB/자원 실제 등록을 확인해 공개 예약만 추가한다. 기존 원본 효과·자원 기록·반환을 변경하지 않는다. 인식/알림은 기존 도시 준비 후 흐름을 사용한다. 과거 자원 보충/순회 없음. 기존 100 이력 및 저장 v3 유지. [근거](0.3.1.md), [현행 대상](../../../Contribution/evidence/patch-targets.json), [0.3.0 대상 보존](../../../Contribution/releases/0.3.0/patch-targets.json).

# 0.3.0 당시 계약

기존 12개 대상 + `PlayerPortDB.AddEnterPort(int)`(RVA 0x676880), 총 13개. 추가 훅은 원본 호출 전 미방문 여부를 읽고 원본 호출 후 방문 기록 추가를 확인해 공개만 예약한다. 원본 인수/반환/예외/방문 기록을 바꾸지 않는다. FinishEnterPort는 실제 입항 경로가 인라인하여 사용하지 않는다.

UpdateInfluence 관측은 기존 혜택 예약에 최초 100 도달 이력을 더한다. OnUpdate 도시 준비 조건 이후 허가를 먼저 적용하고 현재 도시의 원본 KnowledgeManager 공개 함수를 호출한다. KnowledgeDB 상품-도시 기록과 신규 표시/dirty 이벤트를 공유한다. 새 직접 UI 패치·입력 훅·공헌도 쓰기 없음. 부가 저장 v3는 인식 예약/보류 알림/100 이력을 보존한다. [0.3.0 근거·한계](0.3.0.md), [현재 13개 기계 목록](../../../Contribution/evidence/patch-targets.json), [이전 12개 보존본](../../../Contribution/releases/0.2.2/patch-targets.json).

아래는 이전 계약 기록이다.

# 0.2.2 당시 계약 보완

등록 훅은 아래 12개와 동일하다. UpdateInfluence는 실제 상승 시 현재 요구치를 충족한 미해금 항목도 보충한다. 시장 추적은 기존 SetMarketGoods 훅만 사용한다. View/_state/화면 컴포넌트/Model이 준비된 열린 시장만 갱신하며 실패한 캐시를 제거한다. UI 갱신 예외는 해금 실패 차단과 분리한다. 실제 해금 실패의 차단은 다음 상승에서 해제한다. 입력과 저장 v2 수명주기 유지. [0.2.2 근거](0.2.2.md).

# Contribution 0.2.0 패치 계약

이전 23개 패치는 [0.1.0 기록](PATCHES-0.1.0.md)에 보존했다. 현재 12개 대상의 서명/RVA는 [기계 목록](../../../Contribution/evidence/patch-targets.json)을 기준으로 한다.

| 대상 | 처리 및 공유 상태 |
|---|---|
| WorldPortHoldDB.UpdateInfluence 2개 오버로드 | 원본 유지, 전후 값 읽기. 상승 기준 통과만 예약. 하락/동일 값 무시 |
| WorldPortHoldDB.InitHook | 자체 상태 초기화 |
| PlayerData.Deserialize | 자체 상태 초기화 후 부가 저장의 획득 예약 로드 |
| PlayerData.Serialize | 동일 플레이어의 부가 저장 v2 기록 |
| PlayerDataManager.ProcessArchiveInitialize | 현재 플레이어 참조 |
| UIGovHouseEntryView.RefreshMenuBtn | 허가 메뉴 조회 범위, finalizer로 복원 |
| FunctionOpenDB.GetFunctionData(int) | 위 메뉴 범위에서 허가 항목 숨김 |
| UIGovHouseEntryCtrl.ShowView | 수동 허가 탭 진입 차단 |
| GameEffectManager.AddGameEffectInstant | 자동 허가 추가 중 LicenceIncreaseCargoProduction 대상 도시 인수 보정, finally로 범위 해제 |
| UIMarketCtrl.SetMarketGoods | 열린 시장 참조 추적만 수행, 필터/구매 차단 없음 |
| InputSystemManager.OnEventCaptureInput | 자체 확인창 표시 중 입력 차단 |

도시 장면 진입 완료 후 대기한 획득을 AddGovHouseLicenceData로 적용한다. 공헌도를 쓰지 않는다. 시장 갱신·FairyGUI HUD와 확인창·저장 수명주기는 여전히 공유한다. 실제 병용 성공을 정적 검사로 보장하지 않는다.

## 0.2.1 갱신 준비 조건
등록 12개는 동일하다. OnUpdate에서 manager의 현재 Data와 보관 Player의 동일성 및 지휘관/항구/도시 DB 유효성을 확인한 뒤 갱신한다. 미준비 시 자체 HUD만 숨기고 예약을 유지한다. [근거](0.2.1.md).


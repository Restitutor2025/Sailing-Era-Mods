# Core0.7.4 — 셀레네 시험 시작 대화 (실전 미확인)

신규게임 첫 항구에서 준비후 원본 StoryManager.ShowTalkPart를1회 호출. PlayerTalkDB·일시정지·대화/항구 UI 수명은 원본 재생/종료에 맡김. TextSpeed/항구UI 모드와 같은 흐름이므로 병용 실전 확인 필요. InitTalkPart/Talk/TalkTextLib4언어에 기존 HeroRows 삽입 Postfix6개 추가, 고유번호15607401/156074001만 등록. GameEvent11001/그 조건·보상·대화는 보존. [계약](city-editor-core/0.7.4.md).

# Core 0.7.3 — 선택 입력/출항 관측 (실전 미확인)

UICreateRoleCtrl.OnAction_A/좌우 방향 Prefix는 자체 추가 주인공 상세/취소 해제 대기 동안만 원본 차단. 기본5인 선택 입력은 유지. 같은 컨트롤러 입력을 바꾸는 모드와 순서 접점. UIHarborView.CheckQuickEnterOcean/EnterOcean/EnterOceanResult/ShowHook/SetLimit/SetUnLimit 및 UIHarborCtrl.OnAction_X/Y 전후, IsTaskBan/HasPortFacilityEvent 반환은 읽기 전용. Map의 항로 출항 콜백, 항구/출항 UI 모드와 같은 호출 흐름에 있으나 조건·반환·콜백을 바꾸지 않는다. 병용 충돌 없음으로 단정하지 않는다. MainHero156의 시작 기본 메뉴1~10은 새 게임에서 기존 초기화가 적용. [근거](city-editor-core/0.7.3.md).

# Devil Fruits 0.1.6 — 2026-09-22 (빌드·실전 미확인)

btnReturn onTouchBegin/End 리스너 제거(Tab Characters 언어 캐처와의 같은 대상 공유 해소). 새 공유 상태: Stat Rank 등급 글자 부모에 자식 `RestitutorDevilFruitIcon`(touchable=false) 추가, 인물 창이 열린 동안 매 프레임 커서 위치·touchTarget 읽기(Stat Rank 호버와 같은 방식). 패널은 GRoot 자식 32100, 전체 화면 차단막 없음. 훅 변화 없음. [근거](devil-fruits/0.1.6.md)

# Item Rebuild 0.1.21 — 2026-09-21 (빌드·검사 통과, 실전 미확인)

0.1.20 롤백: KnowledgeDB·PlayerLaneDB Deserialize 훅, UIOpenLineCtrl.ShowOpenLine 호출, KnowledgeDB 쓰기, 항로도 자동 사용(PlayerLaneDB.UnlockPrefabLane·가방 기록 삭제) 모두 제거. 남은 항로도 접점은 상점 목록 숨김·구매 선택 해제·PlayerLaneDB 해금 여부 읽기뿐. 0.1.20 사용 중 “상점 위 해금 창을 닫은 뒤 상점 화면·입력 미복구” 보고가 있었다(원인 미확정) — 상점 Exchange 중 다른 UICtrl을 여는 모드는 같은 문제를 일으킬 수 있다 [추론]. [계약](item-rebuild/PATCHES.md) · [근거](item-rebuild/0.1.21.md)

# Item Rebuild 0.1.20 — 2026-09-21 (빌드·검사 통과, 실전 미확인)

새 훅 2개: KnowledgeDB.Deserialize·PlayerLaneDB.Deserialize Postfix(기록만). 구매 항로도마다 원본 UIOpenLineCtrl.ShowOpenLine을 호출(상점 위에 원본 해금 창, 차례 표시)하고 KnowledgeDB.AddRouteMapData로 지식 기록 — 세이브 dirty. 로드 때 130001~130056 해금 항로 지식 보충(1회). UIOpenLineCtrl 모델·UIMenuHelper 메뉴 금지·KnowledgeDB 항로도를 바꾸는 모드와 접점. Contribution(교역품 인식은 KnowledgeManager 사용)과 같은 지식 계열이지만 항로도 사전은 별개 [본문 미대조]. 병용 실전 미확인. [계약](item-rebuild/PATCHES.md) · [근거](item-rebuild/0.1.20.md)

# Devil Fruits 0.1.1 / Stat Rank 0.1.5 — 2026-09-21 (빌드·실전 미확인)

Devil Fruits 0.1.1: 훅 변화 없음. 등급 글자 클릭 시 btnReturn 캡처 범위가 "열매 보유 시"에서 "항해사 등급 글자 전부"로 넓어짐(Stat Rank 글자 영역 안만). 패널 GRoot 자식 32100, 패널 밖 전체 화면 투명 캐처(열린 동안 뒤 화면 클릭 차단, 누르면 닫힘). Stat Rank 0.1.5: 팁 alpha 0.7→0.8만 변경. [근거](devil-fruits/0.1.1.md)

# Text Speed 0.1.4 — 2026-09-21 (완료, 사용자 게임 내 적용 확인)

새 훅 UIDialogView.EndTyping Prefix/Postfix, UIDialogCtrl.OnAction_ContinueTalk Prefix(관측·차단 없음). "빠르게"면 오토가 꺼져 있어도 3초 뒤 스스로 OnAction_ContinueTalk 호출(새 자동 진행). 공유 상태: UIDialogModel.clickWait(원본 자동 진행 트리거의 실행 조건, 이 모드가 false로 바꿈), _autoTalk·IsTyping·TalkTid 읽기, UIDialogCtrl.autoAndSpeed 읽기, OnAction_ContinueTalk 호출. 입력: CTRL(빨리 넘기기)은 원본 그대로, 대기 중 CTRL 감지 시 즉시 진행 — CTRL Instant(항해 중 CTRL)와 같은 키지만 대화 UI 안에서만 동작. 대화 자동 진행·EndTyping·clickWait를 바꾸는 모드와 접점. 병용 실전 미확인. [근거](textspeed/0.1.4.md)

# Item Rebuild 0.1.19 — 2026-09-21 (빌드·실전 미확인)

- 새 공유 접점: `UIPropStoreCtrl.SetStoreGoods` Postfix(상점 4종 목록에서 보유 선실 아이템·해금 항로도 Slot 제거). 상점 목록을 다시 만드는 다른 모드와 순서 확인 필요.
- `Exchange` 뒤 항로도 130001~130056 자동 사용: `PlayerLaneDB.UnlockPrefabLane`(세이브), `PlayerBagDB.RemoveItemByGuid`. 항로 해금·항로 지도 UI(Map 0.2.1의 Tab 지도 통합)와 병용 미확인.
- 선실 설계도 11000~11010·11036·11037은 가방 UI에서 숨김(행 유지). Tab Characters·치트 등 가방 목록을 읽는 모드는 기록을 그대로 본다.

# Item Rebuild 0.1.18 — 2026-09-21 (빌드·실전 미확인)

훅 47개 유지. 언어 서적 30종(71101~71130)이 실제 병합 대상이 되어 PlayerBagDB/ItemBagData의 저장 행·수량을 수정한다(되돌릴 수 없음). 원본 언어 학습(OnClickBtnBook → InnerAddLanguage → InnerRemoveItemByGuid)은 기존 전처리로 한 권 차감. Tab Characters 언어 패널은 BookGuid+ItemId로 Number를 읽어 xN 표시 — 추가 차감 없음. 권별 GUID를 보관하는 외부 모드/열린 UI와 접점. 병용 실전 미확인. [계약](item-rebuild/PATCHES.md) · [근거](item-rebuild/0.1.18.md)

# Stat Rank 0.1.4 — 2026-09-21 (실전 확인)

훅 2개 유지: UICharacterView.RefreshTipsRoleInfo Postfix, HideHook Postfix. 0.1.2부터 등급 글자에 onRollOver/onRollOut 리스너를 붙이지 않고 OnUpdate 커서 좌표 폴링(창 열림 동안만)으로 표를 띄운다. 조건: 맨 위 히트 대상이 UISheetCharacter.btnReturn 또는 글자 자신 — Tab Characters 장비·학습 패널(32000) 등이 덮으면 표시 안 함. btnReturn은 읽기만 하고 리스너를 붙이지 않는다. 글자 이름 `RestitutorStatRank`, 팁 sortingOrder 31950 변경 없음 — Devil Fruits 0.1.0의 이름 의존·btnReturn 터치 리스너(글자 안만)와 접점 — 2026-09-21 병용 실전 확인(열매 사용 후 새 등급 표시). 표 alpha 0.7. [근거](stat-rank/0.1.4.md)

# Devil Fruits 0.1.0 — 2026-09-21 (완료; Stat Rank 0.1.4·Items 0.1.0 병용 실전 부분 확인)

새 훅 11개: TemplateManager.InitTextLib(4개 언어)·InitItemType·InitItem Postfix(행 추가만), PlayerHoldRoleDB.Deserialize·InitHook Postfix, UIHeroLevelUpCtrl.OnClickBtnLevelUp Prefix(차단 안 함), UICharacterView.RefreshTipsRoleInfo Prefix(차단 안 함)·HideHook Postfix. 공유 상태: 공용 Hero 템플릿 growth 5필드(쓰기), PlayerRoleData.ListSkillBooks(열매 tid 추가, 세이브 영구), PlayerHoldRoleDB/PlayerBagDB dirty, TemplateManager `_item`/`_itemType`/`_textLib*` 사전(새 키만). 입력: UISheetCharacter.btnReturn onTouchBegin/End 리스너 — Tab Characters 0.6.8 언어 캐처와 같은 대상, 영역 겹침 없음(Stat Rank 등급 글자 안만). Stat Rank 글자(`RestitutorStatRank`)를 이름으로 찾아 의존. Stat Rank가 같은 RefreshTipsRoleInfo Postfix로 등급을 읽으므로 Prefix에서 먼저 재적용. CityEditor Core·Character Editor가 Hero growth나 같은 tid를 바꾸면 충돌. 확인창 sortingOrder 32100(Tab Characters 패널 32000·Stat Rank 팁 31950 위). Stat Rank 0.1.4 병용은 2026-09-21 실전 확인(열매 사용→새 등급 표시), CityEditor Core·Character Editor 병용은 미확인. [근거](devil-fruits/0.1.0.md)

# Cheats Exp 1.0.0 — 2026-09-20

훅 없음. 공유 상태: 통화 ID 2(경험치) Amount와 PlayerCurrencyDB dirty 표시(`ModifyAmount`), `UIManager._alreadyOpenedUICtrls` 읽기(UIDrunkeryCtrl·UIHeroLevelUpCtrl), Interface 창·입력 억제. 같은 통화를 읽는 Trade Exp(미설치, 정산 화면 표시 합계)·Tab Characters 레벨업 보상 흐름·Skill 치트(레벨업 창)와 값 접점. 레벨업 창이 열린 동안은 패널을 숨겨 창 안 모델과의 동시 변경을 피한다. 병용 실전 미확인. [근거](cheat/exp-1.0.0.md)

# Tab Characters 0.6.0 — 인물 창 장비 패널 — 2026-09-20

새 훅: UICharacterView.RefreshTipsRoleInfo(Postfix). 공유 상태: 장비 시트(UISheetEquip) 위젯 12개를 패널이 열린 동안 빌려 옮김(닫을 때 복원), 장비 목록·필터 목록 itemRenderer와 행 콜백, UICharacterModel의 장비 선택·필터·포커스 필드와 `_sheetType`(원본 장비 함수 1회 호출 동안만 2), ListPropEquip, content.listSkill 참조(미리보기 호출 동안만 대역 목록), 인물 시트 스킬 행의 추가 레벨 문구·색. 입력: 패널 열림 중 Action_A/B/X/Y 소비, 물리 우클릭·Esc·Space 사용(Item Rebuild 가운데 클릭·수량 팝업과 같은 키 없음). 세이브: 원본 PlayerEquipDB.PutOnEquip/TakeOffEquip. Item Rebuild의 장비 묶음 표시는 가방 UI 전용이라 이 목록과 겹치지 않음 [소스]. 장비 시트·UICharacterModel 장비 필드·RefreshTipsRoleInfo를 바꾸는 모드와 접점. 병용 실전 미확인. [근거](tab-characters/0.6.0.md)

아래는 이전 버전 기록이다.

# CTRL Instant 0.1.6 — 2026-09-20

진단 훅을 모두 제거했다. 0.1.3~0.1.5에서 공유하던 `GameManager.PauseGame/ContinueGame`, `UIManager.SetFocusOn/OutUIView`, `UISailingView.CheckRayShip`, `UINpcInteractiveCtrl` 훅과 공통 `XBaseInputAgent.OnEvtCaptureFeatureInput` 관측 훅이 없어졌다. `UIManager.CurrentFocusViewCtrl`을 읽고 `UINpcInteractiveCtrl` 창에 선택 정지를 넘기는 동작은 유지한다. [근거](ctrl-instant/0.1.6.md)

# CTRL Instant 0.1.5 — 2026-09-20

CTRL을 누른 동안 `UINpcInteractiveCtrl`이 포커스를 가져도 자체 정지 토큰을 유지하고, 원본 선단 선택 정지 토큰의 소유권을 그 창의 `CloseHook`에 넘긴다. 이 창의 포커스·수명이나 선택 정지 토큰을 바꾸는 모드와 접점이 있다. [근거](ctrl-instant/0.1.5.md)

# CTRL Instant 0.1.4 — 2026-09-20

0.1.3과 같은 함수를 공유한다. 다만 `UIManager`·`UISailingView`·`UINpcInteractiveCtrl`·`GameManager.PauseGame/ContinueGame` 관측 훅을 게임 시작 시점이 아니라 첫 항해 진입 뒤에 등록한다. 0.1.3의 시작 시점 등록은 검은 화면을 일으켰다(사용자 확인). UI 클래스를 시작 시점에 패치하는 다른 모드도 주의해야 한다. [근거](ctrl-instant/0.1.4.md)

# CTRL Instant 0.1.3 진단 — 2026-09-20

기능 동작은 0.1.2와 같다. 새 관측 훅(void, 원본 불변)은 `GameManager.PauseGame`(Postfix)·`ContinueGame`(Prefix), `UIManager.SetFocusOnUIView`·`SetFocusOutUIView`, `UISailingCtrl.OpenCheckNpcStatus`·`CloseCheckNpcStatus`·`OnAction_CheckTarget_Long`, `UISailingView.CheckRayShip`(매 프레임 가능), `UINpcInteractiveCtrl`(ShowNpcTeamInfo, CloseHook, CloseNpcInteractiveStatus, Trade, Battle, CheckInfo, CheckAndTriggerNpcEvent)다. 정지 토큰·포커스를 다루는 모드와 같은 함수를 공유한다. 기록은 관측 창 안에서만 남긴다. 병용 실전은 미확인. [근거](ctrl-instant/0.1.3.md)

아래는 이전 버전 기록이다.

# Tab Characters 0.5.9 — 공용 상단 메뉴(UIMenu) 접점 추가

새 훅: UIMenuCtrl.GetSheetOpenByIndex(Postfix), UIGlobalCtrl.OnFeatureQuickKeyStart·OnInputActionStart(Prefix, 스킬 탭 3번일 때만 원본 생략), UIMenuView.ItemRender·RefreshBtnKnowledge(Postfix). 공유 상태: 상단 메뉴 목록 ListTitle의 foldInvisibleItems(true로 설정, 복원 안 함), 3번 버튼 visible, 2번 버튼 ctrlNewState. TabMenu(진단)가 ShowChild·PrepareShowChild·UIMenuHelper.Start·CloseHook를 관측하나 동작 변경 없음. 같은 메뉴 함수·목록을 바꾸는 모드와 접점. 병용 미확인. [근거](tab-characters/0.5.9.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.4 — 안내 content만 임시 이동

공용 FeatureUI 정렬 변경 제거. 학습 안내 원본 content 부모/형제 위치/정렬/touchable 및 AniTips._options(이동 순간), timeScale을 공유한다. 같은 안내를 재배치/정렬/재생 제어하는 모드와 접점. 새 훅 없음, 관리 검사 통과이며 native 병용 확인 전. [근거](tab-characters/0.5.4.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.3 — 공통 UI 소실 진단

새 훅/공유 상태 쓰기 없음. 기존 포인트·팁 호출 경계에서 FairyGUI root 자식(통합 치트 포함) 및 팁 조상의 표시·정렬을 읽는다. 관측 비용과 로그 출력 추가, 포인트당 15초/180개 상한. 기존 0.5.2 동작 유지. 병용/실전 해결 미확인. [근거](tab-characters/0.5.3.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.2 — 바깥 클릭 해제 판정

20개 훅 유지. 바깥 입력 영역의 터치 캡처/해제와 이벤트 좌표 사용으로 변경했다. 후속 배경 클릭 취소 유지. 원본 알림/DB 변경 없음. 실제 병용 미확인. [근거](tab-characters/0.5.2.md).

# Tab Characters 0.5.1 — 포인트 획득 후 레벨업 입력

UIHeroLevelUpCtrl 레벨업/입력 4개와 보상 연출 시작 1개를 추가 후킹한다(총 20개). 동일 함수를 바꾸는 모드, UIcomSkillLabel.AniEnter 완료 콜백·touchable을 수정하는 모드와 접점이 있다. 진행 중인 정산 잠금은 유지하고 이전 보상 연출만 원본 완료 경로로 마무리한다. DB/포인트 지급 코드는 대체하지 않는다. 기존 패널 포인트 사용은 다음 프레임으로 이동한다. 실제 병용 미확인. [근거](tab-characters/0.5.1.md).

# Tab Characters 0.5.0 — 클릭 입력과 native 알림

15개 훅 유지. 원본 ShowBaseTips 실행을 막지 않으며 학습 호출 범위의 AniTips.timeScale 및 알림 root 조상 sortingOrder를 임시 변경한다. 같은 Transition/정렬 속성을 바꾸는 모드와 접점이 있다. 일반 알림/종료에서는 복원한다. 바깥 좌클릭은 투명 입력 영역에서 소비하며 Stage.CancelClick을 사용한다. 같은 좌클릭의 배경 동작은 실행하지 않는다. 버튼은 터치 캡처와 해제 판정 사용. 실전 병용 미확인. [패치/근거](tab-characters/0.5.0.md).

# Tab Characters 0.4.1 — 비표시 UI 좌표 변환

15개 훅 유지. 자체 인접 패널 수집에서 displayObject 없는 중첩 그룹을 제외하고 표시 자식만 빌린다. 그룹은 원래 부모에 남고 자식의 그룹 소속은 종료 시 복원한다. 0.4.0의 UI/입력/책·포인트 접점 유지. 대역 재현/수정 검사는 실제 병용 보장이 아니다. [근거](tab-characters/0.4.1.md).

# Tab Characters 0.4.0 — 인접 학습 패널

총 15개 훅. 새 RefreshTipsSkill Prefix는 동일 View의 빌린 native tooltip에 대해 외부 hover/갱신을 제한하고 자체 갱신만 허용한다. 이 함수·UISheetCharacter.groupTips 위젯의 부모/관계/gear/텍스트 색을 수정하는 모드와 추가 접점이 있다. 원본 groupTips 위젯과 책 sheet를 임시 이동/복원하므로 실제 UI 병용은 확인이 필요하다.

중앙 모달과 전체 입력 차단은 제거했다. 취소/짧은 해제 보호 외 입력을 원래 메뉴에 전달하며 페이지 이동은 학습 패널을 먼저 닫는다. 이전 포인트 DB/책 소비/알림 훅 접점은 유지한다. 현재 native 본문·메타데이터 및 대역 검사 통과는 실전 병용 보장이 아니다. [패치](tab-characters/PATCHES.md) · [근거](tab-characters/0.4.0.md).

# Tab Characters 0.3.0 — 학습 UI·포인트

UITipsCtrl.ShowBaseTips 추가, 총 14개 훅. 자체 학습 호출 중의 메시지만 root 최상단 전용 알림으로 전달한다. 같은 팁 훅, 모달 입력, GRoot sortingOrder, 인물 sheet 자식 부모/배치/텍스트 크기 방식을 수정하는 모드와 접점이 있다. 물리 Esc/우클릭은 열린 팝업에만 적용하며 닫기 입력의 해제도 소비한다.

스킬 포인트 버튼은 원본 PlayerHoldRoleDB.UpdateRoleSkillLevelBySkillPoints를 사용하므로 이 함수를 바꾸는 모드와 호출 접점이 생긴다. 잔여 포인트/상한/대상 동일성은 직전 재검사한다. Item Rebuild 책 수량 소비 경로 유지. 정적/대역 검사 통과는 실전 병용 보장이 아니다. [패치](tab-characters/PATCHES.md) · [근거](tab-characters/0.3.0.md).

# Tab Characters 0.2.1 — 책 팝업 중복 렌더·배치

13개 훅 유지. 팝업에서 원본 RefreshSheetSkill/RenderListSkillShow 호출을 제거하여 3D 효과 중복 등록 요청 경로를 끊었다. 책 상세의 미리보기용 content.listSkill을 동기 호출 중 자체 목록으로 바꾸고 finally 복원한다. UISheetSkill/직계 자식의 배치·관계·그룹·표시를 임시 변경/복원하므로 같은 UI를 수정하는 모드는 접점이 있다. 원본 사용 판정/소비와 Item Rebuild 0.1.16의 한 권 차감, 기존 입력 캡처 유지. 원본 TabMenu의 RefreshSheetSkill 계측은 팝업 갱신에서 발생하지 않으며 RenderListBook 등은 계속 계측된다.

사용자 0.2.0 오류 확인에 대한 수정이며 0.2.1 실전 병용 성공을 뜻하지 않는다. [패치](tab-characters/PATCHES.md) · [근거](tab-characters/0.2.1.md).

# Tab Characters 0.2.0 — 인물 책 팝업

새 훅 4개, 총 13개. UICharacterView.RenderListBtnSkill, Ctrl.OnClickBtnBook, InputSystemManager.OnEventCaptureInput, GameManager.ForceReset. 원본 UISheetSkill/listBook/책 선택·필터/미리보기/GUI 포커스를 공유한다. SheetType은 유지하고 원본 학습을 재사용한다. Item Rebuild 0.1.16의 ItemBagData 소비 전처리가 수량을 차감하므로 Tab Characters가 추가 차감하지 않는다.

입력 캡처는 Cheat/Map 등 공통 입력 훅과 직접 접점이 생긴다. 자체 팝업이 열린 동안 입력을 소비하며 닫힌 뒤 해당 press의 release도 소비한다. TabMenu는 원본 RefreshSheetSkill/RenderListBook 등 재사용 호출을 계측한다. 정적/대역 검증은 실전 병용 무충돌 보장이 아니다. [패치](tab-characters/PATCHES.md) · [근거](tab-characters/0.2.0.md).

# Item Rebuild 0.1.16 — 스킬북 실제 수량 통합

기존 47개 훅 유지. 스킬북이 실제 병합 대상이 되어 PlayerBagDB/ItemBagData의 저장 행과 수량을 수정한다. UICharacterCtrl.OnClickBtnBook의 GUID 삭제 호출도 기존 ItemBagData 전처리를 거쳐 한 권 소비한다. 권별 GUID를 보관하는 외부 모드/열린 UI는 중복 행 제거와 접점이 있다. 장비·언어책 제외, 입력·포커스 변경 없음. 정적/대역 검증 통과, 실전 병용 미확인. [계약](item-rebuild/PATCHES.md) · [근거](item-rebuild/0.1.16.md).

# Item Rebuild 0.1.15 — 스킬북 묶음

기존 가방 표시·논리 용량·판매 그룹에 type 72 스킬북 108종을 추가했다. 훅/입력/수명 구조 추가 없음. 가방 UI 행·판매 실제 Slot 선택·논리 용량을 공유하므로 해당 목록이나 용량을 변경하는 다른 모드와의 실전 병용은 미확인이다. 스킬 습득은 원본 GUID 삭제 경로를 유지하고 책 저장 행은 병합하지 않는다.

47개 훅/266개 참조, 오프라인 5,449개 검사 통과. 게임·Steam 미실행. [계약](item-rebuild/PATCHES.md) · [근거](item-rebuild/0.1.15.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.14 — 보급품 최대 선택·휴대 수량

UILandExploreView.Refresh Postfix 추가(총 47개). BtnSupply/왼쪽 보급품 아이콘의 가운데 클릭 이벤트, Supply 모델과 공급 팝업 모델의 계산 상태를 공유한다. 팝업을 열지 않고 원본 ShowHook으로 MaxNum/가격을 준비한다. 따라서 보급품 ShowHook/SetCountPrice를 바꾸는 모드는 이 바로 선택 경로에도 영향을 줄 수 있다. 도구 수량은 별도 비입력 GTextField로 표시하며 재사용/이탈/Reset에 정리한다.

47개 훅·266개 interop 참조, 오프라인 5,323개 검사와 시작 그래프 확인. 게임·Steam 미실행, 실전 입력과 타 모드 병용 미검증. [계약](item-rebuild/PATCHES.md) · [근거](item-rebuild/0.1.14.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.13 — Min/Max 및 가운데 클릭

상점 OnAction_R2/L2 Prefix 두 개 추가. 가운데 버튼의 기본 같은 상품/긴 누름 전체 선택 대신 클릭한 행에 Max를 적용한다. 같은 마우스/상점 입력을 변경하는 모드와의 중첩 검토가 필요하다. UI 행의 onTouchBegin/onRemovedFromStage를 공유하며 native 모델 목록은 유지하고 isSelect만 갱신한다. 탐험은 기존 수량 확정 경로와 무게/재고 검사를 재사용한다.

설치된 UnityEngine.InputLegacyModule에 대한 Private=false 참조만 추가하고 배포 DLL을 추가하지 않았다. 창 포커스와 세션 경계를 검사한다. 46개 고유 훅/257개 참조, 오프라인 5,307개 검사는 실제 입력 타이밍 및 전체 모드 병용 성공을 뜻하지 않는다. 게임·Steam 실행 없음. [계약](item-rebuild/PATCHES.md) · [근거](item-rebuild/0.1.13.md).

아래는 이전 버전 기록이다.

# Cheats Interface 1.3.0 — H 토글

IsOpen 기본 false, H로 펼침/접힘 전환. 도시/항해/세션 및 뷰 재생성에도 유지. 텍스트 입력/앱 비활성 중 토글 차단, X 뒤 H로 재개. 기존 4개 Interface 훅 유지, H 폴링 및 기존 입력 억제 경로 공유. 빌드·Host 92개 검사 통과, 설치·실전 미검증. [계약·근거](cheat/1.3.0.md).

# Item Rebuild 0.1.12 — 저장 복원 장비/기념품 묶음

새 native 훅 없이 44개 유지. 장비 Deserialize가 생성하는 별도 객체를 허용하도록 GUID+ItemId 기준으로 조회 결과를 검증한다. 장착 상태/효과/GUID는 읽기만 한다. 기념품 type=4 41종은 기존 ItemBagData 병합·삭제 및 판매/용량 공유 상태의 대상에 추가했다. 같은 기념품의 개별 GUID를 캐시하는 외부 모드와의 병용은 별도 검토가 필요하다.

5,266개 대역 검사 및 254개 interop 참조 검증 통과. 전체 설치 모드와의 동적 충돌 없음이나 실전 성공을 선언하지 않는다. 게임·Steam 실행 없음. [근거](item-rebuild/0.1.12.md) · [계약](item-rebuild/PATCHES.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.11 — 수량 팝업 입력/배치 수정

UICommonInputNumCtrl 및 UILandExploreSupplyCtrl의 OnAction_A/B 총 4개를 추가했다. 자체 숫자 창의 원본 OnAction_B가 감소 버튼을 FireClick하는 본문을 확인하여 취소로 대체한다. 공통 금액 창의 다른 호출자는 유지한다. 공급 액션 A는 숨겨진 원본 버튼을 경유하지 않고 기존 OnClickBtnOK를 호출하며 B는 원본 Close를 유지한다.

UIContent 내부 추가 자식의 배치/터치, 숫자 제목 및 하단 버튼 visible을 공유한다. 같은 팝업의 액션·제목·버튼·배치를 수정하는 모드는 중첩 검토가 필요하다. 종료 때 원본 표시를 복원하고 콜백을 정리한다. 44개 고유 훅/254개 참조와 5,177개 오프라인 검사 통과는 실제 설치 모드와의 동적 병용 성공을 뜻하지 않는다. 게임·Steam 실행 없음. [계약](item-rebuild/PATCHES.md) · [근거](item-rebuild/0.1.11.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.10 — 판매 표시/마우스 수량

UIPropStoreView.Refresh/ListPlayerRender/ListShopRender와 UIPropStoreCtrl.SelectAllCargos, UILandExploreSupplyView.Refresh 및 SupplyCtrl.ShowHook를 추가했다. 기존 판매/탐험 숫자 팝업 후처리도 마우스 입력으로 확장한다. 상점 모델 단위 목록은 그대로이며 화면의 표시 개수/렌더 인덱스/수량 자식 라벨을 바꾼다. 같은 상점 renderer/리스트 개수/선택 입력을 수정하는 모드는 충돌 검토가 필요하다. UICommonInputNum과 보급품 UIContent 자식 배치/마우스 캡처를 공유할 수 있다.

설치 원본의 전체/같은 상품 선택에서 raw 인덱스로 UI 자식을 조회하는 본문을 확인하고 해당 플레이어 입력을 대응시켰다. 40개 고유 훅/238개 참조, 대역 5,155개 검사 통과. 실제 FairyGUI hit test와 화면 배치 및 전체 설치 모드 병용은 미검증이며 게임/Steam을 실행하지 않았다. [패치 계약](item-rebuild/PATCHES.md) · [근거](item-rebuild/0.1.10.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.9 — 장비 묶음의 용량

PlayerBagDB.GetFreeCapacity, UIBagView.RefreshBagData, UIPropStoreModel.get_SellCount 후처리 3개를 추가했다. 기존 InnerAddItem/Exchange의 공간 검사와 거래 수명 범위도 확장한다. GUID와 저장된 최대 Capacity는 수정하지 않는다. TabMenu 등의 가방 모델/렌더러 중첩 외에 용량 텍스트·ctrlOverLoad를 바꾸는 모드는 표시 순서의 영향이 있다. 다른 모드가 Exchange 안에서 SellCount를 먼저 읽거나 거래 도중 재고를 바꾸면 일회성 공간 비교 보정을 재검토해야 한다.

설치 원본의 직접 BuyCount/SellCount 호출과 구매 우선 결제 본문, 공유 RVA를 확인했다. 총 34개 고유 훅/219개 interop 참조 및 오프라인 3,943개 검사 통과. 전체 설치 모드의 동적 병용/모든 인라인 공간 검사를 검증한 것은 아니며 게임/Steam 실행 없음. [계약](item-rebuild/PATCHES.md) · [본문 근거·제한](item-rebuild/0.1.9.md).

아래는 이전 버전 기록이다.

# Cheats Interface 1.2.3 — 기본 접힘

최초/세션 초기화 시 제목줄만 표시. 수동 펼침과 장소 이동 시 상태 유지. 기존 4개 훅/입력/수명 계약 유지. 빌드 및 Host 80개 검사 통과, 설치·실전 미검증. [근거·배포](cheat/1.2.3.md).

# Item Rebuild 0.1.8 — 여섯 분류/판매 수량

가방 RefreshDataModel 후처리의 장비 표시 그룹, DiscardItem 후처리, 상점 SetPlayerGoods/BtnMulti/SelectSameGoods/Exchange/CloseHook 및 공통 숫자 View.Refresh를 추가/확장한다. 기존 TabMenu의 RefreshDataModel/BagSlotRenderer와 계속 중첩하며 가방 목록·슬롯 인덱스·제목/자식 UI 변경은 상호 영향 가능하다. 상점 선택이나 공통 숫자 팝업을 바꾸는 다른 모드도 공유 상태 검토가 필요하다. GUID별 장비 상태는 읽기만 하며 PlayerEquipDB를 패치하지 않는다.

프로젝트와 분석된 SpecialOrder 소스의 지정 심벌 검색 및 실제 설치 interop/원본 본문 대조 범위다. 전체 설치 DLL의 동적 충돌 없음 또는 실전 병용 성공을 선언하지 않는다. 31개 고유 훅/208개 참조 및 오프라인 3,880개 검사 통과. [현재 패치 계약](item-rebuild/PATCHES.md) · [근거·한계](item-rebuild/0.1.8.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.1.2 — 비표시 초상화 참조 정리

기존 9개 훅 유지. 소유 역할 목록 풀의 분리된 UIbtnRole.loaderRole URL만 비우고 잔여 MyGLoader._handler는 원본 ReleaseLoader로 반환한다. 인물 창 닫기에는 큰/상세 초상화 두 로더도 정리. 선택·저장·입력·3D 수명주기 변경 없음. 공용 이미지 리소스 참조 반환과 비동기 로더 관리 경로를 공유하므로 실제 메모리 감소/다른 로더 모드 병용은 미검증. 설치 MyGLoaderManger.Update의 기존 우회 코드도 조사했고 이를 수정하지 않는다. 빌드·11,509개 대역·9개 서명·60개 API 참조 대조 통과. [계약](tab-characters/PATCHES.md) · [근거](tab-characters/0.1.2.md).

﻿# Item Rebuild 0.1.7 — 수량 여백·서식

BagBadge의 위치 여백과 자체 GTextField 서식만 변경. native 훅 24개, 공유 상태/입력/수명주기 계약 유지. 원래 title 숨김/복원 및 common 자식 배지 접점은 동일하다. 빌드·대역 3,668개·interop 참조 131개 검사 통과, 실제 렌더/병용 미검증. [근거](item-rebuild/0.1.7.md).

# Item Rebuild 0.1.6 — 수량 위치 및 도구 정렬

훅 24개 유지. RefreshDataModel 전처리에서 ItemBagData.Items의 순서를 안정적으로 변경하므로 인덱스를 장기간 보관하는 외부 모드와 공유 상태 영향 가능. 원본 모델 생성 전에만 실행하며 수량/참조 및 다른 항목 상대 순서는 보존. 배지는 부모 로컬 좌표와 최상위 sortingOrder 사용. UI 패키지와 실제 렌더링 병용은 사용자 확인 전. 3,668개 대역 및 129개 참조 검사 통과. [근거](item-rebuild/0.1.6.md).

# Item Rebuild 0.1.5 — 변경 시 수량 갱신

총 24개 훅. InnerRemoveItem 읽기 전후처리, UIBagCtrl.CloseHook 자체 표시 정리 추가. 기존 삭제/추가/병합/롤백에 변경 알림 연결. 200ms 폴링 제거, 변경된 표시만 다음 프레임 갱신. 지정 TabMenu 소스의 CloseHook은 UIMenuCtrl 대상이며 UIBagCtrl 직접 추가 패치는 찾지 못함. 기존 BagSlotRenderer/RefreshDataModel 중첩 유지. 설치 전체 동적 병용은 미검증. 빌드·3,663개 대역·24개 서명·128개 참조 검사 통과. [근거](item-rebuild/0.1.5.md).

# Item Rebuild 0.1.4 — 아이콘 내부 수량 및 동기화

native 훅 22개 유지. common.title의 좌표/서식 변경을 없애고, common 자식 배지와 원래 제목 visible을 관리한다. OnUpdate에서 화면에 남은 배지만 최대 5Hz 갱신, 실제 ItemBagData의 GUID+ItemId로 수량 확인. 슬롯 재사용/화면 이탈/ForceReset 시 정리. 다른 모드가 같은 제목 visible 또는 자식 순서를 변경하면 상호 영향 가능하며 실전 병용은 미검증. Release/대역 3,654개/interop 참조 125개 및 초기화 호출 그래프 검사 통과. [근거](item-rebuild/0.1.4.md).

# Item Rebuild 0.1.3 — 가방 수량 배지

기존 22개 훅 유지, BagSlotRenderer 후처리의 가방 제목만 이름 없는 검은색 `x수량`·절반 크기·우측 아래로 변경. 재사용 슬롯의 자체 서식/좌표 복원 추가. 저장·역참 입력·수명 정책 유지. Release/기존 대역 3,644개/interop 검사 통과, 새 배치 실전 미검증. [근거](item-rebuild/0.1.3.md).

# Item Rebuild 0.1.2 — 역참 클릭 무반응

훅 22개 및 초기화 지연 정책 유지. 원본 ListLandBag[0]의 null 보급품 칸을 검색/표시에서 제외하고 출발 변환에서는 보존한다. 같은 공유 목록을 쓰는 선택·취소·출발의 인덱스 계약 수정이며 저장/3개 상한/무게 정책 변경 없음. 사용자 로그의 IndexOf→ChooseLand 예외를 대역으로 먼저 재현하고 수정 후 3,644개 검사, 빌드·서명·참조 검사 통과. 0.1.1의 타이틀 진입 이후 진행은 로그 확인, 0.1.2 실전 병용 미검증. [근거](item-rebuild/0.1.2.md).

# Item Rebuild 0.1.1 — 타이틀 이전 검은 화면

0.1.0 포함 시 검은 화면/제거 시 정상이라는 사용자 보고 및 Unity 로그의 FairyGUI·UIManager.StartUp 초기화 예외 확인. UIManager generated interop 초기화가 native .cctor → Stage.Instantiate를 조기 실행하는 경로를 확인했다. 총 훅 22개 유지, UIManager.ShowInputNumPromptBox 1개만 최초 역참 수량 클릭으로 지연. 저장·입력 계약 유지. Release 경고/오류 0, 대역 3,636개·compiled 초기화 호출 그래프·서명 22개·참조 94개 통과. 수정 후 실전 병용은 미검증. [근거](item-rebuild/0.1.1.md).

# Item Rebuild 0.1.0 — 2026-09-20

역참 도구 13종 보유 기록/수량 통합 및 출발 수량 팝업, 고유 훅 22개. PlayerBagDB/ItemBagData와 가방·탐험 모델, 공통 숫자 입력을 공유한다. 비무역 대상 ID/현재 가방 포인터/스레드로 저장 수정을 제한하고 숫자 입력은 자체 팝업 모델 포인터로 제한한다. 닫기·타 팝업·Reset은 미확정 입력 취소. TabMenu의 RefreshDataModel/BagSlotRenderer와 직접 중첩하며 진단 훅은 읽기 전용이다. ForceReset 수명 경계도 공유한다. 프로젝트 및 분석 SpecialOrder 1.1.0 지정 심벌 검색 결과이며 전체 설치 DLL·동적 병용 성공 보장은 아니다. Release 빌드, 오프라인 대역 3,633개, native/interop 대조 통과. 게임/Steam 실행 없음. [패치 계약](item-rebuild/PATCHES.md) · [근거/제한](item-rebuild/0.1.0.md).

# Tab Characters 0.1.1 — 가운데 정렬

기존 9개 훅 유지. 소유 listRole.align을 Center로 설정하고 닫기/다른 탭 전환 때 원래 값 복원. 입력·선택·페이지·캐시 정책 유지. 사용자 0.1.0 화면 표시 보고 확인, 0.1.1 실전 정렬/병용 미검증. [근거](tab-characters/0.1.1.md).

# Tab Characters 0.1.0 — 2026-09-20

인물 탭 10명 페이지 신규 DLL, 고유 훅 9개. UICharacterView.Refresh/HideHook, Ctrl.ShowSheet/좌우 버튼, GList.set_numItems/ScrollToView(3인수)/HandleArrowKey/Dispose를 사용한다. 공통 GList 변경은 해당 인물 listRole native pointer 하나로 제한한다. 전체 명단/선택 인덱스/저장/전역 입력 포커스 변경 없음. 원본 행 renderer에 페이지의 전체 인덱스를 전달해 클릭 콜백을 유지하며 닫기·다른 탭·Dispose에 복원한다.
TabMenu 진단의 View.Refresh와 직접 중첩한다. 분석 사본 SpecialOrderEra 1.1.0 CommerceMilestoneUiRuntime의 GList.set_numItems도 동일 대상이지만 상회 건물 목록에 제한된다. 정적 범위 대조이며 전체 설치 모드·실행 패치 순서/병용 검증은 아니다. Release 경고/오류 0, 대역 11,484개·서명 9개·참조 38개 통과. 게임/Steam 실행 없음. [계약](tab-characters/PATCHES.md) · [근거/한계](tab-characters/0.1.0.md).

# Cheats Interface 1.2.2 — 기본 펼침 복원

기본/세션 초기화 시 펼침, 장소 전환 자동 접기 제거. 수동 접기·프로세스 닫힘 및 기존 입력/4개 훅 계약 유지. 사용자 로그와 설치 목록에서 Battle DLL 누락 확인: 배포 ZIP에 Battle 1.0.0 동봉. Battle 로직/배율 유지 정책은 변경 없음. 빌드 및 관리 검사 129개 통과, 설치·실전 검증 전. [근거·배포](cheat/1.2.2.md). 아래는 과거 기록.

# Cheats Battle 1.0.0 — 전투 능력/포격

신규 고유 함수 7개. Interface Panel.Visible/입력/세션 Reset을 사용하며 장면 전환은 Speed/Host와 공유한다. CombatUnit 네 능력치와 AttackInfo.gunDamageFactor는 다른 전투 모드와 공유 가능하므로 원래 값 기준 및 소유 조건 복구를 사용한다. 원본 항해사·HP·저장·공용 속성 getter 변경 없음. 기존 Cheats 다른 기능과 분석 외부 소스의 지정 심벌 검색에서 직접 등록 접점을 발견하지 못했지만 전체 설치 DLL·동적 패치 충돌 검증은 아니다. 백병전 종료는 선택 유지, 전체 해상 전투 종료는 X1. [패치·근거·검증 한계](cheat/battle-1.0.0.md).

# Cheats 1.2.1 — 공헌도 숨김·진입 시 접기

공헌도는 도시 밖 숨김. 최초/도시/항해 진입 시 제목줄만 남기는 최소화. 동일 장소에서는 사용자 펼침 유지. Interface 입력·장면 읽기·세션 수명 공유, 새 훅/게임 상태 쓰기 없음(4+1+2 유지). 빌드 및 248개 관리 검사 통과, 설치·실전 미검증. [근거·배포](cheat/1.2.1.md).

# Cheats 1.2.0 — 장소별 패널 숨김

Interface 1.2.0 / Speed 1.1.1 / Bargirls 1.1.1. 항해 조건 밖 속도 패널 숨김, 도시 밖 여급 패널 숨김. 도시 안 여급 기존 제한 유지. 숨긴 공간 제거·포커스 해제, Refresh/세션/기존 4+1+2 훅 유지. 빌드·375개 관리 검사·원본 기준 대조 통과, 설치 및 실전 검증 전. [변경·근거](cheat/1.2.0.md).

# Cheats Interface 1.1.3 — 비율 기반 초기 위치

자체 창 초기 좌표 계산만 변경. 화면 너비 9%/높이 6.5% 여백. 기존 훅·입력·공유 상태·수명주기 유지, 실전 병용 미검증. [근거](cheat/1.1.3.md).

# Cheats Interface 1.1.2 — 기본 위치 여백

자체 창 최초 위치만 왼쪽 96/아래 84로 변경. 기존 훅·공유 게임 상태·입력·수명주기 유지. 관리 검사 통과는 실전 병용 보장이 아니다. [근거](cheat/1.1.2.md).

# Contribution 0.5.4 — 도시 메인 혜택 HUD

도시 메인 UI 상태·포커스·실제 표시를 읽어 혜택 HUD만 제한. 시설/다른 메뉴에서 숨김, 복귀 시 표시. 14개 훅 유지, 상품 인식·해금·알림·저장 변경 없음. 빌드/관리 검사 통과, 실전 미검증. [근거](contribution/0.5.4.md).

# Cheats Bargirls 1.1.0 — 2026-09-19

새 훅 0개. 과제 완료 수치 직접 쓰기 제거. 현재 여급의 Topic/PlayerTalk 완료 기록 및 해당 roleId 임무만 읽어 진행 중 상승을 막는다. 과제 상한도 읽고, 원본 SocialDB.UpdateFavorability의 dirty 경로를 공유한다. Interface 입력·세션·뷰 수명주기 유지. 원본 여급 퀘스트/대화/보상 이벤트는 수행하지 않는다. 정적·관리 검사 통과, 실전 병용 미검증. [상세](cheat/bargirls-1.1.0.md).

# Cheats Bargirls 1.0.0 — 2026-09-19

신규 훅 0개. Interface의 입력·세션·뷰 수명주기를 재사용한다. PlayerSocialData.Favorability/CompleteTask 및 SocialDB dirty 이벤트가 원본 여급 과제·하트·저장과 공유된다. 단계 제한 상승은 과제 진행을 건너뛰며 보상 목록은 유지한다. Fleet Info와 여급 UI를 공유하나 함대 정보 함수에는 접근하지 않는다. 정적·관리 검증은 실전 병용 보장이 아니다. [근거](cheat/bargirls-1.0.0.md).

# Cheats Money 1.0.0 — 2026-09-19

소지금 목표값 기능 신규 DLL, Harmony 훅 0개. Interface의 입력·플레이어·뷰 수명주기를 재사용한다. 적용 시 PlayerCurrencyDB.ModifyAmount(1,target) 최대 1회와 원본 MarkDBDirty 경로를 사용하며 외부 결제/보상·잔액 표시와 공유 상태가 있다. 직접 필드 쓰기/강제 저장/추가 입력 훅 없음. 정적·관리 검사는 통과했으며 실전 병용은 미검증. [근거](cheat/money-1.0.0.md).
# Trade Exp 0.1.1 — 2026-09-19

대상 함수 2개는 유지하며 단일 분기 변경을 표시 합계 블록 교체로 대체했다. Int64 덧셈 overflow 검사 및 0~Int32.MaxValue 제한 추가. 지급/저장/입력 수명주기와 전체 함수 대조/소유 복원 계약 유지. 빌드와 1,132개 독립 네이티브 산술 검사 통과, 실전 병용 미검증. [계약](trade-exp/PATCHES.md) · [근거](trade-exp/0.1.1.md). 아래 이전 기록 보존.

# Trade Exp 0.1.0 — 2026-09-19

무역 정산 AccountRefresh 및 <AccountRefresh>b__4의 표시 상한 분기 각 1바이트 수정. 지급/저장/입력/포커스 변경 없음. Contribution의 시장 추적과 UI 수명주기를 공유한다. 검토한 프로젝트 소스와 SpecialOrder 1.1.0 분석 소스에서 두 대상 직접 패치는 검색되지 않았다. 디스크 해시와 두 실행 함수 전체 바이트 불일치시 적용하지 않으며 해제시 자체 변경만 복원한다. 정적 검사·빌드 통과, 실전 병용 미검증. [계약](trade-exp/PATCHES.md) · [근거](trade-exp/0.1.0.md).

# Cheats Interface 1.1.1 — 스크롤바 래퍼 수정

GetChildAt 반환값의 GGraph 직접 형변환 제거, 생성 시 보관한 참조 사용. 기존 4/1/2 훅·입력·수명주기 계약 유지. 사용자 1.1.0 오류 확인, 수정본 실전 검증 전. [원인·검증](cheat/1.1.1.md).

# Cheats 1.1.0 집합 UI — 2026-09-19

Interface4/Speed1/Contribution2 훅 유지. 공통 제목줄/스크롤 입력 소유권 추가, 기능이 비활성이어도 제목줄 조작 가능. UIManager 포커스 변경 없음, sortingOrder25000 유지. 모달 Contribution30000보다 아래다. 접힘/닫힘은 기능값 변경 없이 표시만 제어한다. GGraph/Text만 사용하며 image texture/material 경로 없음. 세 DLL 함께 배포, 기능 소유권/기존 동작 관리 검사 통과는 실전 입력 병용을 보장하지 않는다. [상세](cheat/1.1.0.md).

# Contribution 0.5.3 셰이더 속성 — 2026-09-19

14개 훅 유지. 자체 material 키워드/자체 NGraphics clipping flag만 변경. _ClipBox 속성 존재 여부와 clipping 지원을 동일시하던 검사 제거. 설치 shader 자원 대조 및 관리 검사 완료, 실제 렌더/병용 미검증. [근거](contribution/0.5.3.md).

# Contribution 0.5.2 이미지 재질 — 2026-09-19

기존 14개 훅 유지. 전역 ShaderConfig는 읽고 각 자체 이미지 전용 Material만 생성·해제한다. 원본 NGraphics custom material/clipping 경로 사용. texture/material의 자동 자원 정리를 방지하고 플레이어 Reset에서 자체 자원을 반환한다. 전역 shader 교체/다른 UI material 변경 없음. 실제 병용/렌더 정상화는 사용자 실행 전 미확인. [상세](contribution/0.5.2.md).

# Contribution 0.5.1 버튼 — 2026-09-19

새 Harmony 훅 없음. 자체 버튼 클릭 시 게임 CommonSoundUtils.SpecialClick/공통 AudioPlayer를 호출하는 음향 접점 추가. 음량 설정은 변경하지 않고 음향 예외시에도 확인/입력 해제 진행. 기존 모달 정렬·입력·저장 계약 유지. 실제 음향/병용은 사용자 확인 전. [근거](contribution/0.5.1.md).

# Contribution 0.5.0 UI — 2026-09-19

기존 14개 훅, 도시/저장/입력 공유 접점 유지. 자체 GRoot 자식과 Unity texture 캐시를 사용한다. 현재 포커스/게임 데이터는 변경하지 않으며 알림의 기존 입력 차단 및 확인 후 짧은 차단을 유지한다. 알림 정렬은 표시용 복사만 바꾸고 저장 목록은 유지한다. 정적·관리·GDI 대역 검증 통과는 실제 FairyGUI 렌더/다른 모드 병용 성공을 보장하지 않는다. [상세](contribution/0.5.0.md).

# CTRL Instant 0.1.2 — 2026-09-19

훅 8개 유지. 잘못된 HUD 포커스 필수 조건을 시작/유지 양쪽에서 수정했다. 시작에는 해양 장면 입력 포커스, 유지에는 자체 정지·선택 직후 UI 포커스 대상의 동일성을 사용한다. UIManager 포커스는 읽기만 하며 다른 창으로 변경되면 자체 토큰을 정리한다. 같은 입력·정지 수명주기 접점을 유지하고 새 우선순위는 없다. 정적 검증 통과, 실제 병용은 사용자 확인 전. [0.1.2](ctrl-instant/0.1.2.md).

# CTRL Instant 0.1.1 진단 — 2026-09-19

기존 7개에 공통 `XBaseInputAgent.OnEvtCaptureFeatureInput`의 CheckTarget 전용 관측 Prefix를 추가했다. 총 8개. 원본 차단·인자 변경 없는 void Prefix이며 물리 CTRL 폴링도 관측만 한다. 기존 정지/선단 선택/입력 차단 정책 유지. 0.1.0의 사용자 기능 실패를 해결했다고 판정하지 않는다. 현재 원본·interop 대조 통과, 실전 로그/병용은 사용자 실행 전. [진단과 배포](ctrl-instant/0.1.1.md).

# CTRL Instant 0.1.0 — 2026-09-19

항해 CheckTarget 시작/성립/종료, 항해 UI CloseHook/DisposeHook, 항해 상태 Exit, GameManager.ForceReset 총 7개 훅. 원본 정지 토큰과 UI/입력 수명주기를 공유하며 별도 DLL로 분리했다. 선단이 없어도 정지하고 자체 소유 토큰만 반환한다. 다른 창이 포커스를 가지면 새로 시작하지 않으며 키 폴링으로 입력 캡처를 우회하지 않는다. Cheats Speed가 읽는 IsGamePaused를 사용하지만 FixedUpdate/이동 힘/Unity timeScale에는 새 훅이나 쓰기가 없다.

7개 현재 원본 주소/서명/본문과 토큰 관리 검사는 통과했다. 실제 병용과 선단 상호작용은 사용자 검증 전이다. [현재](ctrl-instant/CURRENT.md) · [계약](ctrl-instant/PATCHES.md) · [검증과 한계](ctrl-instant/0.1.0.md). 기존 기록은 아래 보존한다.

# SpecialOrderEra 1.1.0 패치 ZIP 병용 분석 — 2026-09-19

이번 ZIP DLL은 기존 분석 사본과 해시가 달라 새로 디컴파일하고 실제 설치 Restitutor 11개 DLL과 대조했다. [분석·증거/한계](../../analysis/specialorder-1.1.0-review/REPORT.md), [필요한 호환성 패치 제안](../../analysis/specialorder-1.1.0-review/COMPATIBILITY-PLAN.md).

우선 대응: 공헌도 치트의 외부 조회 계약/3,000 상한, 자동 혜택의 상승 전 값 관측 순서, 옵션 행 지연 레이아웃. 추가 대응: 항로/출항 준비 포커스에서 내파 지도 지원 및 창 입력 독점. 배속과 외부 감속 상한은 기능 정책을 구분한다. 설치 HarmonyX는 Prefix들을 실행한 뒤 bool 결과를 합산하므로 ‘외부 false가 내 Prefix를 무조건 생략’하는 것으로 설명하지 않는다. 정적 분석이며 병용 재현·수정·설치는 하지 않았다. 기존 기록은 아래 보존한다.

# Cheats 1.0.0 모듈 분리

기존 cheat 7개 패치를 Interface 4 / Speed 1 / Contribution 2로 분담했다. 입력·플레이어·저장 수명주기는 Interface 하나만 등록한다. 기능 모듈은 Interface에만 의존하며 각자 패널/상태/패치를 해제한다. 원본 함수 대상과 속도/공헌도 쓰기 범위는 유지한다. 기존 Map/Contribution/외부 FixedUpdate 모드 접점과 실전 병용 검증 한계도 그대로다. 임의 Harmony 우선순위 없음.

`Restitutor_Cheats_Contribution.dll`은 목표값 치트이며 기존 도시 혜택 모드 `Restitutor_fixes_Contribution.dll`과 별개다. 후자는 교체/삭제하지 않는다. 이전 단일 `Restitutor_fixes_cheat.dll`과 새 세 DLL은 함께 설치하지 않는다. 빌드·217개 관리 검사·IL/원본 재대조 완료, 분리판 실제 로딩/병용은 사용자 검증 전. [상세](cheat/1.0.0.md).

## 이전 기록

# Cheat 0.3.2 항해 지도

7개 훅 유지. 항해 배율 선택에서 지도 UI/일시정지 읽기 의존 제거, 실제 이동에서는 일시정지 차단 유지. 지도 종료 원본의 Little 렌더 상태를 사용자가 연 지도와 혼동하는 접점을 배속 경로에서 제거한다. 공헌도 경계는 그대로. 기존 FixedUpdate 외부 모드 접점 유지, 실제 병용 검증 전. [조사·수정](cheat/0.3.2.md).

# Cheat 0.3.1 지도 복귀 판정

등록 7개 유지. UIMap/항로/출항 준비 view의 IsOpen 및 실제 UIContent stage/부모/group 표시를 읽어 차단한다. Map의 UIContent.visible 변경과 읽기 접점은 있으나 쓰기·지도 수명주기 훅·포커스 변경은 추가하지 않는다. 항해 종료 X1 유지, 이유 변화 로그 추가. 현재 사용자 증상의 실제 차단 조건과 해결/병용 성공은 미확인. [검증과 근거](cheat/0.3.1.md).

# Cheat 0.3.0 항해 이동 배율

기존 6개에 BoatEntityOceanDriver.FixedUpdate Prefix/Finalizer 추가, 총 7개. 현재 해양 장면의 플레이어 포커스 함대/일반 항해 상태로 한정하고 이동 배율 필드를 호출 범위에서 변경·복원한다. 전역 시간·소모·저장 속성 쓰기 없음. 입항/전투/로딩/항해 종료 시 선택 X1. 지도/일시정지는 효과·조작을 중지한다. 기존 공헌도/초기화/UI 입력 접점 유지.

검토한 프로젝트 모드 소스에서 신규 FixedUpdate/escape 필드의 다른 사용은 발견하지 못했다. 외부 분석 사본 SpecialOrderExpansion SeaworthinessRuntime은 **같은 FixedUpdate**에 속도 제한 Postfix 및 조타 Prefix/Finalizer를 등록한다. BattlePacingRuntime은 같은 함수와 escape 필드를 사용하며 분석 사본에서는 Enabled=false. 현재 설치 외부 DLL과 사본 일치는 미확인. 본문의 배율 읽기 및 복원 검사로 실제 이동 배율·병용 무충돌을 보장하지 않는다. [근거·한계](cheat/0.3.0.md). 임의 우선순위 없음.

# Contribution 0.3.1

AddNaturalResources 성공 Postfix 추가로 총 14개. 원본 유지, 현재 플레이어/WorldPort/자원 기록 확인 후 자체 공개 예약만 변경한다. 효과 적용/자원 등록과 KnowledgeDB 사이의 예약 연결이며 기존 도시/저장/알림 수명주기를 사용한다. 관련 기존 소스 검색에서 이 함수 직접 패치는 발견되지 않았다. 주소 별칭 검사/관리 검증 통과는 런타임 병용 무충돌을 보장하지 않는다. [변경·근거](contribution/0.3.1.md).

# 2026-09-19 Contribution 0.3.0

Contribution 총 13개 훅. AddEnterPort(int) 관측 추가, 원본 유지. 검토한 NavtoCity/Map/Cheat/InstantEntrance 소스에서 이 함수 직접 등록은 발견되지 않았다. 실제 입항 EnterHarbourAniEnd의 인라인 완료 코드에서 직접 호출하는 본문을 확인했다. 기존 장면/저장/입력 수명주기에 KnowledgeDB 기록 및 신규 도감 표시/dirty 이벤트 접점이 추가된다. UIMapHarbourCargoItem이 상품+도시 해금값을 읽으므로 지도 표시 상태와 공유하지만 지도 컨트롤러를 만들거나 패치하지 않는다. 부가 저장 v3로 공개 예약/보류 알림/최초 100 이력을 보존한다. 데이터 준비/현재 도시/지식 DB 일치를 확인한다. 정적·대역 검증은 실제 병용 무충돌을 보장하지 않는다. [상세](contribution/0.3.0.md).

# 2026-09-19 이전 변경

## Fleet Info 0.1.0

[패치 목록](fleet-info/PATCHES.md): UIBarGirlCtrl.GetNpcsBySeaAreaId의 Owner 사전 삽입 정책 immediate 1바이트만 변경한다. 원본 목록 사전은 첫 Owner 대표를 유지하며 WorldShip/저장/함대 객체를 변경하지 않는다. 입력/포커스/애니메이션/Dictionary 공통 함수 훅 없음. 기존 프로젝트 소스의 BarGirl/Drunkery/FleetInfo 직접 참조 검색에는 접점이 없었으나, 외부 모드 및 이후 로드된 패치의 동시 변경은 미검증이다. 초기화 때 전체 네이티브 함수가 기준과 다르면 적용하지 않는다. 빌드와 정적·저장 사본 정책 모사는 통과했으며 실제 병용/실전 성공은 사용자 확인 전이다.

Contribution 0.2.2 / Map 0.2.1 설치 해시 확인. 훅 수는 각각 12/40으로 유지한다. Contribution은 시장 View/_state/컴포넌트/Model 유효성 확인·갱신 예외 격리·실제 상승 시 누락 보충을 추가한다. Map은 소유 항로 세션의 loaderIcon.url과 ctrlSelfPort.selectedIndex를 저장·변경·복원한다. 기존 시장/지도 UI 수명주기와 입력·저장 접점은 유지하며 새 전역 입력 훅/우선순위/공헌도 쓰기는 없다. 대역·정적 검사는 실제 병용 무충돌의 증거가 아니다. [공헌도](contribution/0.2.2.md), [지도](map/0.2.1.md), [사용자 확인](../../analysis/user-reports/2026-09-19-map-contribution/USER-CHECKS.md).

아래는 이전 검토 기록이다.

# 현재 패치와 공유 상태의 호환성 검토

## Contribution 0.2.0 상승 전용

[현재 패치 12개](contribution/PATCHES.md). 하락 회수, 시장 거래 차단, 상회/투자/선단/월간 보고서 훅 및 상태를 제거했다. UpdateInfluence 관측은 상승만 예약하며 Cheat의 숫자 변경은 그대로다. 기존 저장 v1에서 회수 상태는 가져오지 않는다. 메뉴/입력 확인창, AddGameEffectInstant의 획득 도시 보정, 시장 갱신과 저장 수명주기는 여전히 공유한다. 빌드/관리 테스트/등록 RVA 및 IL 대조 완료. 실제 병용 결과는 미확인. 과거 기록은 아래에 보존한다.

## Cheat 0.1.1 입력 수정

입력 제한 정규식만 수정했으며 12개 패치/입력 차단/공유 상태 범위는 그대로다. 0.1.0 로드와 창 표시는 사용자 화면·로그로 확인, 0.1.1 실제 입력/병용 결과는 미확인. [수정 근거](cheat/0.1.1.md).

## Cheat 0.1.0 추가 (기존 기록)

[12개 패치와 범위](cheat/PATCHES.md). 목표 공헌도 변경 시 UpdateInfluence를 1회 호출하므로 Contribution의 관측 경로를 공유한다. 치트 호출 중 동일 지휘관의 133/21번 속성 조회를 각각 1회 중립화하고 원본 속성은 수정하지 않는다. 조회 토큰 소모 후 원본 이벤트/후처리에는 대체가 남지 않는다. PlayerData/WorldPortHoldDB 초기화, Map 일반/항로 지도 Show/Close/Dispose 및 InputSystemManager.OnEventCaptureInput과 GRoot 포커스를 공유한다. 자체 창 정렬 25000은 Contribution 확인창 30000보다 아래다. 임의 Harmony 우선순위 없음. 정적/관리 코드 검사 완료이며 실제 후킹·UI·자동 언락·저장 재로드·병용 정상 동작은 사용자 실행 전 미확인이다.

## Contribution 0.1.0 추가

[당시 패치 23개와 계약](contribution/PATCHES-0.1.0.md). 공헌도 변경 관측, 허가·상회·시장·선단 상태 변경 및 FairyGUI 안내를 추가한다. 지도 모드와 InputSystemManager.OnEventCaptureInput을 공유하며 자체 확인창 중 원본 입력을 차단한다. 저장/로드와 UI 수명주기도 공유한다. 설치 네이티브 SetFleetTime의 기존 비용/보고서 trampoline을 본문 기준으로 확인했으나 실제 병용 실행은 미검증이다. 관리 코드/대역 테스트 통과는 IL2CPP 후킹, 저장 재로드, 포커스·월간 보고서 UI 정상 동작을 보장하지 않는다. 임의 Harmony 우선순위를 추가하지 않았다.

## TabMenu 0.1.0 추가
[진단 패치 27개](tab-menu/PATCHES.md). 메뉴 Start/ShowChild 전후로 가방·동료·책·장비 구성/갱신 시간 및 일부 renderer 호출 수를 관측한다. 대상 RVA의 중복/별칭은 현재 메타데이터에서 발견되지 않았다. 검토한 intro/textspeed/map/instant-entrance/nav-to-city 소스에서 동일 대상 직접 등록은 발견되지 않았으며, 공통 입력 캡처/GList/EffectCalculator에는 훅을 추가하지 않는다.
원본 실행/인수/반환값/예외/입력·포커스/저장 상태는 유지한다. 게임 객체는 기존 훅 인스턴스에서만 읽고, UI 싱글턴을 새로 만들어 조사하지 않는다. 자체 창/호출 스택/유한 큐만 수정한다. NavtoCity와 IL2CPP GC/프로세스 통계 조회 및 배경 기록 부하가 합산될 수 있다. UI 수명주기는 기존 모드와 공유하므로 이 정적 접점 검사만으로 충돌 없음/실전 성공을 선언하지 않는다. 실제 후킹 및 부하/병용 결과는 사용자 실행 전 미확인.

## NavtoCity 0.2.0 계측 보강
대상 15개와 원본 실행 계약은 0.1.0 그대로다. ResourcesManager 장면 사전/임시 목록 Count, iterator state/current, IL2CPP GC/힙, 반환 scene operation 상태를 읽는다. 완료 콜백 교체/구독, 참조 카운트 Release 호출, 강제 GC/로딩 완료 없음. 최대 64개 observer는 입항 관측 창 종료 시 비운다. 기존 모드와 같은 장면 수명주기를 관측하므로 조회/보관 비용과 제네릭 operation 읽기의 실제 병용 결과는 0.2.0 사용자 실행 전 미확인이다. [현재 패치](nav-to-city/PATCHES.md), [추가 근거](nav-to-city/0.2.0.md).

## NavtoCity 0.1.0 추가
[진단 패치 15개](nav-to-city/PATCHES.md). 입항/장면/리소스/도시/자동 저장의 실행 시간을 관측하며 원래 함수, 인수, 결과, 완료 콜백, 입력, 캐시와 저장 상태를 변경하지 않는다. SceneManager/HarbourScene은 읽기만 하며 자체 기록 상태와 큐를 관리한다. 기존 intro/textspeed/map/instant-entrance의 검토한 패치 목록에서 동일 메서드 직접 등록은 발견되지 않았다. 전환 수명주기와 원래 자동 저장 경로는 기존 모드와 공유하므로 실제 부하/패치 순서/병용 정상 동작은 사용자 실행 전 미확인이다. 기존 기계 판독 목록은 이전 범위 기록으로 이 모드를 포함하지 않는다.

## Instant Entrance 0.1.0 추가
[패치 3개](instant-entrance/PATCHES.md). 기존 intro/textspeed/map 소스에서 같은 대상 직접 등록은 확인되지 않았다. Animator.Play(string,int,float)는 공통 메서드이나 항구 등장 호출의 동기 실행 범위와 이름이 모두 일치할 때만 인수를 변경한다. 중첩·예외 복원 및 스레드 격리는 대역으로 확인했으며 네이티브 및 병용 실행은 미검증이다. 아래 세 모드 수치/기계 판독 목록은 이전 범위 기록으로 이 모드를 포함하지 않는다.
2026-09-18. 정적 조사이며 병용 실행 결과가 아니다.

## Restitutor 세 모드
현재 소스에서 intro 4개, textspeed 5개, map 32개 등록을 추출했다. [기계 판독 목록](PATCHES.json). 지도 0.1.5는 항구 onClick 콜백을 추가로 패치하며 항로 상태 밖에서는 원본 실행한다.
서명 기준 서로 같은 대상을 직접 등록하는 항목은 확인되지 않았다. 이것만으로 충돌 없음은 아니다. FairyGUI 수명주기와 전역 UI 상태는 서로 다른 메서드를 통해서도 영향을 받을 수 있다.

| 모드 | 주요 공유 상태 | 추가 패치 시 점검 |
|---|---|---|
| intro | Transition, GroupDeal, 시작 완료 | 완료 중복, 표시/입력 잔여, 약관 상태 |
| textspeed | 효과 콜백, 풀링된 설정 행 | 취소/완료 중복, 행 복원, 객체 교체 |
| map | 포커스, 입력 액션, 지도/항로 UI, 공통 안개 | Prefix 실행 차단, 확인창, release, 저장/해제 |

## 외부 모드에서 확인한 동일 메서드 접점
기존 분석 사본 SpecialOrderEra의 PanelShortcuts.Install은 InputSystemManager.OnEventCaptureInput에 BeforeNativeInput Prefix를 등록한다.
현재 Restitutor map도 같은 함수에 CaptureClose Prefix를 등록한다.
근거: [외부 소스](../../analysis/decompiled/SailingEraSpecialOrderExpansion.Core.PanelShortcuts.cs), [기존 색인](../../analysis/PATCH_TARGETS.md).
[CONFIRMED: 정적 등록 접점] 동일 메서드 대상이다.
[UNKNOWN] 실제 설치된 외부 DLL과 분석 사본의 일치, 활성 패치 순서, 두 모드의 런타임 조건 중첩, 충돌 발생 여부는 미확인.
병용 요청 시 두 핸들러의 포커스·액션·반환 false 조건과 Harmony 순서를 대조한다. 임의로 우선순위를 추가하지 않는다.

## 점검 절차
1. 관련 CURRENT/PATCHES만 읽고 정확한 서명·overload·RVA 공유를 확인한다.
2. 같은 메서드뿐 아니라 같은 필드·UI·저장·초기화/해제 경로를 찾는다.
3. 원래 실행 차단·반환값·완료 콜백·포커스와 해제 순서를 대조한다.
4. 기준 해시와 현재 파일을 비교한다. 다르면 관련 본문과 문서를 갱신한다.
5. 정적/대역 확인과 사용자 실제 실행 결과를 별도로 기록한다.
외부 모드 전체에 대한 충돌 검사를 완료한 목록이 아니다.

## 지도 0.2.0 별도 후보
설치본 0.1.5는 유지. [후보 패치](map/PATCHES.md)는 40개이며 UIMapLine 의존 진입/갱신/종료를 대체한다. UI 포커스·입력·메뉴 안내·전환·경유지 목록을 공유하므로 기존의 관측 모드와 병용 실행은 사용자 확인이 필요하다. 이 후보와 이전 지도 DLL을 동시에 설치하지 않는다. 정적 검증만으로 무충돌을 보장하지 않는다.




## Contribution 0.2.1 HUD 준비 확인
새 훅/공유 상태 쓰기 없음. 현재 플레이어와 지휘관 준비 확인을 OnUpdate 앞에 추가해 미준비 기간에는 HUD 및 예약 적용을 보류한다. 기존 해금 계산 유지. 지도 DisposeHook은 변경하지 않았다. 실제 병용 오류 해소는 미검증. [근거](contribution/0.2.1.md).

## Cheat 0.1.2 지도 선택
추가 훅/상태 쓰기 없음. 현재 선택 조회만 사용하며 Map CurrentRoutePort의 기존 연결을 유지한다. 전체 아이콘 선택 플래그 합산 제거. DisposeHook 예외 수정은 포함하지 않는다. [근거와 한계](cheat/0.1.2.md).

## Cheat 0.2.0 현재 도시 전용
지도 훅 전부 제거, 등록 6개 전체 RVA 별칭 검사 통과. UIManager 열린 목록 읽기 및 자체 창 입력/포커스만 관리. Contribution의 공헌도 관측과 초기화/입력 접점 유지. DisposeHook 공유주소 위험 제거는 확인했으나 출항 충돌 해소 실전 확인 전. [상세](cheat/0.2.0.md).

## Cheat 0.2.1 입력 보정
자체 입력창 onChanged에서 텍스트 상한만 보정. 새 네이티브 훅/게임 데이터 쓰기 없음. 기존 입력 차단/현재 도시 정책 유지. [근거](cheat/0.2.1.md).





## CityEditor Core 0.6.0 — 셀레네 선택·시작 (2026-09-23, 준비본)
신규 Postfix7개: 선택 장면/뷰 종료3, 상회 사전1, 생성/항구 관측3. [계약](city-editor-core/PATCHES.md). NavtoCity0.2.0과 HarbourScene.OnEnter가 겹치지만 이번 처리기는 PlayerData 읽기·로그뿐이다. 원본 EnterHarbourAniEnd 반환에서 상태를 읽는다. InstantEntrance0.1.1의 직접 대상은 이 함수가 아니다. 자체 패널이 열린 동안 해당 UICreateRoleCtrl.UIAgent와 UIContent.touchable, GRoot 포커스 및 장면 transform을 다룬다. 취소 복원/숨김 정리 구현, 원본5명 enum/배열 확장 없음. 입력을 다루는 다른 모드와 실행 순서·병용 실전 미확인. 기존 TableApply/Rows/Sea 코드 변경 없음. [근거](../../analysis/hero-stage2/REPORT.md).


## CityEditor Core 0.7.0 — 시작 진단 준비본
진단 Postfix4개는 항구/시설/부두 조회 및 시설 초기화 반환 관측이며 매프레임 폴링/원본 차단/결과 교체 없음. 자체 관측은180초/2048조회로 제한하고 설치 실패를 개별 기록한다. FunctionData/OpenFacilityList는 생성·입항 시 읽기만 한다. 기존 EnterHarbourAniEnd Postfix에서 시작 일반 이벤트를 지정하면 원본 GameEventManager를 호출하여 게임 상태를 바꿀 수 있다. 이번 비교값은 이벤트0으로 호출하지 않는다. 도시 모드의 최종 Port/PortFacility/Wharf 자료와 초기화 순서가 관측 대상이다. NavtoCity와 항구 진입 접점 및 게임 이벤트 실행 중 다른 모드와 병용 실전 미확인. [패치 계약](city-editor-core/PATCHES.md) · [근거](../../analysis/hero-stage2/DIAGNOSTIC-0.7.0.md).


## CityEditor Core0.7.1 준비본

UICreateRoleView.Refresh/ShowHook 추가Postfix, UIContent.visible 공유 및 복원. HarbourScene.OnEnter 신규Prefix는 신규 주인공 시작 설정의 FunctionOpenDB.SetFunctionOpen을 호출하므로 NavtoCity와 같은 함수 접점이며 기능 알림 구독자에 영향 가능. 기존 저장에는 실행하지 않음. 새 도시 Wharf 행 추가는 전체 항구 초기화 자료를 바꿈. 빌드/오프라인 검사는 실전 병용 성공이 아님. [근거](city-editor-core/0.7.1.md).


## CityEditor Core 0.7.2 준비본

취소 경로 OnAction_B/ReturnToMenu에 소유 컨트롤러 한정 Prefix 추가. 기존 공유 UIAgent/포커스/Hide 수명주기 및 선택 PNG 소유 계약 유지. HarbourScene.OnEnter는 NavtoCity와 같은 함수이며 새 기함 지급으로 PlayerShipHold/선장/dirty/알림/자동 저장 접점 추가. 다른 주인공/저장 불러오기 제외. 실행 순서·병용은 실전 미검증. [계약](city-editor-core/PATCHES.md).

2026-09-23 사용자 정정: 0.7.2 시작 함선은 아라비아 갤리220에서 일반 슬루프110으로 변경. 5000 및 다른 설정, 함수·입력·수명주기 계약은 유지. 원본 Ship110: 슬루프, Special=false, NeedSailorNumber=18. DLL 재빌드 불필요한 배포 설정 변경.


2026-09-24: 캐릭터 에디터 통합 이벤트 작업 화면0.1.0. 에디터 선택/창/문서 공유만 변경하며 게임 Core0.7.4는 무변경. docs/mods/character-editor/CURRENT.md 참조.


2026-09-24 에디터0.1.1: startTalk/startEvent 카드와 설치 추가 대화 읽기 연결. 원본 표 별도 보완본, 파일 쓰기 없음. 문서 설정(0 포함)이 설치 설정보다 우선. docs/mods/character-editor/0.1.1.md 참조.

## Core 0.7.5 이름표 표시 그룹
새 주인공 이름표가 원본 groupBtnCharacter를 공유한다. 그룹의 visible/alpha를 쓰지 않고 멤버로 연결한다. 그룹 배치·위치를 변경하는 모드와 경계 계산 접점이 있음. 후크 및 입력 계약 변화 없음, 병용 실전 미확인. [상세](city-editor-core/0.7.5.md).

## Core 0.7.6 상세 스타일
원본 UIcompNameChinese/English와 입력 아이콘 리소스를 자체 모달에 재사용. 원본 모델 읽기만 하며 원본 UI·키 바인딩·공유 텍스처 소유권 무변경. 표시 겹침·병용은 게임 확인 전. [근거](city-editor-core/0.7.6.md).

## Core 0.7.7 이름 PNG
자체 모달 이름 이미지만 기존 소유 텍스처 목록에 추가. 원본 리소스 파일·후크·입력 무변경. 병용 실전 확인 전. [근거](city-editor-core/0.7.7.md).

## Core0.13.0 / Roman Revival 정의 DLC0.1.0
TemplateUtils.InitTemplateData Prefix(Priority.First)에서 Culture/Country/문구 신규행 및 가격·문화거리 배열을 검증 후 반영. TableApply·다른 문화/가격 모드·해당 초기화 후크와 접점. 이탈리아 기준 복제이며 기존27칸 보존; 배열 expected 불일치는 거부. 입력/세이브 후크 없음. 병용 실전 미확인. [계약](roman-revival/PATCHES.md) · [배포](roman-revival/0.1.0.md).

## City Editor 대체 도시 설정 작성 기능 1

2026-09-26: 대체 도시 설정은 editorState에만 저장하며 Core가 실행하는 ops에 포함하지 않는다. 에디터 문서/모달 입력만 변경, 새 게임 후크·공유 런타임 상태·세이브 쓰기 없음. 설치 Core0.13.0 및 DLC0.1.0 유지. 이벤트 후 전환은 미구현. [계약](roman-revival/PATCHES.md) · [검증/설치 상태](roman-revival/EDITOR-VARIANTS-1.md).

## CityEditor Core0.14.0 도시 개발 단계

기존/신규 도시의 선택적 시설 개방 및 NPC 출생/목적지 정책. 저장 FBVersionSet 벡터 전용 확장, 원본 Deserialize 앞뒤 분리/복구, 살아 있는 VersionSet 불변. 시설 목록/dirty, NPC BornPorts 호출 중 교체, 행동 후보 배열과 항구 화면 갱신 접점. NavDiag의 도착/출발 함수는 취소하지 않는다. 24후크 메타데이터 단일 RVA/본문 대조와 오프라인 검사는 실전 충돌 없음의 증명이 아니다. [계약](city-development/PATCHES.md).

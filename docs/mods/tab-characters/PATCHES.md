# Tab Characters 0.6.9 패치 계약

26개 훅 유지. 언어 모드 `RefreshBookDetails`에서 원본 `RefreshSkillBookTips` 호출 전 `UICharacterMain.roleInfo.listLanguage.data = PlayerRoleData.HeroLanguage`, `numItems = max(현재, UICharacterModel.MaxLanguageCount)`; 호출 후 이전 data·numItems 복원. 게임 상태 쓰기 없음. [근거](0.6.9.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.6.8 패치 계약

26개 훅 유지. `UISheetCharacter.btnReturn`에 onTouchBegin/onTouchEnd 리스너 추가(장비 칸 캐처와 별개): 학습·장비 패널이 닫힌 상태에서 texLanguage/TexTitleCharacterLanguage 영역 안 좌클릭 누름만 CaptureTouch, 뗄 때 StopPropagation+CancelClick, 영역 안에서 떼면 OpenBooks(-1). 영역 밖은 원본. 게임 상태 쓰기 없음. [근거](0.6.8.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.6.7 패치 계약

26개 훅 유지. 진단 전용: 언어 클릭 연결 부착·언어 영역 좌클릭 누름의 hit 대상·클릭 수신·OpenBooks(-1) 조기 반환 사유를 `[LangTrace]`로 기록(최대 40줄, 누름 탐지 5회). 동작·게임 상태 쓰기 불변. [근거](0.6.7.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.6.6 패치 계약

26개 훅 유지. 학습 패널 포인트 버튼 표시만 변경(크기·색·글자). ThemeTexts가 버튼 글자 색을 바꾸지 않도록 제외. 포인트 사용 경로·입력 판정 불변. [근거](0.6.6.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.6.5 패치 계약

26개 훅 유지. 장비 패널: model.ListFilterEquip이 패널이 만든 묶음 목록과 다른 객체면(게임의 SyncPlayerDataToModel→InitEquipData→SetFilterListEquip) Tick·마우스 올림·클릭·우클릭·미리보기 전에 다시 묶고 행을 다시 그린다. 원본 OnClickBtnListEquip/OnTakeOffEquip에 넘기는 인덱스는 화면 행 목록의 GUID로 찾은 현재 인덱스. 로그: `model list rebuilt by the game … regrouped`(최대 20줄), 동작 로그에 guid·wearRole. [근거](0.6.5.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.6.4 패치 계약

26개 훅 유지. SkillTabHide 훅 5개의 대상 탭을 {3 스킬} → {3 스킬, 4 장비}로 확장(GetSheetOpenByIndex false, OnFeatureQuickKeyStart·OnInputActionStart 원본 생략, ItemRender 버튼 숨김). ShowSheet Prefix가 Equip 요청도 Character로 바꾼다. 장비 시트 객체는 유지(패널이 위젯을 빌림). 닫기 라벨: autoSize Both, 버튼 중앙, 버튼 위 순서. [근거](0.6.4.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.6.3 패치 계약

26개 훅 유지. 패널이 열린 동안 UICharacterModel.ListFilterEquip을 묶은 목록(새 List, 원래 EquipItem 객체 재사용, 묶음은 네이티브 순서상 첫 기록이 대표)으로 교체하고 InitEquipData·SetFilterListEquip 뒤마다 다시 묶는다. 닫을 때(화면 유효 시) SetFilterListEquip()으로 원래 목록을 다시 만든다. 목록 행에 자체 `xN` GTextField(닫을 때 Dispose). 원본 장착/해제/미리보기 함수는 묶은 목록의 인덱스를 그대로 쓴다. [근거](0.6.3.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.6.2 패치 계약

26개 훅 유지. 추가 리스너: UISheetCharacter.btnReturn onTouchBegin/onTouchEnd(인물 시트 Refresh 때 1회 부착, 창 닫힘·리셋 때 제거). 누름 좌표가 listEquip 칸 안이고 장비 패널이 닫혀 있을 때만 CaptureTouch → 뗌에서 StopPropagation + Stage.CancelClick(원본 btnReturn 클릭 생략) → 같은 칸이면 패널 열기. 그 외 입력은 원본 경로. [근거](0.6.2.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.6.1 패치 계약

26개 훅 유지. 읽기 전용 계측 추가(세션당 40줄): 슬롯 바인딩 시 목록·자식·조상 touchable/touchChildren, 인물 시트에서 장비 칸 영역 좌클릭 시 Stage.touchTarget 조상 체인, OpenEquip 진입·거부 사유. [근거](0.6.1.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.6.0 패치 계약

26개 훅. 추가: UICharacterView.RefreshTipsRoleInfo Postfix `EquipRoleInfoAfter`(장비 패널 미리보기 중에만 RefreshPropChangeEffect 재적용, content.listSkill은 렌더하지 않은 대역 행으로 잠시 교체). 기존 훅에 선택적 호출 추가: Refresh Prefix(빌린 SheetType 중 Refresh 진단), ShowSheet/HideHook/OnClickBtnLeft·Right/Release(패널 닫기), RenderListBtnSkill Postfix(미리보기 재적용), InputSystemManager.OnEventCaptureInput Prefix(패널 열림 중 Action_A/B/X/Y 소비), ForceReset(리셋). 쓰는 원본 상태: UISheetEquip 위젯 12개의 부모·배치·표시·relations·group·DisplayLock(닫을 때 복원), listEquip·comFilter.listBtn itemRenderer(복원), 행 onClick/onRollOver/onRollOut(닫을 때 비움), UICharacterModel._equipIndex·_wearEquipIndex·IsSelectEquipFilter·_filterIndexEquip·CurFoucType(복원)·_filterIndex(유지, 원본 필터 선택과 동일)·ListPropEquip(닫을 때 비움), 인물 시트 스킬 행 texAddLevel·isAdd(보관·복원), SheetCharacter.listEquip.touchable(복원), 장비 시트 listEquipWear.numItems(필요 시 채움). `_sheetType`=2는 OnClickBtnListEquip·OnTakeOffEquip·InitEquipPropChange 한 번의 동기 호출 동안만. 세이브 쓰기는 원본 PlayerEquipDB.PutOnEquip/TakeOffEquip 경로. [근거](0.6.0.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.10 패치 계약

25개 훅 유지. PrepareNativeInfo는 첫 호출에서만 groupTips 경계를 읽어 패널 기준으로 쓰고(화면 크기 변경 시 재읽기), 이후에는 경계 변화만 1회 기록한다. 책 칸 수량 라벨은 Common.UIcompSlotCommon 템플릿 title 서식(1회 생성·폐기·캐시) + Item Rebuild BagBadge 규칙. 원본 위젯·저장 데이터 변경 없음. [근거](0.5.10.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.9 패치 계약

25개 훅. 추가: UIMenuCtrl.GetSheetOpenByIndex Postfix(3→false), UIGlobalCtrl.OnFeatureQuickKeyStart·OnInputActionStart Prefix(3이면 원본 생략), UIMenuView.ItemRender Postfix(3번 버튼 visible=false, ListTitle.foldInvisibleItems=true), UIMenuView.RefreshBtnKnowledge Postfix(3번 ctrlNewState를 2번으로 복사). 변경: UICharacterCtrl.ShowSheet Prefix BeforeSheet 인자를 ref로 받아 Skill→Character. 메뉴 정렬·부모·자식 순서, 스킬 시트 객체, 저장 데이터는 건드리지 않는다. [근거](0.5.9.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.8 패치 계약

20개 훅 유지. TickBooks에서 물리 Space(창 포커스, 텍스트 입력 포커스 없음)를, BookCapture에서 Action_A를 받아 ConfirmLearn으로 보낸다. confirmLatch로 한 번 누를 때 1회, Space와 Action_A 해제 후 해제. LearnBlock()==null일 때만 Commit. 누를 때 학습/무시 사유 1줄 로그. [근거](0.5.8.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.7 패치 계약

20개 훅 유지. BookCapture: 패널 열림 + Action_A 눌림 + CanLearnSelected(유효·비학습중·선택 GUID가 현재 목록에 있음·재고>0·GetBookStateByParam==0)일 때만 입력 소비 후 Commit 1회, 누름 유지/해제 입력은 bookHeldActions로 소비. 그 외 Action_A는 원본 경로. [근거](0.5.7.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.6 패치 계약

20개 훅 유지. 원본 UISheetCharacter 정보창 위젯의 부모/그룹/좌표/크기/배율/Relations/gear/표시를 쓰지 않는다(groupTips EnsureBoundsCorrect와 경계·배경 읽기만). RefreshTipsSkill 외부 호출 차단 유지. 원본 btnSkillUp은 원래 크기로 오른쪽 열에 배치, 누름 중 gear 잠금 상태에서 배율/좌표만 변경, 닫을 때 RestoreLayout 복원·리스너 제거. 학습은 원본 클릭 → OnClickBtnBook → BookUse → Commit. 0.5.5의 자체 학습 버튼·지연 요청 제거. [근거](0.5.6.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.5 패치 계약

20개 훅 유지. 학습 패널 배경(companionBackdrop.alpha 또는 자체 배경 채우기 alpha)만 0.8. 원본 `btnSkillUp`은 열로 옮기지 않고 숨김(닫을 때 RestoreLayout이 복원). 자체 학습 버튼은 release 시 CancelClick 후 1건 요청 큐 → 다음 프레임 `Commit()`(기존 재확인·원본 OnClickBtnBook 1회). 패널 폐기 시 대기 요청 폐기. 저장 호출·책 차감 경로 변경 없음. 새 참조: GObject.alpha(set), GTextField.textWidth(get). [근거](0.5.5.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.4 패치 계약

20개 훅 유지. FeatureUI 정렬 쓰기 제거. 원본 UITips content의 부모/형제 위치/sortingOrder/touchable을 보관하고 학습 안내 범위에만 임시 GRoot 배치. 이동 시 AniTips._options 자동 중단 방지 비트를 finally 복원. 원본 분리 후 재부착 금지. 기존 speed 복원 및 DB 계약 유지. [수명·본문](0.5.4.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.3 패치 계약

20개 훅 유지. 포인트/ShowBaseTips/정렬 변경·복원 경계 및 TickBooks에 읽기 전용 UI 관측 추가. UI 부모/표시/정렬/입력/DB 변경 없음. 관측 참조는 15초 또는 Reset/예외에 해제, 최대 180개 기록. UI 공용 root 자식 최대 64개/팁 조상 12단계만 읽는다. [범위·근거](0.5.3.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.2 패치 계약

신규 훅 없음(20개). 바깥 입력은 inputEvent.x/y와 CaptureTouch의 begin/end를 함께 판정한다. 해제 시에도 두 창 밖이어야 닫는다. CancelClick 유지. 로그에 좌표/배율/알림 상태를 기록한다. 포인트·원본 알림·정산 변경 없음. [근거](0.5.2.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.1 패치 계약

UIHeroLevelUpCtrl의 OnClickBtnLevelUp/OnClickBtnLevelUpFive/OnAction_A/OnAction_X Prefix AdvancePastReward, ChangeRewardSkillState Prefix RewardBefore(무변경)/Postfix RewardAfter 추가. 총 20개. 활성 View·보상 연출 중·BanTouch=false·ResultAniCount=0 범위에서만 원본 알림 완료 콜백을 실행한다. 레벨업 조건 및 DB 정산 원본 유지. 라벨 touchable은 임시 보관/해제/복원한다.

포인트 버튼 release는 Stage.CancelClick 후 자체 1건 요청 큐에 넣고 다음 프레임에 기존 검증 후 적용한다. 패널 폐기 시 대기 요청도 폐기. 내용 축소 때 배치 높이 하한을 유지한다. 종료 사유 로그 추가. 기존 15개 훅 계약 유지. [본문·추론·검사 구분](0.5.1.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.5.0 패치 계약

15개 훅 유지. UITipsCtrl.ShowBaseTips Prefix는 항상 원본 허용, Postfix BookTipsAfter 추가. 자체 Committing 호출의 native AniTips.timeScale와 원본 알림 root 조상의 sortingOrder를 임시 보관/변경하고 종료·다른 알림·모드 변경·정리 시 복원한다. 자체 알림 객체 생성/폐기 제거.

창 뒤 비가시 입력 사각형의 onTouchBegin에서 두 창 외부 좌클릭만 취소/닫기 처리한다. Stage.CancelClick으로 후속 배경 클릭 방지. 포인트 버튼은 CaptureTouch/onTouchBegin/Move/End 및 RollOver/Out으로 누름 상태를 관리하고 안에서 해제할 때 기존 검증된 DB 호출을 실행한다. UI 정리 때 모든 추가 이벤트/그래프는 기존 자체 패널 수명으로 제거한다. 저장 경로·15개 기존 훅 범위 유지. [근거](0.5.0.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.4.1 패치 계약

15개 훅 유지. PrepareNativeInfo는 disposed/displayObject=null 요소를 이동하지 않는다. 중첩 GGroup은 원래 부모에 남기고 표시 자식만 기존 저장/복원 계약으로 옮긴다. RootBounds의 비표시 요소 좌표는 부모 기준 변환을 사용한다. 입력·책/포인트 저장 경로·공유 모델 변경 없음. [실패 근거와 회귀 검사](0.4.1.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.4.0 패치 계약

기존 14개 + `UICharacterView.RefreshTipsSkill(PlayerRoleData)` Prefix `BookNativeTips` (RVA 0x1025D10) = 15개 훅. 같은 View의 native 정보 위젯을 빌린 동안 외부 tooltip 갱신을 막고 자체 선택 정보 갱신은 허용한다. 다른 View/닫힌 상태 원본 유지. [서명/RVA](../../../TabCharacters/evidence/0.4.0/hooks.json).

공유 상태에 UISheetCharacter.groupTips 소속 위젯의 부모/순서/배율/좌표/크기/표시/그룹/Relations, tipsType 재적용 및 중립 텍스트 색상 복원을 추가한다. 스냅샷은 이동 전에 모두 수집한다. 자체 정보 갱신 중에만 model.SkillIndex/CurFoucType을 선택 스킬로 바꿔 finally 복원한다. 정리 때 원본 위젯을 먼저 복원하고 자체 컨테이너를 폐기한다. 원본 부모가 이미 폐기되었으면 빌린 위젯을 분리한다.

중앙 팝업/암전/전체 입력 차단/강제 focus 이동을 제거했다. Esc/우클릭/Action_B 취소 및 짧은 해제 보호 외 입력은 전달한다. 전역 확정→책 소비 변환 없음. 스킬 클릭으로 기존 패널 교체, 인물/페이지/탭/메뉴 이탈 시 정리. 책/포인트 저장 경로와 3D 전체 렌더 금지 유지. [본문·검사·실전 한계](0.4.0.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.3.0 패치 계약

기존 13개 + UITipsCtrl.ShowBaseTips(string,string) Prefix(BookTips, RVA 0xDFAB60) = 14개 훅. 자체 학습 Committing 범위의 texTips/url만 최상단 비입력 알림에 전달하고 원본 중복 표시를 막는다. 일반 팁/전달 실패는 원본을 유지한다.

팝업/직계 자식의 공유 상태에 부모·원래 순서·singleLine/autoSize·sheet.touchable을 추가 보관/복원한다. 독립된 두 열로 이동한 native 컨트롤에는 원본 sheet controller를 재적용한다. 입력은 모달 범위의 Keyboard Esc/Mouse right polling 및 기존 InputSystem 캡처를 함께 사용한다. Tab은 배경 차단만 하며 닫기 입력의 눌림/해제 직후는 배경 입력을 소비한다. 하단 포인트 표시 및 학습 알림은 자체 root 자식으로 소유·정리한다.

저장 변경: 선택 스킬의 포인트 사용 버튼에서 현재 인물/DB 동일 객체·잔여 포인트·원본 최대 레벨을 확인한 뒤 PlayerHoldRoleDB.UpdateRoleSkillLevelBySkillPoints(roleId,skillId,1)을 한 번 호출한다. Native 함수의 포인트 부족 사전 검사 부재를 UI 호출 전에 보완한다. 책 소비는 기존 Item Rebuild 경로 유지, 추가 수량/함대 경험치 차감 없음. 스킬 전체 렌더/3D 등록 호출 금지 유지. [본문·검사·실전 한계](0.3.0.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.2.1 패치 계약

0.2.0과 동일한 13개 훅. 추가/삭제 훅 없음. BookPopup.Refresh에서 RefreshSheetSkill 호출 제거; RenderListSkillShow/3D 등록을 호출하지 않는 자체 미리보기 목록을 RefreshSkillBookTips 실행 중에만 공유 content.listSkill에 대입하고 finally 복원한다. 언어 미리보기는 원본 RefreshTipsRoleInfo를 유지한다.

공유 상태 추가 명시: UISheetSkill/직계 자식의 크기·좌표·표시·그룹·Relations. 수명 시작에 보관하고 팝업에서 실제 내용만 배치하며 종료/실패에 복원한다. 원본 gear 위치 저장값 변경을 막기 위해 자체 좌표/크기 변경 동안만 _gearLocked를 보관/설정/복원한다. 자체 미리보기/관계 보관 객체는 닫을 때 해제한다. 같은 sheet 배치를 변경하는 모드는 병용 확인이 필요하다.

원본 사용/금지/조건·책 행 렌더와 Item Rebuild 단일 소비 계약, 모달 입력과 닫기 계약 유지. native 3D 관리자 후킹/사전 수정/예외 억제 없음. [현재 원본·사용자 보고·검증 구분](0.2.1.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.2.0 패치 계약

기존 9개 + 신규 4개 = 13개. [서명/RVA](../../../TabCharacters/evidence/0.2.0/hooks.json).

| 대상 | RVA | 변경 |
|---|---|---|
| UICharacterView.RenderListBtnSkill | 0x1026CD0 | 원본 렌더 전 자체 이전 클릭 연결 해제, 원본 후 인물 스킬 행 클릭 추가 |
| UICharacterCtrl.OnClickBtnBook | 0x94BC90 | 자체 팝업 동안 최신 인물/경험치/보유 책/금지 판정 검증 후 원본을 한 번 호출; 외부 스킬창 유지 |
| InputSystemManager.OnEventCaptureInput | 0x8C50C0 | 자체 팝업의 전체 배경 게임 입력 차단, 확정/닫기 처리, 누름에 대응하는 release까지 소비 |
| GameManager.ForceReset (static) | 0x4D9000 | 팝업/이벤트/입력 추적 정리 |

기존 View.Refresh 후처리에 언어 영역 클릭 연결/팝업 갱신 추가. ShowSheet/HideHook/Release/모드 종료에 팝업을 먼저 복원한다. 기존 페이지 입력은 팝업 동안 소비한다. 기존 인물 목록 및 선택 인덱스 교체 없음.

공유 상태: UISheetSkill 부모/배율/그룹/표시 잠금과 listBook renderer/행 이벤트, 모델 ListFilterBook/BookIndex/CurSelectBook/IsSelectBook/Exp/CurFoucType, 원본 미리보기 목록, GRoot 포커스 및 입력 캡처. SheetType은 Character로 유지한다. 원본 상세용 RefreshSheetSkill로 미리보기 행을 먼저 준비한다. 원본 학습 호출을 제외한 저장 데이터 직접 쓰기 없음.

사용 직전 대상이 사라졌으면 다른 책으로 대체 소비하지 않는다. Native 클릭 callback은 팝업 행에서만 교체하며 원래 renderer는 닫을 때 복원하고 풀에 남은 팝업 callback을 지운다. 원래 sheet는 Dispose하지 않는다. 기본 입력과 겹치는 타 모드 병용은 미검증. [전체 근거/제한](0.2.0.md).

아래는 이전 버전 기록이다.

# Tab Characters 0.1.2 패치 계약

소스: [EntryPoint.cs](../../../TabCharacters/src/EntryPoint.cs). [실제 서명/RVA](../../../TabCharacters/evidence/hooks.json).

| 대상 | RVA | 변경 |
|---|---|---|
| UICharacterView.Refresh | 0x10236A0 | Prefix에서 인물 탭 페이지 상태/renderer 연결, 원본 유지, Postfix에서 목록 위치 0 고정. Finalizer는 원래 예외 그대로 반환 |
| UICharacterView.HideHook | 0x1029A30 | Prefix에서 페이지 참조·UI 설정·비표시 초상화 및 닫힌 상세 이미지 정리, 원본 유지 |
| UICharacterCtrl.ShowSheet(ESheetType) | 0x947500 | 비인물 탭 진입 전 원래 renderer·UI 설정 복원, 원본 유지 |
| UICharacterCtrl.OnClickBtnLeft | 0x94CE80 | 인물 탭에서만 원본 선택 변경을 대체하고 이전 페이지 표시 |
| UICharacterCtrl.OnClickBtnRight | 0x94CF20 | 인물 탭에서만 원본 선택 변경을 대체하고 다음 페이지 표시 |
| GList.set_numItems(int) | 0x22055F0 | 소유한 listRole의 native pointer가 정확히 일치할 때만 현재 페이지 항목 수로 인수 변경 |
| GList.ScrollToView(int,bool,bool) | 0x22049C0 | 같은 listRole에서만 원본 차단. 나머지 목록 원본 유지 |
| GList.HandleArrowKey(int) | 0x2203680 | 같은 listRole에서만 원본 차단/결과 -1. 나머지 목록 원본 유지 |
| GList.Dispose | 0x22013F0 | 같은 listRole을 폐기하기 전 자체 renderer 연결/참조 해제. 폐기 중 재렌더 없음, 원본 유지 |

## renderer와 인덱스

Harmony로 RenderListRole 본문을 교체하지 않는다. 해당 listRole의 itemRenderer만 원본을 보관한 delegate로 감싼다. 슬롯 0~9를 전체 명단의 `페이지 시작 + 슬롯`으로 바꾸어 원본 renderer에 전달한다. 원본 renderer는 이 전체 인덱스로 초상화/직책/선택 표시를 만들고, 같은 인덱스를 클릭 콜백에 캡처한다. 원본 ChangeCurRoleIndex·저장/역할 데이터는 그대로다.

원본 UICharacterCtrl 방향키와 Action_A 본문에는 항해사 선택 변경이 없으며, L1/R1은 기존 좌우 버튼 FireClick으로 연결된다. 그 버튼 핸들러를 페이지 이동으로 바꾸고 GList 자체 방향키 선택을 막는다. 전역 키 폴링이나 입력 캡처·포커스 변경 없음. 실제 Q/E 바인딩은 사용자 키 설정을 따른다.

## 공유 상태/수명주기

인물 탭 단일 View/Model/listRole만 보관한다. 명단 객체 교체·선택 인덱스 쓰기 없음. 목록 Count 변경 시 페이지 상한을 보정하며 행은 현재 명단 기준으로 다시 렌더한다.
선택 추적 스크롤, scrollItemToViewOnClick, touchEffect, mouseWheelEnabled, 목록 선택 모드를 해당 목록에 한정한다. 레이아웃 SingleRow 및 align=Center 적용. 원래 align을 보관하고 자체 Center 값이 유지된 경우 종료 시 복원한다. 0.1.1 신규 훅 없음. 닫기·다른 탭·View/Model/list 교체·list Dispose·모드 종료에 원래 renderer와 설정을 복원/참조 해제한다. renderer가 외부에서 교체됐으면 덮어쓰지 않는다.

renderer 내부 오류는 네이티브 콜백 밖으로 던지지 않고 다음 Update에서 원본 UI 복구/자체 훅 해제를 수행한다. 게임 원본 Refresh 예외는 억제하지 않는다. GameAssembly 디스크 SHA256 불일치 시 훅을 설치하지 않는다. 실행 중 다른 패치 적용 여부까지 인증하는 검사는 아니다.

## 기존 모드 접점

- TabMenu 진단과 UICharacterView.Refresh 직접 중첩. 원본 renderer 호출도 계속 계측된다. 이제 행 렌더 수는 전체 명단 수가 아니라 표시 페이지 인원수에 따라 변한다.
- 분석 사본 SpecialOrderEra 1.1.0 CommerceMilestoneUiRuntime도 GList.set_numItems에 BeforeSetBuildingItemCount를 등록한다. 그쪽은 활성 상회 건물 목록 pointer 또는 IsBuildingList로, 이쪽은 인물 listRole pointer로 제한한다. 정적 조건 분리이며 실행 병용 무충돌 보장이 아니다.
- 공통 GList 훅에는 메인 스레드/정확한 인스턴스 범위 검사가 있다. 외부 모드 전체·동적 패치 순서는 미검증. 임의 Harmony 우선순위 없음.

게임·Steam 실행 없음. 관리 대역 통과와 실제 IL2CPP/화면 검증을 구분한다.

## 0.1.2 이미지 참조 정리 계약

새 훅 없음. [PortraitResources.cs](../../../TabCharacters/src/PortraitResources.cs) 추가. 페이지 상태의 Pin에서 itemPool.count 변화/최초 상태를 확인하여 소유 listRole.itemPool._pool만 순회한다. 닫기/원래 UI 복원에서는 원본 numItems로 행을 풀에 반환한 뒤 정리한다. UIbtnRole이고 disposed=false, parent=null인 행의 loaderRole만 대상으로 한다. 풀 큐를 변경하거나 UI 객체를 Dispose하지 않는다. 기본 child 수가 그대로인 페이지/상세 갱신에서 풀 재순회를 생략한다.

loader.url을 빈 문자열로 설정해 원본 ClearContent/FreeExternal을 수행한다. 그 후 실제 MyGLoader로 TryCast되고 _handler가 남아 있을 때만 ReleaseLoader를 호출한다. 완료된 이미지의 원래 해제가 handler를 비웠으면 추가 반환하지 않는다. 미완료 로딩 참조는 이 후처리로 반환한다. 기본/다른 GLoader는 원본 URL 경로만 사용한다.

BeforeHide는 현재 소유 PageState의 큰 content.loaderRole과 content.roleInfo.loaderRole만 정리한다. 탭 전환/페이지 이동에서는 이 상세 두 로더를 비우지 않는다. 스킬·장비 탭 상태에서 닫히는 창에는 이 정리를 새로 적용하지 않는다. 원본 ShowSheet의 선택 데이터 dirty/Refresh 및 ShowHook의 RefreshTipsRoleInfo가 재열기 때 URL을 다시 설정한다.

로더별 정리 횟수를 합쳐 이벤트 단위 로그를 남긴다. 정리 오류는 cleanupEnabled만 끄고 페이지 기능을 유지한다. 전체 리소스 관리자/패키지/아틀라스·원본 항해사/저장 데이터 및 3D 해제는 변경하지 않는다. 공통 native 로더 관리자를 별도로 후킹하지 않는다. [설치 관리자 우회 코드와 확인 범위](0.1.2.md).
